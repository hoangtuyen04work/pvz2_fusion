Tổng hợp các vấn đề thường gặp

Thư mục Resources dùng để chứa tài nguyên game, không được xoá hoặc đổi tên.
Hàm Resources.Load trỏ tới thư mục Resources.

Khi chỉnh ô trồng cây, nếu phóng to scene mà đường viền xanh của collider biến mất, hãy đổi toạ độ trục z về 0.
Chỉnh xong nhớ đổi toạ độ trục z về lại 1, nếu không sẽ ảnh hưởng tới việc xẻng dò tìm cây.

Khi chuyển animation, nếu đã bỏ Has Exit Time mà gọi SetBool xong animation vẫn không đổi ngay, hãy thu nhỏ khoảng chuyển tiếp (chính là thanh thời gian nằm ngay dưới tuỳ chọn Has Exit Time).
Vì sau khi bỏ Has Exit Time, animation không cần chạy tới vị trí chỉ định nữa, nhưng vẫn chuyển theo đúng thời gian chuyển tiếp đã đặt.