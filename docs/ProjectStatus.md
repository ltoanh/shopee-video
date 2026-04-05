# Trạng thái Dự án: Shopee Affiliate Automation Worker

## 1. Tài liệu mô tả dự án
Dự án là một **C# Worker Service** chạy ngầm, tự động hóa quy trình tạo nội dung cho Shopee Affiliate. 

### Quy trình cốt lõi:
1. **Lấy Keyword**: Đọc từ Google Sheets (ngẫu nhiên để đa dạng hóa).
2. **Crawl Video**: Tìm kiếm và tải video từ Douyin/Xiaohongshu dựa trên keyword tiếng Trung. **Kiểm tra trùng lặp** bằng cách đối chiếu link video với các bản ghi cũ trong sheet `Log`.
3. **Xử lý Video**: Kiểm tra thời lượng và trích xuất hình ảnh đặc trưng bằng FFmpeg.
4. **Tìm sản phẩm (Pending)**: Sử dụng Google Lens để tìm link Shopee tương ứng từ hình ảnh.
5. **Tạo Link Affiliate (Pending)**: Tự động đăng nhập Shopee Affiliate Console để chuyển đổi link gốc thành link affiliate.
6. **Báo cáo**: Ghi log kết quả, link video gốc và trạng thái vào Google Sheets.

---

## 2. Các Task đã hoàn thành
### Cơ sở hạ tầng & Cấu hình
- [x] Khởi tạo cấu trúc dự án .NET Worker Service.
- [x] Thiết lập hệ thống Logging và Dependency Injection.
- [x] Cấu hình `appsettings.json` cho Google Sheets, Automation và Cooldown.

### Google Sheets Integration (`GoogleSheetService`)
- [x] Kết nối Google Sheets API bằng Service Account.
- [x] Hàm lấy Keyword ngẫu nhiên từ sheet `KeyWord`.
- [x] Hàm cập nhật trạng thái Task (Pending -> InProgress -> Completed).
- [x] Hệ thống ghi Log tập trung vào sheet `Log`.

### Browser Automation (`CrawlService`)
- [x] Tích hợp Microsoft Playwright.
- [x] Cấu hình `BrowserProfile` để duy trì session (tránh đăng nhập lại nhiều lần).
- [x] Cơ chế Stealth để tránh bị phát hiện (AddInitScript).
- [ ] Logic kiểm tra trùng lặp video trước khi tải (Dựa trên Sheet `Log`).
- [ ] Logic tìm kiếm và tải video từ **Douyin** (Xử lý intercept media URL).
- [ ] Logic tìm kiếm và tải video từ **Xiaohongshu** (Xử lý intercept media URL).

### Xử lý Media (`VideoService`)
- [x] Tích hợp thư viện Xabe.FFmpeg.
- [x] Hàm lấy Duration của video.
- [x] Logic trích xuất N ảnh từ các mốc thời gian khác nhau trong video.

---

## 3. Các Task còn cần thực hiện
### Sprint 3: AI & Affiliate Logic (Ưu tiên cao)
- [ ] **Google Lens Integration**: Viết Service tải ảnh lên Google Lens và crawl kết quả tìm kiếm để lấy link `shopee.vn`.
- [ ] **Shopee Affiliate Service**:
    - [ ] Logic điều hướng đến trang quản trị Affiliate.
    - [ ] Tự động dán link và lấy link rút gọn (Custom Link).
    - [ ] Xử lý lọc Shop Mall/Shop yêu thích.

### Sprint 4: Hoàn thiện & Tối ưu
- [ ] **Hoàn thiện Xiaohongshu Scraper**: Đảm bảo logic crawl ổn định cho cả hai nền tảng.
- [ ] **Cải thiện độ ổn định**:
    - [ ] Xử lý Captcha (nếu có) hoặc thêm delay mô phỏng người dùng thật.
    - [ ] Retry logic khi crawl thất bại.
    - [ ] Dọn dẹp thư mục `temp_videos` và `temp_images` ngay sau khi xử lý xong mỗi video.
- [ ] **End-to-End Testing**: Kiểm tra toàn bộ luồng từ Keyword đến Link Affiliate.

---

## 4. Ghi chú kỹ thuật
- **Thư mục lưu trữ tạm**: `temp_videos/` và `temp_images/`.
- **Session Browser**: Lưu tại `BrowserProfile/`.
- **Cấu hình Google**: Yêu cầu file `google-sheets-cridential.json` trong thư mục gốc.
