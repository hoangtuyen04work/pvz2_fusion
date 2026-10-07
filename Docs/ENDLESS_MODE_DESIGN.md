# ĐẶC TẢ CHẾ ĐỘ ENDLESS — SINH TỒN VÔ HẠN

## 1. Mục đích tài liệu

Tài liệu này mô tả thiết kế gameplay, luật chơi, tiến trình độ khó, hệ thống buff/debuff, zombie Elite, boss, đổi bộ cây và bảng xếp hạng cục bộ cho chế độ **Endless — Sinh tồn vô hạn**.

Mục tiêu của chế độ là tạo ra các lượt chơi có khả năng chơi lại cao, trong đó người chơi liên tục phải lựa chọn giữa:

- Chơi an toàn và không nhận thêm sức mạnh.
- Nhận buff để hoàn thiện build nhưng bắt buộc chấp nhận một debuff.
- Điều chỉnh bộ cây sau mỗi boss để thích nghi với thử thách tiếp theo.
- Cố gắng vượt qua số màn cao nhất và cải thiện thành tích cá nhân trên bảng xếp hạng cục bộ.

Tài liệu là cơ sở chung cho thiết kế, lập trình, UI/UX, cân bằng và kiểm thử. Các con số trong tài liệu là giá trị khởi đầu và có thể thay đổi sau playtest.

---

## 2. Nền móng hiện có trong project

Project hiện đã có một phiên bản Endless cơ bản trong `Assets/Resources/Scripts/Endless/`, gồm:

- Sinh zombie theo ngân sách độ nguy hiểm.
- Mở khóa thêm loại zombie theo wave.
- Một đợt lớn sau mỗi 5 wave.
- Tăng tốc zombie theo tiến trình.
- Tính điểm từ zombie bị tiêu diệt và wave đã vượt qua.
- Lưu Top 10 cục bộ theo wave, điểm và thời gian.
- Cho phép chọn bộ cây trước khi bắt đầu.

Thiết kế trong tài liệu này mở rộng nền móng đó thành một chế độ hoàn chỉnh. Khái niệm `wave` hiện tại sẽ được nâng thành `stage` hoặc được nhóm lại theo chu kỳ 5 màn.

---

## 3. Thuật ngữ

| Thuật ngữ | Ý nghĩa |
| --- | --- |
| Lượt chơi / Run | Toàn bộ quá trình từ khi bắt đầu Endless đến khi thua hoặc thoát |
| Màn / Stage | Một trận chiến riêng biệt có điều kiện thắng rõ ràng |
| Chu kỳ / Cycle | Một nhóm 5 màn liên tiếp |
| Màn thường | Bốn màn đầu của một chu kỳ |
| Màn boss | Màn thứ 5 của mỗi chu kỳ |
| Buff | Hiệu ứng có lợi tồn tại trong lượt chơi, trừ khi mô tả ghi khác |
| Debuff đánh đổi | Hiệu ứng bất lợi nhận khi người chơi chủ động chọn buff sau màn thường |
| Debuff màn | Luật bất lợi chỉ tồn tại trong một màn |
| Debuff chu kỳ | Luật bất lợi áp dụng cho cả 5 màn của một chu kỳ |
| Elite | Zombie thường được gắn thêm một thuộc tính đặc biệt |
| Build | Tổ hợp bộ cây, buff và chiến thuật hiện tại của người chơi |

---

## 4. Trụ cột thiết kế

### 4.1. Quyết định có đánh đổi

Buff sau màn thường không miễn phí. Người chơi có thể bỏ qua, hoặc nhận sức mạnh mới và chịu một debuff tương ứng.

### 4.2. Thích nghi theo chu kỳ

Mỗi chu kỳ có một debuff riêng và tập zombie ngày càng đa dạng. Sau khi hạ boss, người chơi được chọn lại bộ cây để tránh bị khóa cứng vào một chiến thuật.

### 4.3. Độ khó đến từ cơ chế

Độ khó không chỉ tăng bằng máu và tốc độ. Elite, boss, debuff màn và debuff chu kỳ phải buộc người chơi thay đổi cách bố trí phòng tuyến.

### 4.4. Thành tích rõ ràng và có thể tái hiện

Mọi kết quả cần lưu đủ dữ liệu để xem lại và hỗ trợ debug. Bảng xếp hạng cục bộ ưu tiên thành tích tiến xa, sau đó mới xét điểm và thời gian.

---

## 5. Vòng lặp gameplay tổng thể

Một chu kỳ Endless gồm 5 màn:

1. Bắt đầu chu kỳ và công bố debuff chu kỳ.
2. Chơi màn thường 1.
3. Chọn bỏ qua hoặc nhận một cặp buff–debuff.
4. Lặp lại với màn thường 2, 3 và 4.
5. Chơi màn boss ở màn thứ 5.
6. Sau khi thắng boss, chọn một buff miễn phí trong 3 lựa chọn.
7. Xem trước debuff chu kỳ kế tiếp.
8. Chọn lại bộ cây.
9. Bắt đầu chu kỳ mới.

