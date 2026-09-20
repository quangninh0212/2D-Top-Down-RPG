# BÁO CÁO: TRẠNG THÁI GAME OVER / WIN, CÁC CẤP ĐỘ VÀ NPC THÔNG MINH

**Trò chơi:** SOULBOUND GATE — game nhập vai hành động 2D nhìn từ trên xuống, nền tảng Android
**Nhóm thực hiện:** Quang Ninh và Hong Phong
**Nhánh mã nguồn:** `feature/game-states-npc-ai`
**Công cụ:** Unity 2022.3.3f1, URP 2D, Input System, Cinemachine, giao diện dựng bằng mã (uGUI)

---

## Tổng quan mức độ đáp ứng

| Yêu cầu | Trạng thái | Thực hiện ở đâu |
|---|---|---|
| 1. Trạng thái *game over*: thông báo / hiệu ứng hình ảnh / âm thanh | **Đạt** | `GameOverUI.cs` |
| 1. Ít nhất 3 nút điều hướng, có chức năng đầy đủ, sang **màn hình riêng biệt** | **Đạt — 4 nút, 4 màn hình** | `GameOverUI.cs`, `SceneFlow.cs` |
| 2. Ít nhất 3 cấp độ khác nhau về đồ họa, cơ chế, thế giới | **Đạt — 5 cấp độ** | `Scene1`–`Scene5`, `LevelCatalog.cs` |
| 2. Lưu trạng thái sau khi hoàn thành từng cấp độ | **Đạt** | `LevelManager.cs`, `GameSaveManager.cs`, `ProfileStats.cs` |
| 3. Trạng thái *win*: có mục tiêu cụ thể | **Đạt** | Hạ Soul Warden ở màn 5 |
| 3. Thông báo / hiệu ứng hình ảnh / âm thanh khi thắng | **Đạt** | `WinOverlayUI.cs`, `VictorySequence.cs` |
| 3. Ít nhất 3 nút điều hướng sang màn hình riêng biệt | **Đạt — 4 nút, 4 màn hình** | `VictoryScreenController.cs` |
| Bổ sung: hành vi thông minh cho ít nhất 3 NPC | **Đạt — 3 loại NPC** | `SlimePackBrain`, `GrapeThrowerBrain`, `GhostAmbusherBrain` |

