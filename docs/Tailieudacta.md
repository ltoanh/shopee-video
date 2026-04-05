# TÀI LIỆU ĐẶC TẢ KỸ THUẬT: WORKER AUTOMATION SHOPEE AFFILIATE

## 1. Tổng quan quy trình
Dự án là một **C# Worker Service** chạy ngầm, thực hiện các bước:
1. Lấy video từ mạng xã hội Trung Quốc (Douyin, Xiaohongshu).
2. Trích xuất hình ảnh từ video.
3. Tìm kiếm sản phẩm tương ứng trên Shopee qua Google Lens.
4. Tự động chuyển đổi thành link Affiliate thông qua trang quản trị Shopee.
5. Lưu trữ video và log kết quả.

---

## 2. Luồng xử lý chi tiết (Workflow)

### Bước 1: Lấy dữ liệu Keyword
- Kết nối với **Google Sheets API**.
- **Cơ chế chọn:** Đọc danh sách và lựa chọn **ngẫu nhiên** một hàng (`Pending`) để xử lý, nhằm đa dạng hóa nội dung và tránh xung đột khi chạy đa luồng.

### Bước 2: Lựa chọn nền tảng Source
- Chọn ngẫu nhiên một trang mạng xã hội (Douyin, Xiaohongshu, v.v.) từ danh sách cấu hình trong `appsettings.json`.

### Bước 3: Tìm kiếm Video
- Sử dụng Keyword tiếng Trung (có sẵn trong Google Sheet) để tìm kiếm trên nền tảng đã chọn.

### Bước 4: Lọc và Tải Video
- **Tiêu chí lọc:** Chỉ chọn các video có thời lượng **< 40 giây**.
- **Tải về:** Lưu video vào thư mục tạm.
- **Trích xuất ảnh:** Sử dụng **FFmpeg** để cắt lấy 3-5 ảnh rõ nét nhất từ các mốc thời gian khác nhau.

### Bước 5: Tìm kiếm sản phẩm qua Google Lens
- Upload các ảnh đã trích xuất lên **Google Lens** kèm từ khóa `"Shopee"`.
- Thu thập các đường link `shopee.vn` từ kết quả trả về.

### Bước 6: Lấy link Affiliate (Chế độ Login)
- Với mỗi link Shopee tìm được, kiểm tra điều kiện (Ưu tiên Mall hoặc Shop Yêu thích).
- Sử dụng **Playwright** để điều hướng:
    1. Truy cập trang quản trị Affiliate (sử dụng session/cookie đã lưu).
    2. Dán link sản phẩm vào công cụ **Custom Link**.
    3. Thực hiện lấy **Link rút gọn Affiliate**.

### Bước 7: Lưu trữ và Log
- Upload video lên **Google Drive**.
- Lưu log vào Google Sheet bao gồm: Keyword, Link Affiliate, Link Google Drive.
- Cập nhật trạng thái hàng dữ liệu thành `Completed`.

### Bước 8: Nghỉ (Cooldown)
- Thực hiện lệnh chờ (Delay) ngẫu nhiên từ **2 - 5 phút** trước khi quay lại Bước 1.

---

## 3. Các điểm lưu ý kỹ thuật quan trọng

### 🔑 Quản lý Session Playwright
Vì Bước 6 yêu cầu truy cập trang quản trị đã đăng nhập, cần cấu hình `BrowserContext` để lưu trữ và tái sử dụng Cookies/Storage State (thư mục `BrowserProfile`).

### 🎬 Xử lý Video với FFmpeg
- Sử dụng thư viện `Xabe.FFmpeg` để kiểm tra thuộc tính `Duration`.
- **Lệnh cắt ảnh mẫu:**
  ```bash
  ffmpeg -i input.mp4 -vf "select=not(mod(n\,100))" -vsync vfr -q:v 2 img_%03d.jpg
  ```

### ⚡ Đa luồng & Đồng bộ
Cần có cột trạng thái (`Processing`) trên Google Sheet để tránh việc nhiều Worker chọn trùng một keyword cùng lúc.

---

## 4. Cấu trúc dữ liệu Log (Output)

| Keyword | Nguồn Video | Link Shopee Gốc | Link Affiliate | Link Google Drive | Trạng thái |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Máy pha cà phê | Douyin | `shopee.vn/product/123` | `shp.ee/abcxyz` | `drive.google.com/vid1` | Thành công |