Luồng tổng quát:

```text
Bắt đầu lượt chơi
        ↓
Chọn bộ cây ban đầu
        ↓
Công bố debuff chu kỳ
        ↓
Màn thường × 4
        ↓
Sau mỗi màn: Bỏ qua HOẶC chọn Buff → chọn Debuff
        ↓
Màn boss
        ↓
Chọn 1/3 buff miễn phí
        ↓
Xem trước chu kỳ mới + chọn lại bộ cây
        ↓
Chu kỳ tiếp theo
```

Lượt chơi kết thúc khi zombie vượt qua phòng tuyến, một điều kiện thua đặc biệt được kích hoạt, hoặc người chơi chủ động kết thúc lượt chơi.

---

## 6. Cấu trúc màn chơi

### 6.1. Màn thường

Mỗi chu kỳ có 4 màn thường. Một màn thường bao gồm:

- Một ngân sách zombie xác định bởi số màn.
- Danh sách zombie được phép xuất hiện.
- Xác suất xuất hiện Elite.
- Không hoặc một debuff riêng của màn.
- Một hoặc nhiều đợt nhỏ tùy thời lượng mong muốn.

Mục tiêu khuyến nghị là mỗi màn kéo dài khoảng 2–4 phút. Thời lượng cần đủ để quyết định về build có ý nghĩa nhưng không khiến một lượt chơi kéo dài quá mức.

Khi tiêu diệt toàn bộ zombie, người chơi thắng màn và chuyển sang màn hình phần thưởng.

### 6.2. Màn boss

Màn thứ 5, 10, 15, 20... là màn boss. Boss phải kiểm tra khả năng của build, không chỉ là zombie có lượng máu lớn.

Boss có thể có nhiều giai đoạn:

- 100–70% máu: hành vi cơ bản.
- 70–40% máu: triệu hồi zombie hoặc kích hoạt kỹ năng mới.
- Dưới 40% máu: tăng áp lực, thay đổi hàng hoặc dùng kỹ năng đặc biệt.

Boss ở chu kỳ cao có thể dùng một hoặc nhiều cơ chế:

- Triệu hồi Elite.
- Tạo giáp theo chu kỳ.
- Tạm khóa ô trồng cây.
- Đẩy hoặc phá đội hình cây.
- Tăng tốc zombie cùng hàng.
- Đổi hàng di chuyển.
- Tạo vùng cấm hoặc hiệu ứng môi trường.

Boss nên có cơ chế tăng sức mạnh theo thời gian để tránh chiến thuật phòng thủ vô hạn. Người chơi thắng boss sẽ nhận buff miễn phí và quyền chọn lại bộ cây.

---

## 7. Hệ thống phần thưởng sau màn

### 7.1. Sau màn thường

Màn hình phần thưởng hiển thị:

- Ba thẻ buff khác nhau.
- Nút **Bỏ qua**.
- Mô tả rõ buff có hiệu lực bao lâu và ảnh hưởng tới đối tượng nào.

Nếu người chơi bấm **Bỏ qua**:

- Không nhận buff.
- Không phải nhận debuff đánh đổi.
- Chuyển thẳng sang màn kế tiếp.

Nếu người chơi chọn một buff:

1. Buff được tạm đánh dấu nhưng chưa áp dụng.
2. Game hiển thị 3 thẻ debuff khác nhau.
3. Người chơi bắt buộc chọn 1 debuff.
4. Buff và debuff được xác nhận cùng lúc.
5. Cả hai được ghi vào lịch sử lượt chơi.

Người chơi không được quay lại để đổi buff sau khi đã thấy danh sách debuff, nhằm tránh việc xem miễn phí rồi hủy lựa chọn.

### 7.2. Sau màn boss

Sau khi thắng boss:

- Hiển thị 3 buff khác nhau.
- Người chơi bắt buộc chọn 1 buff.
- Buff này không yêu cầu nhận debuff đánh đổi.
- Sau đó hiển thị debuff chu kỳ kế tiếp.
- Cuối cùng mở màn hình chọn lại bộ cây.

Phần thưởng boss nên có xác suất xuất hiện buff hiếm cao hơn màn thường.

### 7.3. Quy tắc sinh lựa chọn

Ba lựa chọn trong cùng một màn không được trùng nhau hoặc có tác dụng gần như giống nhau.

Hệ thống nên ưu tiên thẻ có ích với build hiện tại:

- Không ưu tiên buff băng khi bộ cây không có cây gây băng.
- Không đưa buff dành riêng cho cây tạo nắng nếu không có cây phù hợp, trừ khi người chơi sắp được đổi cây.
- Không đưa buff đã đạt cấp tối đa.
- Không đưa debuff không còn tác dụng thực tế để làm “lựa chọn miễn phí”.

