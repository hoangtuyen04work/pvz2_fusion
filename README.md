# PvZ-Unity

## Màn Đấu trường Gargantuar

Chọn **Phiêu lưu** ở menu chính để vào màn chơi duy nhất mới. Điều khiển Peashooter bằng **WASD**, **phím mũi tên** hoặc joystick trên màn hình; nhấn **Space**, **Ctrl**, nút A trên tay cầm hoặc nút **BẮN** để bắn theo hướng đang quay.

Gargantuar có 200 máu, mỗi viên đậu gây 20 sát thương. Hạ một Gargantuar được 100 điểm; nếu nó vượt biên trái, nó sẽ trở lại từ biên phải ở vị trí dọc ngẫu nhiên và giữ nguyên lượng máu. Peashooter có ba mạng và điểm cao nhất được lưu giữa các lượt chơi.
Bản dựng lại game Plants vs. Zombies (PvZ) bằng Unity với độ tái hiện cao. Dự án mới hoàn thiện luồng chơi của các màn, chưa dựng lại toàn bộ luồng game của bản gốc. Có bổ sung một số cây, zombie và màn chơi mới do tác giả tự thiết kế; một phần tài nguyên được lấy từ trên mạng.



## Giới thiệu dự án

### Video dự án

Video giới thiệu dự án đã được đăng lên Bilibili, mời ghé trang của tác giả “落拓的狗子” để xem. Một số link video:

【Dave: Không ngờ đúng không, xe đẩy bị tôi trộm mất rồi (PVZ tự làm)】 https://www.bilibili.com/video/BV1U84y1b7i4/?share_source=copy_web&vd_source=c0fb8fd2ce069eb3b7f57c407f2e3a26

【Màn mới: Vùng Đất Bất Tử | Bóng ma lởn vởn khắp nơi, zombie biết hồi sinh】 https://www.bilibili.com/video/BV1C14y1F77b/?share_source=copy_web&vd_source=c0fb8fd2ce069eb3b7f57c407f2e3a26

【Cây mới: Mèo Miu | Rơi nước mắt trong một giây? DNA tuổi thơ của bạn có rung động không】 https://www.bilibili.com/video/BV13k4y1e7bL/?share_source=copy_web&vd_source=c0fb8fd2ce069eb3b7f57c407f2e3a26

### Ảnh chụp màn hình

![image-20231014152934799](README.assets/image-20231014152934799.png)

![image-20231014153012715](README.assets/image-20231014153012715.png)

![image-20231014153102982](README.assets/image-20231014153102982.png)



## Bắt đầu nhanh

- Tải mã nguồn, dùng Unity mở thư mục này như một project.
    - Phiên bản Unity: 6000.5.8f1
- Mở scene game Assets/Scenes/GameScene .
- Chạy project.



## Chơi mạng hai người

Vào **PHIÊU LƯU**, bấm nút **CHƠI MẠNG** ở góc trái trên bảng chọn màn. Màn đang chọn sẽ được mang
sẵn sang sảnh chờ. Một người bấm **TẠO PHÒNG**, đọc địa chỉ hiện trên
màn hình cho người kia; người kia bấm **THAM GIA**, gõ địa chỉ đó vào rồi bấm **KẾT NỐI**. Khi chủ phòng
bấm **BẮT ĐẦU**, cả hai máy cùng vào màn.

Hai chế độ:

- **Đồng đội** — hai người cùng phe trồng cây, dùng chung kho nắng và chung dãy thẻ. Zombie vẫn sinh
  theo trục thời gian trong file JSON của màn.
- **Đối kháng** — chủ phòng giữ phe Cây, người tham gia chỉ huy phe Zombie. Trục thời gian bị tắt,
  phe zombie tích **não** (10 mỗi giây, tối đa 500) rồi chọn thẻ zombie và bấm vào hàng để thả quân.
  Zombie chạm vạch cuối là phe zombie thắng; phe cây trụ hết **4 phút** là phe cây thắng.

### Cách hoạt động

Mô hình **máy chủ giữ quyền quyết định**: máy tạo phòng chạy toàn bộ logic trò chơi, máy khách chỉ gửi
thao tác lên rồi vẽ lại kết quả nhận về. Nhờ vậy hai máy không thể lệch trạng thái. Truyền tin bằng
socket TCP tự viết, mỗi gói là một dòng JSON, vị trí và máu zombie đồng bộ 10 lần mỗi giây.

Toàn bộ mã nằm trong `Assets/Resources/Scripts/Net/`:

| Tệp | Việc |
| --- | --- |
| `NetSession.cs` | Trạng thái phiên: chế độ, vai trò, các cờ phân quyền |
| `NetMessage.cs` | Danh sách loại gói tin và cấu trúc gói |
| `NetTransport.cs` | Socket TCP thô, luồng đọc và luồng ghi riêng |
| `NetManager.cs` | Bơm gói tin ra luồng chính, giữ kết nối, đo độ trễ |
| `NetLobbyUI.cs` | Sảnh chờ ở menu chính |
| `NetGameplay.cs` | Đồng bộ trong màn chơi: cây, nắng, zombie, kết quả |
| `NetZombieView.cs` | Kéo zombie ở máy khách bám theo vị trí máy chủ |
| `ZombieCommanderUI.cs` | Thanh thả quân của phe zombie |

Cổng mặc định là **7777**. Hai máy cùng mạng nội bộ thì chơi được ngay; qua Internet thì cần mở cổng
này trên router hoặc dùng phần mềm tạo mạng ảo.

### Giới hạn hiện tại

- Chỉ hai người mỗi phòng.
- Mất kết nối giữa trận thì không nối lại được, phải tạo phòng mới.
- Khối băng của Zombie Tuyết và hiệu ứng đóng băng của màn Sông Băng chỉ là hình ảnh, có thể khác
  nhau đôi chút giữa hai máy; máu và sống chết vẫn do máy chủ quyết định nên không ảnh hưởng luật chơi.



## Ghi chú dự án

- Đối tượng Game Management trong scene có component script cùng tên; tham số level của nó dùng để chọn màn chơi sẽ tải, hiện dùng được các giá trị 0 - 4 .
