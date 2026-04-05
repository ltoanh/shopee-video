# Kế hoạch Triển khai: Shopee Affiliate Automation

Tài liệu này chi tiết lộ trình triển khai hệ thống tự động hóa tìm nguồn video và tạo link affiliate Shopee.

## 1. Cấu trúc Google Sheets
Hệ thống sử dụng 2 sheets riêng biệt để quản lý tác vụ và dữ liệu kết quả:

### Sheet: `KeyWord` (Input)
- **Cột A (KeyWord):** Tiếng Việt - Định danh sản phẩm.
- **Cột B (KeyWord_ZH):** Tiếng Trung - Dùng để tìm kiếm trên Douyin/Xiaohongshu.
- **Cột C (Status):** Trạng thái xử lý (`Pending`, `Processing`, `Completed`).

### Sheet: `Log` (Output)
- **Cột A:** Keyword (Tiếng Việt).
- **Cột B:** Nguồn Video (Douyin/Xiaohongshu).
- **Cột C:** Link Shopee Gốc (Tìm từ Google Lens).
- **Cột D:** Link Affiliate (Rút gọn từ Shopee Affiliate).
- **Cột E:** Link Google Drive (Video sau khi upload).
- **Cột F:** Trạng thái (Thành công/Thất bại).

---

## 2. Luồng xử lý (Workflow)
1. **Đọc:** Worker chọn ngẫu nhiên 1 hàng `Pending` từ sheet `KeyWord`.
2. **Khóa:** Cập nhật trạng thái thành `Processing`.
3. **Xử lý:** Chạy các Sprint (Crawl -> Extract -> Lens -> Affiliate -> Drive).
4. **Ghi Log:** Append kết quả vào sheet `Log`.
5. **Hoàn tất:** Cập nhật trạng thái `KeyWord` thành `Completed`.

---

## 3. Các Giai đoạn Triển khai (Sprints)

### Sprint 2: Thu thập & Xử lý Video
*Mục tiêu: Tự động tải video và trích xuất hình ảnh.*
- [ ] **Module Crawl:**
    - Search theo keyword tiếng Trung.
    - Thu thập link video & lọc video < 40s.
- [ ] **VideoProcessor (FFmpeg):**
    - Tải video vào thư mục tạm.
    - Trích xuất 3-5 ảnh tiêu biểu (screenshot).
    - Dọn dẹp thư mục tạm sau khi xử lý.

### Sprint 3: AI Search & Affiliate Automation
*Mục tiêu: Tìm sản phẩm từ ảnh và tạo link Affiliate.*
- [ ] **ProductSearchService:**
    - Sử dụng Playwright điều hướng Google Lens.
    - Upload ảnh và thêm keyword "Shopee" để lọc.
    - Lấy link `shopee.vn` từ kết quả.
- [ ] **ShopeeAffiliateService:**
    - Dùng `BrowserContext` (thư mục `BrowserProfile`) để giữ đăng nhập.
    - Dán link vào trang quản trị Affiliate -> Lấy link rút gọn.
- [ ] **Logic tối ưu:** Ưu tiên Shopee Mall/Yêu thích.

### Sprint 4: Lưu trữ & Hoàn thiện
*Mục tiêu: Backup video và tối ưu hệ thống.*
- [ ] **GoogleDriveService:** Upload video & lấy link Public.
- [ ] **Pipeline Integration:** Kết nối toàn bộ quy trình, xử lý Retry-logic.
- [ ] **Antidetect:**
    - Randomize User-Agent.
    - Random Cooldown (2-5 phút).
- [ ] **End-to-End Testing.**

---

## 🛠️ Công nghệ sử dụng
- **Ngôn ngữ:** C# Worker Service (.NET)
- **Automation:** [Playwright](https://playwright.dev/dotnet/)
- **Xử lý Media:** [FFmpeg](https://ffmpeg.org/) & [Xabe.FFmpeg](https://ffmpege.com/)
- **Cloud APIs:** Google Sheets v4, Google Drive v3