Mỗi lựa chọn phải được tạo bằng seed của lượt chơi để có thể tái hiện khi debug hoặc khôi phục save.

---

## 8. Hệ thống buff

### 8.1. Nhóm buff

Buff có thể chia thành các nhóm:

#### Kinh tế

- Tăng lượng nắng tạo ra.
- Tăng tốc độ rơi của nắng trời.
- Nhận nắng khi hạ Elite.
- Giảm giá một nhóm cây.
- Nhận một lượng nắng ngay đầu màn.

#### Tấn công

- Tăng sát thương.
- Tăng tốc độ tấn công.
- Có xác suất xuyên mục tiêu.
- Tăng sát thương lên Elite hoặc boss.
- Tăng hiệu lực của lửa, băng hoặc hiệu ứng đặc biệt.

#### Phòng thủ

- Tăng máu cây.
- Hồi một phần máu cây sau màn.
- Tạo khiên khi cây xuống thấp máu.
- Tăng khả năng kháng hiệu ứng.
- Hồi hoặc bảo vệ máy cắt cỏ.

#### Tiện ích

- Giảm thời gian hồi thẻ.
- Tăng thời gian làm chậm.
- Thêm một ô thẻ cây.
- Cho phép đổi vị trí một cây trước khi bắt đầu màn.
- Hiển thị sớm hàng sẽ chịu đợt tấn công lớn.

### 8.2. Độ hiếm

| Độ hiếm | Vai trò | Tần suất dự kiến |
| --- | --- | --- |
| Thường | Tăng nhẹ một chỉ số | Cao |
| Hiếm | Thay đổi đáng kể build | Trung bình |
| Sử thi | Tạo cơ chế hoặc combo mạnh | Thấp |

Buff có thể có nhiều cấp, nhưng cần giới hạn thường từ 3 đến 5 cấp. Khi đạt tối đa, buff không còn xuất hiện trong danh sách lựa chọn.

### 8.3. Thời hạn

Mặc định, buff tồn tại đến hết lượt chơi. Một số buff đặc biệt có thể chỉ tồn tại trong chu kỳ hoặc màn kế tiếp, nhưng phải ghi rõ trên thẻ.

---

## 9. Hệ thống debuff

Có ba loại debuff độc lập.

### 9.1. Debuff đánh đổi

Đây là cái giá phải trả khi nhận buff sau màn thường. Mặc định debuff tồn tại đến hết lượt chơi. Debuff có thể tác động theo ba hướng: làm cây yếu đi, áp hiệu ứng bất lợi lên cây, hoặc tăng sức mạnh cho zombie.

#### Giảm sức mạnh của cây

- Giá cây tăng 5%.
- Thời gian hồi thẻ tăng 8%.
- Nắng đầu màn giảm.
- Cây gây ít sát thương hơn.
- Cây phòng thủ có ít máu hơn.
- Khả năng tạo nắng của cây bị giảm.
- Hiệu lực làm chậm, đốt cháy hoặc khống chế bị giảm.

#### Áp hiệu ứng bất lợi lên cây

- Cây vừa trồng bị giảm tốc đánh trong vài giây.
- Cây có xác suất bị choáng ngắn khi trúng đòn.
- Cây mất một phần máu khi bắt đầu màn.
- Một cây ngẫu nhiên bị khóa kỹ năng theo chu kỳ.
- Cây ở một hàng xác định bị giảm tầm đánh.
- Cây không được hồi đầy máu sau khi qua màn.

#### Tăng sức mạnh cho zombie

- Zombie tăng máu, tốc độ di chuyển hoặc tốc độ tấn công.
- Zombie nhận giáp trong vài giây sau khi xuất hiện.
- Zombie hồi một phần máu khi phá được cây.
- Elite xuất hiện thường xuyên hơn.
- Zombie trong cùng hàng nhận buff khi một Elite xuất hiện.
- Zombie kháng một phần hiệu ứng băng, lửa hoặc khống chế.

Debuff đánh đổi cần có cấp tối đa và không được tăng vô hạn. Các hiệu ứng tác động lên cùng một chỉ số phải dùng chung giới hạn để tránh làm cây hoàn toàn vô dụng hoặc khiến zombie đạt tốc độ không thể phản ứng.

### 9.2. Debuff màn

Chỉ áp dụng cho một màn và tự động biến mất khi màn kết thúc.

Ví dụ:

- Không có nắng rơi từ trời.
- Một hàng nhận nhiều zombie hơn.
- Một số ô bị khóa tạm thời.
- Zombie tăng tốc khi còn ít máu.
- Tầm nhìn bị hạn chế.
- Cây mới trồng bắt đầu với một phần máu bị thiếu.

Debuff màn cần được công bố trước khi người chơi bắt đầu màn.

### 9.3. Debuff chu kỳ

