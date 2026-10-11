# ĐẶC TẢ KỸ THUẬT & HƯỚNG DẪN TRIỂN KHAI
## TÍNH NĂNG: HỎI ĐÁP AI - BÁCH KHOA TOÀN THƯ CỦA DAVE (THE SUBURBAN ALMANAC AI)

> **Dự án:** Plants vs. Zombies 2D Fusion (PvZ 2D Game AI Advisor)  
> **Phiên bản tài liệu:** 1.0  
> **Mục tiêu:** Cung cấp thông số kỹ thuật, API contract, Prompt tri thức và code mẫu để server Python (FastAPI) triển khai và kiểm thử endpoint hỏi đáp AI phục vụ cho Unity Client.

---

## 1. TỔNG QUAN KIẾN TRÚC & LUỒNG HOẠT ĐỘNG

Tính năng **Hỏi Đáp AI** cho phép người chơi bấm vào cuốn sách **The Suburban Almanac** (Bách khoa toàn thư) ngay tại màn hình chính (HomeScreen) để trò chuyện với Crazy Dave, tra cứu công thức ghép cây Fusion, điểm mạnh/yếu của Zombie và chiến thuật đặt trận.

### Sơ đồ tương tác:

```
[Unity Client (HomeScreen)]
   │
   │ 1. Người chơi nhấn vào cuốn sách Suburban Almanac
   ▼
[AIQAPopup (UI Chat) & AIQAManager]
   │
   │ 2. HTTP POST http://<SERVER_HOST>:8000/qa
   │    Body: {"question": "...", "session_id": "menu_session"}
   ▼
[Python FastAPI Server (pvz-2d-game-ai-advisor)]
   ├── Router: POST /qa (src/routers.py)
   ├── Service: answer_gameplay_qa() (src/services.py)
   ├── Tri thức game: DAVE_QA_SYSTEM_PROMPT
   └── Google Gemini API (gemini-2.5-flash / gemini-2.0-flash)
   │
   │ 3. HTTP 200 OK: {"answer": "...", "status": "ok"}
   ▼
[Unity Client]
   │ 4. Hiển thị tin nhắn dạng bong bóng chat co giãn tự động
```

---

## 2. CHI TIẾT API CONTRACT

### 2.1. Endpoint Information
- **URL:** `http://<SERVER_HOST>:8000/qa`
- **Method:** `POST`
- **Headers:** `Content-Type: application/json`
- **Timeout khuyến nghị:** 15 giây

### 2.2. Request Schema (Client -> Server)
```json
{
  "question": "Cơ chế ghép cây Fusion trong game hoạt động thế nào?",
  "session_id": "menu_session"
}
```

| Trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `question` | `string` | **Có** | Câu hỏi của người chơi (về cây trồng, zombie, công thức lai, mẹo). Chuỗi không được để trống. |
| `session_id` | `string` | Không | Mã phiên giao dịch (mặc định: `"default"` hoặc `"menu_session"`). |

### 2.3. Response Schema (Server -> Client)

#### Phản hồi thành công (HTTP 200 OK):
```json
{
  "answer": "Wabby Wabbo! Trong PvZ Fusion, cơ chế ghép cây cực kỳ độc đáo: Anh bạn chỉ việc trồng một cây đè lên một cây tương thích khác trên cùng một ô đất!\n\nVí dụ: Ghép Quả Óc Chó (WallNut) với Đuốc Lửa (TorchWood) sẽ tạo ra Hạt Dẻ Lửa (FireWallNut) - vừa có máu trâu bò vừa phản sát thương lửa thiêu rụi zombie!",
  "status": "ok"
}
```

| Trường | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `answer` | `string` | Nội dung giải đáp chi tiết theo văn phong Crazy Dave. Hỗ trợ ký tự xuống dòng `\n` và các gạch đầu dòng `•`. |
| `status` | `string` | Trạng thái xử lý: `"ok"` hoặc `"error"`. |

#### Phản hồi lỗi xác thực (HTTP 422 Unprocessable Entity):
Nếu client gửi body thiếu trường `question` hoặc sai định dạng JSON.

---

## 3. CODE MẪU PHÍA SERVER PYTHON (FASTAPI)

### 3.1. Data Models (`src/models.py`)
Thêm các Pydantic models sau:

