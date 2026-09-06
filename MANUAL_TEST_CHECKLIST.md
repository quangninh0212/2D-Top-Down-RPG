# MANUAL_TEST_CHECKLIST — Soulbound Gate

Checklist để kiểm thử thủ công trong Unity Editor (Play từ `Assets/Scenes/SplashScene.unity`)
và trên thiết bị Android.

Trong Editor vẫn giữ được điều khiển PC: **WASD** di chuyển, **chuột trái** tấn công,
**Space** dash, **1/2/3** đổi vũ khí (chỉ đổi được sang vũ khí đã sở hữu).
Các nút cảm ứng vẫn hiện trong Play Mode để test bằng chuột.

> Muốn test lại từ đầu: xoá file save tại
> `%USERPROFILE%\AppData\LocalLow\Quang Ninh and Hong Phong\Soulbound Gate\soulboundgate_run.json`

| # | Kiểm thử | Kết quả mong đợi | ✔ |
|---|---|---|---|
| 1 | Mở app | Unity splash → SplashScene (SOULBOUND GATE + "Developed by Quang Ninh and Hong Phong", fade in/out) → MainMenu | ☐ |
| 2 | Ở MainMenu khi chưa từng chơi | Nút **TIẾP TỤC** mờ và không bấm được | ☐ |
| 3 | Bấm **CHƠI MỚI** | LoadingScene (tiêu đề, thanh %, mẹo chơi) → Scene1, banner "FORGOTTEN MEADOW" | ☐ |
| 3b | Ngay khi vào Scene1 | Nhân vật đứng ở chỗ trống, **đi được ngay** — không bị kẹt trong bụi/tường | ☐ |
| 3c | Nhìn góc trên trái | Máu, stamina, vàng hiển thị **đầy đủ trong màn hình**, không bị cắt hay chui ra ngoài | ☐ |
| 4 | Kéo joystick góc dưới trái | Nhân vật di chuyển đúng hướng, sprite lật trái/phải chính xác | ☐ |
| 5 | Giữ nút **TẤN CÔNG** | Đánh liên tục theo cooldown của vũ khí; trên mobile tự nhắm vào quái gần nhất | ☐ |
| 6 | Bấm **LƯỚT** | Player lướt nhanh, mất 1 stamina; stamina tự hồi sau vài giây | ☐ |
| 7 | Đếm quái trong Scene1 | Đúng **5 Blue Slime** | ☐ |
| 8 | Đi vào cổng khi còn quái | Không chuyển scene; hiện "CỔNG BỊ KHÓA - CÒN n QUÁI VẬT"; hạt cổng màu xanh lạnh | ☐ |
| 9 | Giết đủ 5 Slime | Banner "CỔNG ĐÃ MỞ!", toast "+10 VÀNG", vàng trên HUD tăng, cổng chuyển vàng ấm | ☐ |
| 10 | Về MainMenu → **CỬA HÀNG** | Bow hiện "10 Gold" + nút MUA (trước đó là "Hoàn thành Màn 1 để mở khóa.") | ☐ |
| 11 | Mua Bow | Vàng giảm 10, dòng chữ đổi thành "ĐÃ SỞ HỮU"; vào game bấm ĐỔI VŨ KHÍ chọn được Bow | ☐ |
| 12 | Scene2 | Đúng **5 Grape**, banner "SHADOW GROVE"; clear được +20 vàng và mở bán Staff | ☐ |
| 13 | Scene3 | Đúng **3 Blue Slime + 4 Grape** (7 quái), map khác hẳn Scene1/2, có tường ngăn giữa 2 khu | ☐ |
| 14 | Scene4 | Đúng **4 Ghost**, map đầm lầy có hồ nước trang trí và đuốc | ☐ |
| 15 | Scene5 | Boss **SOUL WARDEN** to gấp ~2.3 lần, tím, có quầng sáng; thanh máu lớn ở giữa trên | ☐ |
| 16 | Pause (☰) → **LƯU GAME** → **VỀ TRANG CHỦ** → **TIẾP TỤC** | Quay lại đúng scene đang chơi | ☐ |
| 17 | Sau khi Continue | Đúng vị trí, máu, stamina, vàng và vũ khí đang cầm | ☐ |
| 18 | Clear Scene1 → sang Scene2 → quay lại Scene1 | Slime **không** hồi sinh, cổng vẫn mở, **không** nhận lại 10 vàng | ☐ |
| 19 | Phá bụi/thùng rồi rời scene và quay lại | Vật đã phá không xuất hiện lại | ☐ |
| 20 | Để Player chết | Animation chết → màn GAME OVER; quay lại MainMenu thấy **TIẾP TỤC** bị mờ | ☐ |
| 21 | Game Over → **CHƠI LẠI** | Vào Scene1 hoàn toàn mới: máu đầy, 0 vàng, chỉ có Sword, 5 Slime sống lại | ☐ |
| 22 | Vào Shop khi không đủ vàng | Nút MUA bị mờ, không thể mua, vàng không âm | ☐ |
| 23 | Chỉnh âm lượng trong CÀI ĐẶT rồi thoát và mở lại app | Giá trị được giữ nguyên | ☐ |
| 24 | Đánh bại Boss | VFX + banner → VictoryScene hiện tổng vàng, thời gian, kỷ lục | ☐ |
| 25 | Cài APK lên Redmi 13 | Chạy Landscape, xoay được 2 chiều, joystick/nút không bị notch che | ☐ |