Debuff chu kỳ áp dụng cho toàn bộ 5 màn, bao gồm cả boss. Nó kết thúc sau khi boss bị hạ và được thay bằng debuff mới ở chu kỳ tiếp theo.

Ví dụ:

| Debuff chu kỳ | Cấp 1 | Cấp 2 | Cấp 3 |
| --- | --- | --- | --- |
| Nạn đói mặt trời | Nắng giảm 10% | Giảm 18% | Giảm 25% |
| Đất cằn | Khóa 1 ô | Khóa 2 ô | Ô khóa đổi theo màn |
| Cuồng nộ | Zombie thấp máu tăng 15% tốc độ | 25% | 35% |
| Băng giá | Hồi thẻ chậm 8% | 15% | 22% |
| Áp đảo | Ngân sách zombie tăng 10% | 18% | 25% |
| Cây suy yếu | Sát thương cây giảm 8% | Giảm 14% | Giảm 20% |
| Dịch bệnh | Cây bắt đầu màn mất 5% máu | Mất 10% | Mất 15% |
| Da cứng | Zombie nhận ít hơn 5% sát thương | 10% | 15% |

Debuff chu kỳ được chọn ngẫu nhiên, nhưng phải tuân theo các quy tắc:

- Không lặp lại cùng loại ở hai chu kỳ liên tiếp.
- Cấp độ phụ thuộc vào tiến trình hiện tại.
- Không chọn tổ hợp gần như không thể vượt qua với debuff màn.
- Có thể giảm xác suất của debuff khắc chế trực tiếp build hiện tại.
- Người chơi được xem debuff chu kỳ mới trước khi chọn lại cây.

---

## 10. Ghép cặp sức mạnh giữa buff và debuff

Mỗi buff và debuff có một `powerValue`. Khi người chơi chọn buff, ba debuff được sinh ra phải có sức mạnh bất lợi tương ứng.

Ví dụ thang điểm ban đầu:

- Thường: 1–2 điểm.
- Hiếm: 3–4 điểm.
- Sử thi: 5–6 điểm.

Một buff 4 điểm chỉ nên sinh debuff khoảng 3–5 điểm. Điều này ngăn trường hợp nhận buff cực mạnh và chọn một debuff không đáng kể.

Ví dụ cặp hợp lệ:

| Buff | Debuff đánh đổi |
| --- | --- |
| Cây bắn nhanh hơn 15% | Zombie tăng 10% máu |
| Nắng tạo ra tăng 20% | Giá cây tăng 10% |
| Đạn có 15% xuyên mục tiêu | Elite xuất hiện thường xuyên hơn |
| Cây phòng thủ tăng 30% máu | Hồi thẻ phòng thủ chậm hơn |
| Có thêm một ô thẻ cây | Nắng đầu màn giảm 100 |

Không cần ghép cố định từng buff với một debuff, nhưng hệ thống phải ghép theo nhóm sức mạnh và kiểm tra ảnh hưởng thực tế lên build.

---

## 11. Zombie Elite

Elite là modifier có thể gắn lên các zombie hiện có. Cách này tận dụng prefab và hành vi sẵn có, đồng thời tạo nhiều tổ hợp hơn so với việc tạo từng zombie hoàn toàn mới.

Elite cần có dấu hiệu nhận biết rõ ràng:

- Viền màu.
- Biểu tượng trên đầu.
- Hiệu ứng âm thanh khi xuất hiện.
- Màu sắc hoặc hiệu ứng riêng theo loại.

### 11.1. Danh sách Elite

#### Berserker

- Càng mất máu càng di chuyển hoặc tấn công nhanh.
- Cần giới hạn tốc độ tối đa.

#### Armored

- Giảm sát thương nhận từ phía trước.
- Có thể bị khắc chế bởi bom, đạn từ trên hoặc sát thương xuyên giáp.

#### Healer

- Hồi máu định kỳ cho zombie xung quanh.
- Không hồi máu cho Healer khác.
- Hiệu ứng hồi máu không cộng dồn vô hạn.

#### Commander

- Tăng tốc độ di chuyển hoặc tấn công cho zombie cùng hàng.
- Nhiều Commander chỉ lấy hiệu ứng mạnh nhất, không cộng dồn toàn bộ.

#### Splitter

- Khi chết sinh ra hai zombie yếu.
- Zombie con không có khả năng tiếp tục phân chia.
- Zombie con cho ít điểm hơn zombie gốc.

#### Sun Thief

- Định kỳ đánh cắp một lượng nắng hoặc lấy nắng khi tấn công cây.
- Phải có cảnh báo rõ trước khi kỹ năng kích hoạt.
- Cần giới hạn lượng nắng có thể lấy trong một lần và trong cả màn.

### 11.2. Tiến trình mở khóa Elite

| Chu kỳ | Elite có thể xuất hiện |
| ---: | --- |
| 1 | Chưa xuất hiện hoặc chỉ xuất hiện trong hướng dẫn |
| 2 | Berserker |
| 3 | Armored, Sun Thief |
| 4 | Healer |
| 5 | Commander |
| 6 trở đi | Splitter và tổ hợp Elite đa dạng hơn |

