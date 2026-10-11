Plant là lớp cơ sở của cây, mọi loại cây đều phải là lớp con của nó.
MultiImagePlant kế thừa từ Plant; cây làm bằng animation xương nhiều ảnh thì kế thừa lớp này, còn cây dùng animation chuỗi khung hình một ảnh chỉ cần kế thừa Plant.

Những điều bắt buộc phải cân nhắc khi kế thừa Plant:
1. Cân nhắc xem hàm intensify_specific có cần ghi đè không, và nhớ cài đặt năng lực sau khi được tăng cường vào cây.
2. Cân nhắc xem hàm cold có cần ghi đè không, ví dụ Bí Ngòi khi bị lạnh thì không được chậm lại nên phải ghi đè.