```python
from typing import Optional
from pydantic import BaseModel, Field

class QARequest(BaseModel):
    """Yêu cầu hỏi đáp về cơ chế game, cây trồng, zombie hoặc chiến thuật."""
    question: str = Field(..., description="Câu hỏi của người chơi")
    session_id: Optional[str] = Field(default="default", description="ID phiên để theo dõi ngữ cảnh")

class QAResponse(BaseModel):
    """Phản hồi giải đáp thắc mắc từ Crazy Dave / Bách khoa toàn thư."""
    answer: str = Field(..., description="Nội dung giải đáp chi tiết, hóm hỉnh và bổ ích")
    status: str = Field(default="ok", description="'ok' hoặc 'error'")
```

---

### 3.2. Router Endpoint (`src/routers.py`)
Thêm endpoint `POST /qa`:

```python
from fastapi import APIRouter
from src.models import QARequest, QAResponse
from src.services import answer_gameplay_qa

router = APIRouter(tags=["AI Advisor"])

@router.post("/qa", response_model=QAResponse, summary="Hỏi đáp về cơ chế game, cây trồng, zombie và chiến thuật")
async def ask_gameplay_qa(request: QARequest) -> QAResponse:
    """
    Endpoint tiếp nhận câu hỏi của người chơi từ HomeScreen / Suburban Almanac,
    chuyển cho Crazy Dave AI xử lý và trả về giải đáp chi tiết.
    """
    return await answer_gameplay_qa(request)
```

---

### 3.3. Service Logic & System Prompt (`src/services.py`)

#### System Prompt Tri Thức (`DAVE_QA_SYSTEM_PROMPT`):
```python
DAVE_QA_SYSTEM_PROMPT = """Bạn là Crazy Dave (hoặc Trợ lý Bách Khoa Toàn Thư Cây & Zombie - The Suburban Almanac) trong tựa game Plants vs Zombies 2D Fusion.
Nhiệm vụ: Giải đáp câu hỏi của người chơi về cơ chế game, các loại cây, zombie và chiến thuật một cách tận tình, dễ hiểu, sinh động, mang phong cách hóm hỉnh đặc trưng của Crazy Dave ("Wabby Wabbo!").

TRI THỨC BÁCH KHOA VỀ PVZ FUSION:
1. CƠ CHẾ LAI TẠO CÂY (FUSION SYSTEM):
- Cơ chế cốt lõi của PvZ Fusion là ghép 2 cây tương thích trực tiếp lên nhau trên cùng 1 ô đất để tạo thành giống cây lai mới với sức mạnh vượt trội.
- Ví dụ:
  + FireWallNutFusion: Ghép WallNut + TorchWood (vừa có lượng máu cực lớn của Óc Chó, vừa có ngọn lửa phản sát thương thiêu đốt zombie khi chúng cắn).
  + Các dòng lai kết hợp Peashooter với các nguyên tố lửa/băng tạo ra đạn đa hiệu ứng cực mạnh.

2. CÁC LOẠI CÂY QUAN TRỌNG:
- SunFlower (50 Nắng): Cây tạo mặt trời. Ban ngày có nắng rơi tự nhiên, nhưng BAN ĐÊM BẮT BUỘC phải trồng nhiều SunFlower vì trời không tự rơi nắng.
- PeaShooter (100 Nắng): Cây bắn đậu cơ bản, thích hợp giữ hàng đầu trận.
- WallNut (50 Nắng, 4000HP): Khiên chắn câu giờ siêu hạng, chặn đứng bước tiến zombie để hàng sau xả đạn.
- Squash (50 Nắng): Nhảy bổ đè bẹp 1 zombie gây sát thương hủy diệt tức thì. Rất hiệu quả để diệt khẩn cấp Buckethead, Gargantuar hoặc zombie sắp vào nhà.
- TorchWood (175 Nắng): Đuốc lửa biến đạn đậu thường thành đạn lửa gây x2 sát thương. ĐẶC BIỆT: sưởi ấm và giải trạng thái đóng băng 'Cold' cho các cây lân cận!
- SnowKing (175 Nắng): Cây hỗ trợ buff toàn sân, kích hoạt trạng thái cộng hóa (intensified=true).
- MiaoMiao (50 Nắng): Ký sinh lên zombie mục tiêu, rút 1% máu mỗi giây và hồi máu ngược lại cho cây phe mình.
- HypnoShroom: Nấm thôi miên khiến zombie ăn phải quay ngược lại tấn công đồng bọn.

3. CÁC LOẠI ZOMBIE NGUY HIỂM & CÁCH ĐỐI PHÓ:
- Zombie Thường (270HP): Dễ tiêu diệt bằng 1-2 PeaShooter.
- Zombie Côn (Conehead - 560HP): Nón bảo hộ tăng giáp, cần 2 hàng bắn đậu hoặc 1 WallNut cản đường.
- Zombie Đội Xô (Buckethead - 1370HP): Rất trâu bò, cần đạn lửa TorchWood, Squash hoặc tập trung hỏa lực lớn.
- Zombie Băng Tuyết (SnowZombie): Đóng băng cây (trạng thái Cold), làm cây giảm 50% tốc độ bắn và mất 8 HP/giây. Cách khắc chế duy nhất: Đặt TorchWood gần đó để tỏa nhiệt rã băng!
- Zombie Khổng Lồ (Gargantuar): Máu cực trâu, đập nát cây trong 1 chùy. Cần kết hợp làm chậm, ném Squash, bom và xả đạn liên tục từ xa.

YÊU CẦU TRẢ LỜI:
- Luôn giữ giọng điệu hóm hỉnh, thân thiện, mở đầu bằng phong cách Crazy Dave (ví dụ: "Wabby Wabbo!").
- Trả lời bằng tiếng Việt, ngắn gọn, súc tích, đi thẳng vào trọng tâm (khoảng 2-5 câu hoặc gạch đầu dòng rõ ràng).
- Nếu người chơi hỏi về mẹo vượt ải, hãy tư vấn chiến thuật 3 lớp: Hậu cần (Sunflower) cột 1-2 -> Hỏa lực (Peashooter, Torchwood) cột 3-4 -> Phòng thủ (WallNut) cột 5-6."""
```