Trong giai đoạn đầu, mỗi zombie chỉ có tối đa một thuộc tính Elite. Ở cấp rất cao có thể cho xác suất nhỏ xuất hiện zombie có hai thuộc tính, nhưng phải loại bỏ các tổ hợp lỗi hoặc quá mạnh.

### 11.3. Giới hạn Elite

- Elite sử dụng nhiều điểm ngân sách hơn zombie thường.
- Có giới hạn số Elite cùng tồn tại.
- Healer và Commander cần giới hạn riêng theo hàng.
- Không sinh Elite liên tục trên cùng một hàng nếu người chơi không có thời gian phản ứng.

---

## 12. Chọn lại bộ cây sau boss

Sau mỗi màn boss, người chơi được thay đổi bộ cây để chuẩn bị cho chu kỳ mới.

Màn hình chọn cây cần hiển thị:

- Debuff chu kỳ tiếp theo.
- Các Elite đã được mở khóa.
- Buff và debuff vĩnh viễn hiện có.
- Số slot cây được phép sử dụng.
- Mô tả những cây được buff trực tiếp bởi build hiện tại.

Quy tắc đề xuất:

- Người chơi được thay toàn bộ bộ cây.
- Chỉ seed bank được cập nhật; cây đã trồng, lượng máu hiện tại và lượng nắng trên sân được giữ nguyên.
- Không tải lại gameplay scene khi chuyển sang chu kỳ mới.
- Buff đã nhận không bị mất khi đổi cây.
- Buff chưa phù hợp vẫn được lưu để có thể dùng lại sau.
- Không cho đổi cây sau từng màn thường.
- Có thể lưu 1–2 preset để thao tác nhanh.
- Nếu một debuff làm cây nào đó không thể dùng, UI phải cảnh báo trước khi xác nhận.

Trong lúc chiến đấu, HUD có nút chuyển `x1/x2`. Tốc độ được giữ qua các màn trong cùng lượt chơi,
nhưng popup phần thưởng và màn hình chọn cây luôn tạm dừng hoàn toàn. Thời gian thành tích dùng thời gian
thực đang chiến đấu, vì vậy bật `x2` không làm số giây bị nhân đôi.

---

## 13. Độ khó và tiến trình

Độ khó được tạo từ nhiều thành phần:

```text
Độ khó màn =
    ngân sách zombie
  + loại zombie đã mở khóa
  + xác suất Elite
  + cấp debuff chu kỳ
  + debuff riêng của màn
  + sức mạnh boss nếu là màn thứ 5
```

### 13.1. Ngân sách zombie

Có thể bắt đầu với công thức:

```text
baseBudget = 5 + 1.8 × stage + 0.04 × stage²
```

Sau đó nhân với:

- Hệ số màn boss.
- Hệ số debuff chu kỳ.
- Hệ số difficulty được cấu hình từ playtest.

Không nên chỉ tăng ngân sách. Từ chu kỳ cao, ưu tiên thay đổi đội hình zombie và tạo combo giữa zombie thường với Elite.

### 13.2. Nhịp tăng đề xuất

- Mỗi chu kỳ tăng ngân sách khoảng 12–18%.
- Mỗi 2 chu kỳ tăng một cấp debuff chu kỳ.
- Tỷ lệ Elite tăng chậm và có giới hạn.
- Boss ưu tiên mở thêm kỹ năng trước khi tăng mạnh lượng máu.
- Số zombie sống đồng thời phải có giới hạn để bảo vệ hiệu năng.

### 13.3. Chống tình trạng snowball

Để tránh người chơi quá mạnh hoặc quá yếu sau nhiều màn:

- Buff và debuff trùng có cấp tối đa.
- Hạn chế multiplier nhân chồng; ưu tiên cộng vào cùng một nhóm chỉ số.
- Theo dõi sức mạnh build để điều chỉnh pool lựa chọn, không âm thầm tăng chỉ số zombie ngoài luật đã công bố.
- Có thể cho phép đổi một buff không phù hợp sau mỗi 10 hoặc 15 màn.
- Có thể thêm phần thưởng hồi phục nhỏ nếu người chơi thắng boss trong tình trạng nguy hiểm.

---

## 14. Điểm số

Điểm số phải thưởng cho việc tiến xa, tiêu diệt hiệu quả và chấp nhận thử thách.

Công thức khởi đầu:

```text
Điểm màn =
    điểm tiêu diệt zombie
  + thưởng hoàn thành màn
  + thưởng máy cắt cỏ còn lại
  + thưởng nắng còn lại có giới hạn
  + thưởng boss

Điểm cuối = tổng điểm màn × hệ số thử thách
```

Hệ số thử thách có thể tăng theo:

