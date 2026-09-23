# Nguồn của báo cáo đồ án

Thư mục này chứa phần chữ và công cụ sinh ra tệp
`BAO_CAO_DO_AN_SOULBOUND_GATE.docx` ở thư mục gốc dự án.

| Tệp | Vai trò |
|---|---|
| `noi-dung.txt` | Toàn bộ nội dung báo cáo, ở dạng văn bản thuần để dễ sửa |
| `tao-bao-cao.ps1` | Sinh tệp Word từ `noi-dung.txt` và ảnh trong `Docs/Screenshots` |

## Sửa nội dung rồi sinh lại

Mở `noi-dung.txt`, sửa chữ, rồi chạy trong PowerShell:

```powershell
.\Docs\BaoCao\tao-bao-cao.ps1
```

Tệp Word ở thư mục gốc sẽ được ghi đè.

## Cú pháp của `noi-dung.txt`

Mỗi dòng bắt đầu bằng một mã, ngăn cách với nội dung bằng dấu `|`:

| Mã | Ý nghĩa |
|---|---|
| `H1|...` | Tiêu đề chương (tự sang trang mới, vào mục lục) |
| `H2|...` | Tiêu đề mục (vào mục lục) |
| `P|...` | Một đoạn văn |
| `B|...` | Một gạch đầu dòng |
| `FIG|tên-ảnh.png|chú thích` | Chèn ảnh từ `Docs/Screenshots` kèm chú thích, tự đánh số |
| `TBLCAP|chú thích` | Bắt đầu một bảng, kèm chú thích |
| `TBLW|0.3,0.2,0.5` | Tỉ lệ bề rộng các cột của bảng đó |
| `TBLH|Cột A;Cột B;Cột C` | Hàng tiêu đề |
| `TBLR|ô 1;ô 2;ô 3` | Một hàng dữ liệu, lặp lại bao nhiêu hàng tuỳ ý |

Trang bìa, các danh mục, bảng từ viết tắt và danh sách tài liệu tham khảo nằm
trong `tao-bao-cao.ps1`.

## Lưu ý khi mở bằng Word

Mục lục, danh mục hình vẽ và danh mục bảng biểu là **trường tự động** của Word.
Word thường tự điền khi mở tệp. Nếu vẫn thấy dòng nhắc thay cho danh mục, bấm
**Ctrl+A** rồi **F9** để cập nhật, sau đó lưu lại.

Định dạng đã đặt sẵn theo chuẩn thường dùng: khổ A4, lề trái 3 cm và ba lề còn
lại 2 cm, phông Times New Roman cỡ 13, giãn dòng 1,5, đánh số trang ở giữa chân
trang và không đánh số ở trang bìa.