## Kiểm thử bổ sung nên làm

| # | Kiểm thử | Kết quả mong đợi | ✔ |
|---|---|---|---|
| 26 | Bấm nút Back của Android khi đang chơi | Mở/đóng menu Tạm dừng, **không** thoát game | ☐ |
| 27 | Bấm Back trong Cửa hàng/Hướng dẫn/Cài đặt ở MainMenu | Đóng panel, quay về MainMenu | ☐ |
| 27b | Bấm Back khi đang ở MainMenu gốc | Hiện hộp "Thoát game?" — **không** thoát ngay. Bấm HỦY thì ở lại | ☐ |
| 27c | Ở MainMenu, kiểm tra hiển thị | Thấy rõ logo, 6 nút, nhân vật trái/phải **nằm trên** nền động | ☐ |
| 28 | Bấm **CHƠI MỚI** khi đang có save | Hộp xác nhận "Bắt đầu trò chơi mới? Dữ liệu lưu hiện tại sẽ bị xóa." | ☐ |
| 29 | Thoát app đột ngột khi đang chơi (nút Home) | Mở lại → TIẾP TỤC vẫn hoạt động (autosave khi `OnApplicationPause`) | ☐ |
| 30 | Đi ngược từ Scene3 về Scene2 | Cổng lùi luôn mở, không cần clear lại | ☐ |
| 31 | Tắt Rung trong Cài đặt rồi bị quái đánh | Máy không rung | ☐ |
| 32 | Quan sát rìa màn hình trên máy 20:9 | Không thấy vùng trống ngoài map; Player không đi ra khỏi khung hình | ☐ |
| 33 | Sang màn mới bằng cổng (không thoát app) | **Thao tác được ngay**: joystick, tấn công, dash, pause đều ăn | ☐ |
| 34 | Đi qua nhiều màn liên tiếp (1→2→3→4) | Lần nào cũng điều khiển được, không phải khởi động lại app | ☐ |
| 35 | Nhìn HUD ở mọi màn | Máu / stamina / vàng xếp thành **3 hàng dọc** góc trên trái, không đè lên nhau | ☐ |
| 36 | Vào Scene3 | Đi lại thoải mái khắp 2 khu, không bị nhốt trong ô nhỏ | ☐ |
| 36b | Vào Scene4 và Scene5 | Đi được khắp map, không va phải tường vô hình | ☐ |
| 37 | Nhìn tán cây ở màn 1 và 2 | Tán cây chỉ còn ở rìa map, không che vùng chiến đấu | ☐ |
| 38 | Giết đủ quái ở màn 2 | Đủ 5 Grape đều tiếp cận được; cổng mở bình thường | ☐ |