- Số debuff đánh đổi đang sở hữu.
- Cấp debuff chu kỳ.
- Số Elite đã tiêu diệt.
- Thành tích không dùng máy cắt cỏ.

Không nên thưởng quá nhiều cho nắng còn lại vì có thể khuyến khích người chơi không sử dụng tài nguyên thay vì chơi hiệu quả.

---

## 15. Bảng xếp hạng cục bộ

Trong phạm vi hiện tại, bảng xếp hạng chỉ được lưu trên máy người chơi. Đăng nhập, tài khoản, server và đồng bộ bảng xếp hạng online chưa nằm trong kế hoạch triển khai.

### 15.1. Thứ tự xếp hạng

Thứ hạng đề xuất:

1. Màn cao nhất đã vượt qua.
2. Điểm cao nhất.
3. Nếu vẫn bằng nhau, thời gian hoàn thành ngắn hơn.
4. Nếu vẫn bằng nhau, người đạt thành tích sớm hơn đứng trước.

Boss chưa bị tiêu diệt không được tính là màn đã vượt qua.

### 15.2. Các bảng cần hỗ trợ

- Top toàn thời gian.
- Top cá nhân.
- Lịch sử một số lượt chơi gần nhất nếu cần cho việc xem lại build.

### 15.3. Thông tin hiển thị

- Thứ hạng.
- Tên người chơi.
- Màn và chu kỳ cao nhất.
- Điểm.
- Thời gian sống sót.
- Ngày đạt thành tích.
- Biểu tượng hoặc tóm tắt build nếu có không gian.

Ví dụ:

```text
#1  PlayerA   Màn 47 · Chu kỳ 10   184,500 điểm   01:42:18
#2  PlayerB   Màn 47 · Chu kỳ 10   179,200 điểm   01:38:04
#3  PlayerC   Màn 45 · Chu kỳ 9    191,000 điểm   01:31:22
```

---

## 16. Trạng thái lượt chơi và lưu tiến trình

Một lượt Endless có thể kéo dài, vì vậy nên hỗ trợ lưu giữa các màn.

Chỉ cho lưu hoặc thoát an toàn tại:

- Sau khi hoàn thành màn.
- Trước khi bắt đầu màn tiếp theo.
- Trong màn chọn buff/debuff.
- Trong màn chọn lại cây sau boss.

Không lưu giữa lúc chiến đấu trong phiên bản đầu để giảm độ phức tạp và tránh việc tải lại nhằm thay đổi kết quả trận.

Dữ liệu cần lưu:

- Stage hiện tại.
- Seed và trạng thái bộ sinh số ngẫu nhiên.
- Bộ cây.
- Buff/debuff.
- Debuff chu kỳ.
- Nắng và các tài nguyên được phép mang sang màn mới.
- Trạng thái máy cắt cỏ nếu chúng được giữ qua màn.
- Điểm, kill và thời gian.

Nếu người chơi thoát hoặc đóng game giữa trận, cần xác định rõ chính sách:

- An toàn: lượt chơi bị đánh dấu dang dở và không được ghi vào bảng thành tích.
- Linh hoạt: cho tiếp tục từ đầu màn hiện tại nhưng kết quả có cờ `resumed` trong lịch sử cục bộ.

---

## 17. UI/UX

### 17.1. HUD trong trận

HUD cần hiển thị tối thiểu:

- Màn hiện tại và vị trí trong chu kỳ, ví dụ `Màn 13 · 3/5`.
- Điểm hiện tại.
- Số zombie hoặc tiến độ đợt.
- Debuff chu kỳ đang hoạt động.
- Debuff riêng của màn.
- Biểu tượng buff/debuff vĩnh viễn; có thể mở bảng chi tiết.

### 17.2. Thẻ buff/debuff

Mỗi thẻ cần có:

- Tên.
- Biểu tượng.
- Mô tả bằng con số rõ ràng.
- Độ hiếm.
- Thời hạn hiệu lực.
- Cấp hiện tại và cấp tối đa.
- Nhóm đối tượng bị ảnh hưởng.

Không dùng mô tả mơ hồ như “tăng đáng kể”. Nên ghi cụ thể `+15% tốc độ bắn`.

### 17.3. Màn chuẩn bị chu kỳ

Trước mỗi chu kỳ, hiển thị:

- Số chu kỳ và phạm vi màn.
- Debuff chu kỳ.
- Zombie/Elite mới được mở khóa.
- Cảnh báo boss nếu đây là màn thứ 5.
- Nút xem build hiện tại.

---

## 18. Mô hình dữ liệu trong Unity

Nên dùng `ScriptableObject` cho dữ liệu nội dung để designer có thể chỉnh mà không sửa code.

### 18.1. BuffDefinition

```text
id
displayName
description
icon
rarity
category
powerValue
maxStacks
durationType
tags
effectType
effectValue
```

### 18.2. DebuffDefinition