#### Hàm xử lý chính:
```python
import logging
from google.genai import types
from src.models import QARequest, QAResponse

logger = logging.getLogger(__name__)

async def answer_gameplay_qa(request: QARequest) -> QAResponse:
    """Xử lý câu hỏi đáp về cơ chế game, cây trồng, zombie và chiến thuật."""
    try:
        user_question = request.question.strip()
        if not user_question:
            return QAResponse(
                answer="Wabby Wabbo! Anh bạn chưa nhập câu hỏi kìa! Hãy hỏi tôi về cây, zombie hay cách ghép cây nhé!",
                status="ok"
            )

        logger.info("❓ Người chơi hỏi QA: %s", user_question)

        prompt = (
            f"{DAVE_QA_SYSTEM_PROMPT}\n\n"
            f"--- CÂU HỎI CỦA NGƯỜI CHƠI ---\n"
            f"{user_question}\n\n"
            f"Hãy giải đáp tận tình theo phong cách Crazy Dave / Bách khoa toàn thư PvZ:"
        )

        config = types.GenerateContentConfig(
            automatic_function_calling=types.AutomaticFunctionCallingConfig(
                disable=True
            ),
            temperature=0.35,
        )

        response = await gemini_client.aio.models.generate_content(
            model=GEMINI_MODEL,
            contents=prompt,
            config=config,
        )

        raw_text = response.text or ""
        answer_text = raw_text.strip()

        if not answer_text:
            answer_text = get_fallback_qa_answer(user_question)

        logger.info("💡 AI QA trả lời thành công (độ dài: %d)", len(answer_text))
        return QAResponse(answer=answer_text, status="ok")

    except Exception as exc:
        logger.error("❌ Lỗi khi sinh câu trả lời QA: %s", exc)
        fallback = get_fallback_qa_answer(request.question)
        return QAResponse(answer=fallback, status="ok")
```