Ghi chú: các mục "Đạt" đều được kiểm chứng bằng bài kiểm thử tự động chạy thật trong Unity, kết quả
ở [phần cuối báo cáo](#kết-quả-kiểm-thử-tự-động).

---

## 1. Trạng thái GAME OVER

### 1a. Thông báo, hiệu ứng hình ảnh và âm thanh

Khi máu người chơi về 0, `PlayerHealth` khóa điều khiển, chạy hoạt ảnh chết, rồi sau 1,6 giây phát sự
kiện `OnPlayerDied`. Lớp giao diện trong game bắt sự kiện đó và mở `GameOverUI` **ngay trên màn hình
chơi game**, không cần chuyển cảnh.

Trên màn hình đó có:

| Thành phần | Mô tả |
|---|---|
| **Thông báo** | Dòng chữ lớn **GAME OVER** màu đỏ, kèm dòng giải thích "Lượt chơi đã kết thúc. Dữ liệu lưu đã bị xóa." |
| **Tóm tắt lượt chơi** | Màn đã tới / tổng số màn, số vàng, thời gian đã chơi |
| **Hiệu ứng hình ảnh 1** | Lớp phủ tối dần toàn màn hình |
| **Hiệu ứng hình ảnh 2** | Dòng chữ phóng to rồi thu về đúng cỡ khi hiện ra |
| **Hiệu ứng hình ảnh 3** | Quầng sáng đỏ phía sau nhấp nháy theo nhịp thở, chạy liên tục |
| **Hiệu ứng âm thanh** | `GameSfx.GameOver` — một hợp âm rải đi xuống (392 → 311 → 262 → 196 Hz), sinh bằng mã trong `PlaceholderAudioGenerator` |

Mọi hiệu ứng đều dùng `Time.unscaledDeltaTime` vì lúc này thế giới đã dừng phía sau.

### 1b. Bốn nút điều hướng sang bốn màn hình riêng biệt

Các nút xếp thành 2 hàng, mỗi hàng 2 nút:

| Nút | Chức năng | Chuyển tới |
|---|---|---|
| **CHƠI LẠI** | Xóa lượt chơi cũ, bắt đầu lượt mới từ màn 1 | `Scene1` (màn chơi) |
| **TRANG CHỦ** | Dọn sạch đối tượng của lượt chơi, về trang chủ | `MainMenu` |
| **TIẾN TRÌNH** | Xem lịch sử các màn đã qua | `ProgressScene` *(màn hình mới)* |
| **CÀI ĐẶT** | Chỉnh âm lượng, bật/tắt tiếng, rung | `SettingsScene` *(màn hình mới)* |

Bốn đích đến là **bốn scene khác nhau**, đều nằm trong Build Settings — không phải bảng dán đè lên
màn chơi. Việc chuyển màn đi qua `SceneFlow.GoToScreen`, hàm này dọn người chơi, HUD và các đối tượng
sống xuyên màn trước khi nạp scene mới, nên màn hình mới không bị dính nhân vật của lượt chơi đã chết.

---

## 2. Các cấp độ chơi

### 2a. Năm cấp độ khác nhau

| Màn | Tên | Thế giới | Quái | Cơ chế riêng | Thưởng |
|---|---|---|---|---|---|
| 1 | FORGOTTEN MEADOW | Đồng cỏ sáng | 5 Blue Slime | Làm quen: chỉ có quái cận chiến | 10 vàng + mở **Bow** trong cửa hàng |
| 2 | SHADOW GROVE | Rừng tối | 5 Grape | Quái ném từ xa, phải biết né | 20 vàng + mở **Staff** |
| 3 | WHISPERING CROSSROADS | Ngã tư đá | 3 Slime + 4 Grape | Đánh hỗn hợp gần và xa cùng lúc | 25 vàng |
| 4 | HAUNTED MARSH | Đầm lầy ma | 4 Ghost | Quái mai phục, tàng hình, dịch chuyển | 30 vàng |
| 5 | GATE OF SOULS | Cổng linh hồn | Boss Soul Warden | Đánh boss nhiều giai đoạn, có thanh máu riêng | Kết thúc trò chơi |

Khác biệt là thật, không chỉ đổi màu: mỗi màn có bản đồ tile riêng, ánh sáng riêng, khung camera
riêng, loại quái riêng và vì thế lối chơi riêng. Ngoài ra mỗi màn đều có sẵn vùng cấm, rune tăng tốc,
bẫy gai và rương báu (phần việc của tuần trước).

### 2b. Lưu trạng thái sau khi hoàn thành từng cấp độ

Khi con quái bắt buộc cuối cùng của một màn chết, `LevelManager.OpenGate` chạy và ghi lại:

**Ghi vào file lưu của lượt chơi** (`SaveSystem`, định dạng JSON):

- `completed`, `gateOpen`, `rewardClaimed` của màn đó,
- `highestUnlockedLevel` tăng lên màn kế tiếp,
- danh sách đối tượng đã biến mất (quái đã chết, rương đã mở, rune đã nhặt),
- máu còn lại của những con quái chưa chết,
- máu, thể lực, vàng, vũ khí đang cầm, vị trí người chơi, tổng thời gian chơi,
- vũ khí vừa được mở bán trong cửa hàng.

Nhờ vậy người chơi thoát game giữa chừng rồi bấm **TIẾP TỤC** sẽ quay lại đúng màn, đúng chỗ, đúng số
vàng; đi ngược về màn cũ thì màn đó vẫn sạch quái, rương đã mở không mọc lại.

**Ghi vào lịch sử ngoài lượt chơi** (`ProfileStats`, lưu bằng PlayerPrefs):

- số lần đã hoàn thành từng màn,
- thời gian tốt nhất của từng màn,
- màn xa nhất từng qua, số lần phá đảo, số lần thất bại,
- kết cục của lượt chơi gần nhất.

Phân biệt hai chỗ lưu là có chủ ý: file lượt chơi bị xóa khi người chơi chết hoặc phá đảo, còn **lịch
sử thì không** — nếu không, màn hình Tiến trình sẽ trắng trơn đúng lúc người chơi muốn xem nó nhất.

---

## 3. Trạng thái WIN

### 3a. Mục tiêu để thắng

Mục tiêu rõ ràng và hiện ngay trên HUD: **đi hết 5 màn và hạ Soul Warden ở Cổng Linh Hồn**. Ở bốn màn
đầu, dòng mục tiêu trên HUD đếm số quái còn lại; ở màn boss, nó ghi "MỤC TIÊU: ĐÁNH BẠI SOUL WARDEN".
Boss chết là điều kiện duy nhất để thắng.

### 3b. Thông báo và hiệu ứng ngay trên màn hình chơi game

Ngay khi boss chết, `VictorySequence.Begin` phát âm thanh chiến thắng và bật `WinOverlayUI` **trên
chính màn hình chơi game**:

| Thành phần | Mô tả |
|---|---|
| **Thông báo** | **CHIẾN THẮNG!** cỡ lớn màu vàng kim, kèm dòng "SOUL WARDEN ĐÃ BỊ ĐÁNH BẠI" |
| **Hiệu ứng hình ảnh 1** | Chớp sáng trắng vàng phủ toàn màn hình rồi tắt dần |
| **Hiệu ứng hình ảnh 2** | Dòng chữ phóng to từ 60% lên 100% |
| **Hiệu ứng hình ảnh 3** | Quầng sáng vàng phía sau đập theo nhịp |
| **Hiệu ứng âm thanh** | `GameSfx.Victory` — hợp âm rải đi lên (523 → 659 → 784 → 1046 Hz) |

Nút bấm trên màn chơi được ẩn đi trong lúc này. Sau khoảng 2,2 giây, màn hình mờ dần và chuyển sang
màn hình chiến thắng.

### 3c. Màn hình chiến thắng với bốn nút điều hướng

Màn hình `VictoryScene` hiển thị tổng vàng, thời gian hoàn thành, thời gian tốt nhất và vàng cao nhất,
cùng bốn nút:

| Nút | Chức năng | Chuyển tới |
|---|---|---|
| **CHƠI LẠI** | Bắt đầu lượt chơi mới từ màn 1 | `Scene1` |
| **TRANG CHỦ** | Về trang chủ | `MainMenu` |
| **THÀNH TÍCH** | Xem kỷ lục và các cột mốc | `AchievementsScene` *(màn hình mới)* |
| **TIẾN TRÌNH** | Xem lịch sử từng màn | `ProgressScene` *(màn hình mới)* |

Hai màn hình sau có nút **QUAY LẠI** đưa người chơi về đúng màn hình chiến thắng vừa rời đi.

---

## Ba màn hình mới

### Màn hình TIẾN TRÌNH (`ProgressScene`)

- Bảng 5 màn: tên màn và trạng thái — **HOÀN THÀNH** kèm thời gian tốt nhất, **ĐANG CHƠI**, hoặc **CHƯA MỞ**.
- Bảng tổng kết: màn xa nhất đã qua, số lần phá đảo, số lần thất bại, kết cục lượt chơi gần nhất.
- Nút **THÀNH TÍCH** đi thẳng sang màn kỷ lục, nút **QUAY LẠI** trở về nơi đã mở nó.
- Vào được từ: trang chủ, màn hình game over, màn hình chiến thắng.

### Màn hình THÀNH TÍCH (`AchievementsScene`)

- Bảng kỷ lục: thời gian phá đảo nhanh nhất, vàng cao nhất một lượt, số lần phá đảo.
- Năm cột mốc, tự mở khóa theo lịch sử đã lưu: *Bước chân đầu tiên*, *Người mở đường*, *Kẻ diệt Soul
  Warden*, *Phú hộ*, *Tốc hành*. Cột mốc đã đạt có dấu `[x]` và chữ màu vàng, chưa đạt thì mờ đi.

### Màn hình CÀI ĐẶT (`SettingsScene`)

- Dùng lại đúng bảng cài đặt của trang chủ và menu tạm dừng: âm lượng chung, nhạc nền, hiệu ứng, rung.
- Thêm hai công tắc **Hiệu ứng âm thanh** và **Nhạc nền** (BẬT/TẮT), để tắt tiếng mà không cần vào màn chơi.
- Nút **ĐÓNG** trả người chơi về nơi đã mở màn hình này.

---

## Yêu cầu bổ sung: hành vi thông minh cho NPC

### Phần dùng chung — `NpcBrain`

Trước đây mọi quái dùng chung một script `EnemyAI`: đi lang thang ngẫu nhiên, thấy người chơi trong
tầm thì lao vào. Nay mỗi loại quái có một "bộ não" riêng kế thừa từ `NpcBrain`. Lớp nền lo phần mà
loại nào cũng cần:

| Cơ chế | Ý nghĩa trong game |
|---|---|
| **Máy trạng thái** | 4 trạng thái: *Tuần tra → Điều tra → Giao chiến → Rút lui* |
| **Tầm nhìn có vật cản** | Dùng raycast: quái **không nhìn xuyên tường**, khác hẳn kiểu chỉ đo khoảng cách |
| **Trí nhớ** | Mất dấu người chơi thì vẫn nhớ vị trí cuối trong 4 giây và đi tới đó kiểm tra, chứ không quên ngay |
| **Mạng báo động** (`NpcAlertNetwork`) | Con nào thấy người chơi sẽ **báo cho đồng bọn trong bán kính 10 đơn vị**; đồng bọn chuyển sang đi điều tra |
| **Dấu chấm than** | Hiện biểu tượng `!` trên đầu khi vừa phát hiện, để người chơi đọc được trạng thái của quái |
| **Né vật cản** | Hướng đi bị tường chắn thì tự thử lệch 25°, 50°, 75°, 90° sang hai bên thay vì húc thẳng vào tường |
| **Rút lui khi yếu** | Máu còn ≤ 1/3 thì bỏ chạy 3 giây rồi quay lại; mỗi con chỉ rút lui một lần trong đời |
| **Tôn trọng kỹ năng choáng** | Đang bị choáng thì bộ não ngừng hẳn, không lách luật |

`EnemyAI` cũ được tắt đi chứ không xóa, nên những thứ đã gắn với nó (sát thương khi chạm, kỹ năng
choáng) vẫn chạy nguyên.

### NPC 1 — Blue Slime: **đi săn theo bầy** (`SlimePackBrain`)

- Thấy người chơi là **hú gọi cả bầy**; những con trong tầm nghe bỏ tuần tra, kéo tới.
- Mỗi con giữ một **góc tiếp cận riêng** (cách nhau 115°) nên cả bầy **vây từ nhiều phía** thay vì xếp
  hàng sau lưng nhau; nếu vị trí vây rơi vào tường thì bỏ qua, lao thẳng.
- Vào sát trong 1,9 đơn vị thì ngừng vòng vèo, lao thẳng để cắn.
- Máu yếu: đổi màu xanh nhạt, lùi ra xa 3 giây, **hồi 1 máu** rồi quay lại đánh tiếp.

### NPC 2 — Grape: **ném cầm chân từ xa** (`GrapeThrowerBrain`)

- Giữ **khoảng cách ưa thích ~4,2 đơn vị**: người chơi lại gần dưới 2,6 thì **vừa lùi vừa ném**, xa
  quá thì tiến lên.
- Ở đúng tầm thì **đi ngang qua lại**, đổi chiều sau mỗi 1,5 giây, nên khó bị nhắm trúng.
- **Không ném khi bị tường chắn** — thay vào đó di chuyển để lấy lại đường ngắm. Đây là khác biệt lớn
  so với bản cũ: quái cũ ném cả vào tường.
- Máu yếu: mở rộng khoảng cách hẳn ra rồi mới ném tiếp.

### NPC 3 — Ghost: **mai phục và dịch chuyển** (`GhostAmbusherBrain`)

- Khi chưa bị đánh động: **mờ 35%, đứng yên**, không tuần tra — đúng nghĩa nằm rình.
- Người chơi bước vào bán kính 5 đơn vị và có đường ngắm: **hiện hình**, hú báo, lao vào giao chiến.
- Giao chiến: giữ khoảng cách ~3,6 đơn vị và bắn.
- Mất dấu hoặc người chơi chạy xa: **dịch chuyển ra sau lưng người chơi** (thử 6 hướng, chỉ chọn chỗ
  trống và nhìn thấy người chơi), kèm hiệu ứng vòng sáng tím ở cả điểm đi lẫn điểm đến, hồi 7 giây một
  lần — thay vì lẽo đẽo chạy theo.
- Máu yếu: mờ đi và trôi ra xa.

---

## Kiến trúc và danh sách file

### File mới

| File | Vai trò |
|---|---|
| `Scripts/UI/WinOverlayUI.cs` | Thông báo và hiệu ứng thắng trên màn chơi |
| `Scripts/Menu/ScreenScaffold.cs` | Khung dùng chung của 3 màn hình mới (camera, nền, tiêu đề, nút quay lại) |
| `Scripts/Menu/ProgressScreenController.cs` | Màn hình Tiến trình |
| `Scripts/Menu/AchievementsScreenController.cs` | Màn hình Thành tích |
| `Scripts/Menu/SettingsScreenController.cs` | Màn hình Cài đặt |
| `Scripts/Enemies/NpcBrain.cs` | Lớp nền cho AI của NPC |
| `Scripts/Enemies/SlimePackBrain.cs` | AI đi săn theo bầy |
| `Scripts/Enemies/GrapeThrowerBrain.cs` | AI ném từ xa |
| `Scripts/Enemies/GhostAmbusherBrain.cs` | AI mai phục, dịch chuyển |
| `Scripts/Enemies/NpcSenses.cs` | Tầm nhìn, kiểm tra chỗ trống, né vật cản |
| `Scripts/Enemies/NpcAlertNetwork.cs` | Mạng báo động và chia góc vây của bầy |
| `Editor/NpcBrainInstaller.cs` | Gắn bộ não vào 3 prefab quái |
| `Editor/StatesSmokeTest.cs` | Kiểm thử game over, lưu tiến trình, AI bầy |
| `Editor/NpcBrainSmokeTest.cs` | Kiểm thử AI ném và AI mai phục |
| `Editor/ScreenSmokeTest.cs` | Kiểm thử 3 màn hình mới |
| `Editor/Tests/GameStateTests.cs` | 11 unit test cho điều hướng, lịch sử, hình học AI |
| `Scenes/ProgressScene.unity`, `AchievementsScene.unity`, `SettingsScene.unity` | Ba màn hình mới |
| `Resources/Audio/SFX/GameOver.wav`, `NpcAlert.wav` | Hai âm thanh mới |

### File sửa

| File | Sửa gì |
|---|---|
| `UI/GameOverUI.cs` | Viết lại: hiệu ứng, tóm tắt lượt chơi, 4 nút điều hướng |
| `Menu/VictoryScreenController.cs` | Thêm 2 nút (Thành tích, Tiến trình), lấy số liệu dự phòng từ lịch sử |
| `Boss/VictorySequence.cs` | Phát sự kiện thắng ngay khi boss chết để bật hiệu ứng trên màn chơi |
| `UI/GameplayRuntime.cs` | Dựng và điều khiển lớp phủ chiến thắng |
| `Core/GameScenes.cs` | Thêm 3 scene mới vào danh sách và thứ tự build |
| `Core/SceneFlow.cs` | `GoToScreen` / `ReturnFromScreen` — vào và ra khỏi màn hình riêng |
| `Save/ProfileStats.cs` | Lịch sử từng màn, số lần thất bại, kết cục lượt gần nhất |
| `Save/GameSaveManager.cs` | Ghi lại lượt chơi thất bại vào lịch sử |
| `Levels/LevelManager.cs` | Ghi lịch sử khi qua màn và khi hạ boss; reset mạng báo động mỗi màn |
| `Menu/MainMenu.cs` | Thêm nút TIẾN TRÌNH |
| `Misc/GameplaySprites.cs` | Vẽ biểu tượng `!` cho NPC |
| `Audio/GameSfx.cs`, `Editor/PlaceholderAudioGenerator.cs` | Hai âm thanh mới |
| `Editor/LevelSceneBuilder.cs` | Tạo 3 scene màn hình mới (không đụng scene đã có) |
| `Editor/ProjectValidator.cs` | Kiểm tra 3 màn hình và 3 bộ não NPC |
| `Editor/SoulboundGateAndroidSetup.cs` | Gọi bước gắn bộ não NPC |

---

## Kết quả kiểm thử tự động

Toàn bộ chạy bằng Unity 2022.3.3f1 ở chế độ batch trên nhánh `feature/game-states-npc-ai`.

| Bộ kiểm thử | Nội dung | Kết quả |
|---|---|---|
| Unit test (EditMode) | 36 test, trong đó 11 test mới | **36 / 36 đạt** |
| `ProjectValidator` | 12 scene trong build, 3 màn hình có controller, 3 prefab quái có bộ não | **Đạt hết** |
| `StatesSmokeTest` | Game over, lưu tiến trình, AI bầy (chạy thật trong Scene1) | **Đạt hết** |
| `NpcBrainSmokeTest` (Grape) | AI ném từ xa trong Scene2 | **Đạt hết** |
| `NpcBrainSmokeTest` (Ghost) | AI mai phục trong Scene4 | **Đạt hết** |
| `ScreenSmokeTest` | Ba màn hình mới và đường quay lại | **Đạt hết** |
| `MenuSmokeTest` | Trang chủ (hồi quy) | **Đạt hết** |
| `GameplaySmokeTest` | HUD và màn chơi (hồi quy) | **Đạt hết** |
| `MechanicsSmokeTest` | Cơ chế tuần trước (hồi quy) | **Đạt hết** |
| `TransitionSmokeTest` | Chuyển màn (hồi quy) | **Đạt hết** |
| `AndroidBuilder` | Build file cài đặt Android | **Thành công** |

### Vài số liệu do máy ghi lại trong lúc chạy

**Game over và lưu tiến trình** (`StatesSmokeTest`, chạy thật trong Scene1):

- Dọn sạch quái → cổng mở, màn được đánh dấu hoàn thành, `highestUnlockedLevel` lên 2.
- Lịch sử ghi nhận màn 1 đã qua, kèm thời gian; màn xa nhất = 1.
- Người chơi chết → màn hình game over hiện lên, tìm thấy **4 / 4 nút** điều hướng, cả 4 đích đến đều
  là scene có trong build.
- Số lần thất bại trong lịch sử: 0 → 1; kết cục lượt gần nhất được ghi là "thua".

**AI đi săn theo bầy:**

- 5/5 slime trong Scene1 mang bộ não mới, mỗi con một góc vây riêng, `EnemyAI` cũ đã tắt.
- Con ở xa 13 đơn vị vẫn đang tuần tra (không đuổi theo khi chưa thấy).
- Đưa người chơi tới sát 1 con → con đó chuyển sang giao chiến, phát báo động, **3 đồng bọn khác lập
  tức phản ứng** theo lời gọi.

**AI ném từ xa:** người chơi áp sát 1,20 đơn vị → sau hơn một giây quái đã lùi ra **2,64** đơn vị, vẫn
ném được 2 lần khi có đường ngắm; đưa người chơi ra khỏi tầm nhìn thì **số lần ném không tăng thêm**
và quái thôi giao chiến.

**AI mai phục:** ghost chờ ở trạng thái mờ (alpha 0,35), chưa hiện hình; người chơi bước vào 2,5 đơn
vị → **hiện hình (alpha 1,00) và giao chiến**; người chơi chạy ra 7 đơn vị → quái **dịch chuyển 1
lần** và khoảng cách rút còn **3,00** đơn vị.

**Ba màn hình mới:** màn Tiến trình liệt kê đủ 5 màn (màn đã qua hiện "HOÀN THÀNH 01:04", màn chưa
chơi hiện "CHƯA MỞ"), đếm đúng 1 lần thất bại; màn Thành tích liệt kê đủ 5 cột mốc và đánh dấu đúng
cột mốc đã đạt; màn Cài đặt có đủ thanh trượt và hai công tắc; bấm ĐÓNG thì quay về đúng trang chủ.

### Một lỗi đã tìm ra và sửa trong quá trình kiểm thử

Lần chạy đầu tiên, **không NPC nào nhìn thấy người chơi**. Nguyên nhân: mỗi màn có một collider hình
đa giác cỡ cả bản đồ dùng để giới hạn camera; tia kiểm tra tầm nhìn xuất phát từ bên trong nó nên
Unity luôn báo "có vật cản". Đã sửa bằng hai việc: bỏ qua collider của bộ camera, và tắt tùy chọn
"tính cả collider chứa điểm xuất phát" trong lúc bắn tia. Sau khi sửa, cả ba loại NPC đều hoạt động
đúng như thiết kế.

---

## Hướng dẫn kiểm tra bằng tay

| Kiểm tra | Cách làm | Kết quả mong đợi |
|---|---|---|
| Game over | Để nhân vật chết | Chữ GAME OVER đỏ, quầng đỏ nhấp nháy, có tiếng, có tóm tắt lượt chơi |
| 4 nút game over | Bấm lần lượt từng nút | CHƠI LẠI vào màn 1; TRANG CHỦ về trang chủ; TIẾN TRÌNH và CÀI ĐẶT mở màn hình riêng |
| Lưu theo màn | Qua màn 1, thoát hẳn game, mở lại, bấm TIẾP TỤC | Vào đúng màn 2, giữ nguyên vàng và máu |
| Không mọc lại quái | Qua màn 2 rồi đi ngược về màn 1 | Màn 1 vẫn sạch quái, cổng vẫn mở |
| Lịch sử tiến trình | Trang chủ → TIẾN TRÌNH | Màn đã qua hiện HOÀN THÀNH kèm thời gian; có số lần thất bại |
| Thắng | Hạ Soul Warden ở màn 5 | Chớp sáng vàng, chữ CHIẾN THẮNG!, có nhạc; sau ~2 giây sang màn hình chiến thắng |
| 4 nút màn thắng | Bấm THÀNH TÍCH rồi QUAY LẠI | Mở màn hình kỷ lục rồi trở về đúng màn hình chiến thắng |
| NPC bầy (màn 1) | Đứng cho 1 slime thấy | Hiện dấu `!`, cả bầy kéo tới **từ nhiều phía** |
| NPC bầy rút lui | Đánh 1 slime gần chết | Nó đổi màu nhạt, lùi ra, lát sau quay lại |
| NPC ném (màn 2) | Chạy sát 1 con Grape | Nó vừa lùi vừa ném, đi ngang qua lại |
| NPC ném sau tường | Đứng nấp sau tường | Nó ngừng ném và di chuyển để lấy đường ngắm |
| NPC ma (màn 4) | Quan sát ghost từ xa | Nó mờ và đứng im cho tới khi mình lại gần |
| NPC ma dịch chuyển | Lại gần rồi bỏ chạy | Nó biến mất kèm vòng sáng tím và hiện ra sau lưng mình |