```text
id
displayName
description
icon
debuffType: Tradeoff | Stage | Cycle
rarity
powerValue
maxStacks
minimumStage
incompatibleTags
effectType
effectValueByLevel
```

### 18.3. EliteDefinition

```text
id
displayName
icon
visualColor
minimumCycle
budgetMultiplier
maximumAlive
incompatibleZombieTags
abilityType
abilityValues
```

### 18.4. BossDefinition

```text
id
displayName
prefab
minimumCycle
baseHealth
healthScaling
phases
skills
summonPools
scoreReward
```

### 18.5. EndlessRunState

```text
localRunId
seed
currentStage
completedCycles
score
kills
duration
selectedPlantIds
buffStacks
tradeoffDebuffStacks
activeCycleDebuff
cycleDebuffLevel
runHistory
```

Runtime logic không nên tham chiếu trực tiếp tên prefab bằng chuỗi nếu có thể thay bằng ID ổn định và registry.

---

## 19. Kiến trúc hệ thống đề xuất

Có thể tách thành các thành phần:

| Thành phần | Trách nhiệm |
| --- | --- |
| `EndlessRunManager` | Quản lý toàn bộ trạng thái lượt chơi |
| `EndlessStageDirector` | Sinh stage, wave, ngân sách và đội hình zombie |
| `EndlessRewardManager` | Sinh và áp dụng lựa chọn buff/debuff |
| `EndlessModifierSystem` | Tổng hợp modifier và áp dụng vào gameplay |
| `EliteFactory` | Gắn hành vi Elite lên zombie |
| `BossController` | Điều khiển phase và kỹ năng boss |
| `EndlessDeckManager` | Chọn và thay bộ cây sau boss |
| `EndlessSaveService` | Lưu/khôi phục lượt chơi giữa các màn |
| `EndlessLeaderboard` | Lưu và hiển thị thành tích cục bộ |

Hệ thống modifier nên tính giá trị cuối cùng theo một thứ tự thống nhất:

```text
Giá trị cuối = (giá trị gốc + tổng cộng thẳng) × tổng hệ số nhân
```

Mỗi stat cần quy định giới hạn tối thiểu/tối đa để tránh cooldown bằng 0, tốc độ quá cao hoặc số âm.

---

## 20. Random và khả năng tái hiện

Mỗi lượt chơi có seed duy nhất được tạo và lưu cục bộ. Từ seed gốc có thể tạo seed con cho từng hệ thống:

- Seed sinh màn.
- Seed zombie.
- Seed Elite.
- Seed buff.
- Seed debuff.
- Seed boss.

Không nên để hiệu ứng hình ảnh tiêu thụ cùng luồng random với gameplay. Nếu không, khác biệt animation hoặc frame rate có thể làm thay đổi kết quả random.

Mọi lựa chọn quan trọng cần ghi vào lịch sử run để hỗ trợ debug, tải save và tái hiện lỗi.

---

## 21. Xử lý trường hợp đặc biệt

- Nếu không đủ 3 buff hợp lệ, giảm điều kiện lọc; tuyệt đối không khóa màn hình.
- Nếu không đủ 3 debuff hợp lệ, có thể hiển thị ít lựa chọn hơn nhưng phải ghi log cấu hình.
- Nếu thao tác ghi file thất bại, giữ trạng thái trong bộ nhớ, thông báo rõ ràng và cho phép thử lưu lại.
- Nếu game được cập nhật cân bằng lớn, lưu `gameVersion` để phân biệt thành tích giữa các phiên bản khi cần.
- Nếu một buff/debuff bị xóa khỏi phiên bản mới, hệ thống tải save phải có quy tắc migration hoặc bỏ qua an toàn.
- Nếu người chơi chọn cây không tương thích với debuff chu kỳ, UI cảnh báo nhưng không tự ý thay lựa chọn.

---

## 22. Phạm vi phát triển đề xuất

Mục tiêu hiện tại gồm gameplay cốt lõi và phần nội dung mở rộng. Đăng nhập, tài khoản, server và bảng xếp hạng online được để dành cho giai đoạn phát triển sau, ngoài phạm vi tài liệu này.

### Giai đoạn 1 — Gameplay cốt lõi

- Chu kỳ 5 màn: 4 thường + 1 boss.
- 8–12 buff.
- 8–12 debuff đánh đổi.
- 5 debuff màn.
- 4 debuff chu kỳ, mỗi loại có 3 cấp.
- 2 loại Elite: Berserker và Armored.
- 1 boss có 2–3 phase.
- Chọn lại cây sau boss.
- Bảng xếp hạng cục bộ mở rộng.

### Giai đoạn 3 — Mở rộng nội dung

- Đủ 6 loại Elite.
- Nhiều boss.
- Mở rộng pool buff và ba nhóm debuff: giảm sức mạnh cây, gây hiệu ứng xấu cho cây và buff zombie.
- Thêm nhiều debuff màn và debuff chu kỳ có cấp độ.
- Thành tựu và phần thưởng trang trí cục bộ nếu đủ thời gian.