#### Hàm Dự Phòng Thông Minh Khi Mất Mạng/Lỗi API:
```python
def get_fallback_qa_answer(question: str) -> str:
    """Câu trả lời dự phòng khi mất kết nối mạng hoặc lỗi server."""
    q = question.lower()
    if any(k in q for k in ["fusion", "ghép", "lai", "kết hợp"]):
        return (
            "Wabby Wabbo! Trong PvZ Fusion, cơ chế ghép cây cực kỳ thú vị: Bạn chỉ cần trồng "
            "một cây lên trên một cây tương thích khác trên cùng một ô đất! "
            "Ví dụ: Ghép WallNut với TorchWood sẽ tạo ra FireWallNut - vừa trâu bò vừa phản sát thương lửa thiêu đốt zombie đấy!"
        )
    elif any(k in q for k in ["đuốc", "torchwood", "băng", "lạnh", "cold", "đóng băng"]):
        return (
            "Wabby Wabbo! Cây Đuốc Lửa (TorchWood) là 'thần hộ mệnh' khi gặp zombie băng! "
            "Nó không chỉ biến đạn đậu thành đạn lửa gấp đôi sát thương, mà còn tỏa hơi ấm giải trạng thái đóng băng "
            "(Cold) cho tất cả cây trồng xung quanh. Đừng quên đặt nó trước hàng bắn đậu nhé!"
        )
    elif any(k in q for k in ["nắng", "mặt trời", "sun", "tiết kiệm", "ban đêm"]):
        return (
            "Bí quyết của Dave đây: Ban ngày hãy trồng ngay 1-2 hàng Hướng Dương ở cột 1 và 2 sát nhà. "
            "Còn màn ban đêm thì không có nắng tự nhiên rơi xuống đâu, bắt buộc phải trồng Hướng Dương thật sớm "
            "và dùng WallNut câu giờ ở phía trước để tích lũy tài nguyên!"
        )
    elif any(k in q for k in ["xô", "bucket", "gargantuar", "khổng lồ", "zombie mạnh"]):
        return (
            "Gặp Zombie Xô hay Khổng Lồ ư? Hãy chuẩn bị Quả Bí Ép (Squash) hoặc nâng cấp đạn lửa với TorchWood! "
            "Squash có thể đè bẹp ngay 1 mục tiêu nguy hiểm, còn WallNut sẽ giúp kìm chân chúng để dàn bắn phía sau dồn sát thương!"
        )
    return (
        "Wabby Wabbo! Dave Điên có lời khuyên vàng cho anh bạn đây: Hãy luôn dàn trận theo 3 lớp chuẩn chiến thuật:\n\n"
        "1. Lớp hậu cần: Hướng Dương ở cột 1-2 sát nhà để cấp nắng an toàn.\n"
        "2. Lớp hỏa lực: PeaShooter và TorchWood ở cột 3-4 để xả đạn lửa.\n"
        "3. Lớp phòng ngự: WallNut ở cột 5-6 để chặn đứng zombie.\n\n"
        "Nếu zombie áp sát bất ngờ, hãy dùng ngay Squash để giải nguy!"
    )
```

---

## 4. HƯỚNG DẪN KIỂM THỬ ENDPOINT (TESTING GUIDE)

### 4.1. Kiểm thử bằng cURL:
```bash
curl -X POST "http://localhost:8000/qa" \
     -H "Content-Type: application/json" \
     -d "{\"question\": \"Cách khắc chế Zombie Băng?\"}"
```

### 4.2. Kiểm thử bằng Python TestClient / Requests:
```python
import requests

url = "http://localhost:8000/qa"
payload = {
    "question": "Cơ chế ghép cây Fusion?",
    "session_id": "test_session"
}

response = requests.post(url, json=payload)
print("HTTP Status:", response.status_code)
print("Response JSON:", response.json())
```

### 4.3. Kiểm thử qua Swagger UI:
1. Mở trình duyệt: `http://localhost:8000/docs`
2. Tìm đến mục **AI Advisor** -> `POST /qa`
3. Bấm **Try it out** -> Nhập JSON và bấm **Execute**.

---

## 5. THIẾT LẬP MÔI TRƯỜNG & KHỞI CHẠY SERVER

### 5.1. File môi trường `.env`
Đảm bảo file `.env` tại thư mục gốc của server chứa API key của Google Gemini:
```env
GEMINI_API_KEY=AIzaSy...your-gemini-key...
GEMINI_MODEL=gemini-2.5-flash
LOG_LEVEL=INFO
```

### 5.2. Khởi chạy Server
```powershell
# Di chuyển vào thư mục server
cd d:\HVKTMM_TL\Nam_5\Ki_1\Dot_1\PTGTAR\AI_Agent\pvz-2d-game-ai-advisor

# Kích hoạt môi trường ảo
.\.venv\Scripts\activate

# Chạy máy chủ Uvicorn
uvicorn src.main:app --host 0.0.0.0 --port 8000 --reload
```

---

## 6. LƯU Ý KHI KẾT NỐI VỚI UNITY CLIENT
1. **Cổng mặc định:** Unity Client kết nối tới `http://localhost:8000/qa`. Nếu chạy server trên máy khác trong mạng LAN, đổi IP trong `AIQAManager.cs` (`qaEndpointUrl = "http://<LAN_IP>:8000/qa"`).
2. **CORS:** Server đã kích hoạt middleware CORS `allow_origins=["*"]` nên Unity WebGL hoặc thiết bị ngoài mạng đều có thể kết nối mà không bị chặn.
3. **Chế độ Offline Mock:** Unity Client có tích hợp cờ `useMockFallback = true`. Nếu server tạm tắt, client vẫn tự động đưa ra các câu trả lời cơ bản mà không làm đơ game hay báo crash.