---

## 23. Tiêu chí nghiệm thu

Phạm vi gameplay hiện tại được xem là hoàn thành khi:

- Người chơi có thể bắt đầu và kết thúc một lượt Endless độc lập với Campaign.
- Cứ 5 màn có đúng một màn boss.
- Sau màn thường, người chơi có thể bỏ qua hoặc chọn 1/3 buff rồi bắt buộc chọn 1/3 debuff.
- Sau boss, người chơi chọn 1/3 buff miễn phí.
- Debuff màn tự hết sau một màn.
- Debuff chu kỳ tồn tại đúng 5 màn và được thay sau boss.
- Người chơi xem trước debuff chu kỳ mới rồi chọn lại bộ cây.
- Elite chỉ xuất hiện sau mốc mở khóa và tuân thủ giới hạn.
- Buff/debuff được áp dụng chính xác, không nhân chồng ngoài giới hạn.
- Debuff có thể giảm chỉ số cây, áp trạng thái xấu lên cây hoặc tăng sức mạnh zombie.
- Save giữa các màn có thể khôi phục đúng build và seed.
- Kết quả được sắp xếp theo màn, điểm và thời gian.
- Toàn bộ 6 loại Elite có thể xuất hiện đúng theo tiến trình mở khóa.
- Có nhiều boss hoặc biến thể boss để các chu kỳ không lặp lại hoàn toàn.
- Khi lưu cục bộ thất bại, gameplay không bị treo và người chơi nhận thông báo rõ ràng.

---

## 24. Chỉ số cần thu thập khi playtest

- Tỷ lệ người chơi chọn buff thay vì bỏ qua.
- Buff và debuff được chọn nhiều/ít nhất.
- Stage trung bình và stage cao nhất.
- Chu kỳ có tỷ lệ thua cao bất thường.
- Loại Elite gây nhiều lượt thua nhất.
- Thời gian trung bình mỗi màn và mỗi run.
- Tỷ lệ đổi cây sau boss.
- Số build khác nhau trong Top leaderboard.
- Chênh lệch sức mạnh giữa người chơi có và không nhận buff ở đầu run.

Nếu gần như mọi người luôn chọn buff, debuff đang quá nhẹ. Nếu gần như mọi người luôn bỏ qua, buff không đủ hấp dẫn hoặc debuff quá nặng. Mục tiêu ban đầu có thể là tỷ lệ chọn buff khoảng 50–70%, tùy phong cách mong muốn.

---

## 25. Ví dụ một lượt chơi

### Chu kỳ 1 — Màn 1 đến 5

- Debuff chu kỳ: nắng tạo ra giảm 10%.
- Màn 1: gameplay cơ bản.
- Phần thưởng: người chơi chọn `+10% tốc độ bắn`, sau đó chọn `zombie +6% máu`.
- Màn 2: một hàng có nhiều zombie hơn.
- Phần thưởng: người chơi bỏ qua.
- Màn 3: Berserker được giới thiệu có kiểm soát.
- Phần thưởng: chọn `cây phòng thủ +20% máu`, nhận `hồi thẻ +8% thời gian`.
- Màn 4: đợt lớn trước boss.
- Màn 5: boss hai phase.
- Phần thưởng boss: chọn một buff miễn phí.
- Công bố chu kỳ 2: một ô đất bị khóa trong mỗi màn.
- Người chơi thay đổi bộ cây để thích nghi.

### Chu kỳ 2 — Màn 6 đến 10

- Debuff chu kỳ: một ô đất bị khóa, vị trí được báo trước.
- Armored bắt đầu xuất hiện.
- Tỷ lệ Berserker cao hơn.
- Boss màn 10 triệu hồi Elite ở phase cuối.

Ví dụ trên thể hiện nhịp mong muốn: mỗi chu kỳ thêm một vấn đề mới, nhưng người chơi luôn có thông tin và cơ hội điều chỉnh trước khi bước vào thử thách.

---

## 26. Những quyết định cần chốt sau playtest

- Cây và máy cắt cỏ có giữ nguyên trạng thái giữa các màn hay được reset.
- Nắng có được mang sang màn tiếp theo hay không.
- Debuff đánh đổi tồn tại cả run hay chỉ một số chu kỳ.
- Người chơi có bắt buộc chọn buff boss hay được bỏ qua.
- Có cho tiếp tục run sau khi đóng game giữa trận hay không.
- Có tách bảng thành tích cục bộ theo bộ cây, độ khó hoặc phiên bản không.
- Boss dùng prefab hoàn toàn mới hay nâng cấp từ zombie hiện có trong MVP.

Các quyết định này không cản trở việc xây prototype vòng lặp 5 màn, nhưng nên được chốt trước khi cân bằng và đóng gói phiên bản Endless hoàn chỉnh.
