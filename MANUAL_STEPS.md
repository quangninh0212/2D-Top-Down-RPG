# MANUAL_STEPS — những việc bạn cần tự làm

Mục tiêu là danh sách này càng ngắn càng tốt. Toàn bộ code, scene, prefab, audio,
app icon, Build Settings và Player Settings đã được tạo và cấu hình tự động.

## Bắt buộc

**Không có.** Project đã compile sạch, 9 scene đã vào Build Settings đúng thứ tự,
và APK đã được build bằng script (xem `GAME_IMPLEMENTATION_REPORT.md` mục kết quả build).

Chỉ cần: mở project bằng **Unity 2022.3.3f1**, mở `Assets/Scenes/SplashScene.unity`, bấm **Play**.

## Nếu bạn thay đổi nội dung và cần dựng lại

Chạy lại menu trong Unity Editor:

| Menu | Việc nó làm |
|---|---|
| `Tools > Soulbound Gate > Complete Android Game Setup` | Chạy toàn bộ: sinh audio + icon + art library + boss prefab, dựng lại 9 scene, đặt Build Settings và Player Settings |
| `Tools > Soulbound Gate > Validate Project` | Kiểm tra số quái mỗi màn, cổng, camera anchor, persistent id, save/economy/unlock |
| `Tools > Soulbound Gate > Build Android APK` | Build ra `Builds/Android/SoulboundGate.apk` |
| `Tools > Soulbound Gate > Debug > Menu Smoke Test` | Vào Play Mode ở MainMenu và báo cáo thứ tự vẽ, các nút, nhân vật — dùng để bắt lỗi kiểu "nền che mất menu" |
| `Tools > Soulbound Gate > Debug > Check Spawn Points` | Kiểm tra mọi điểm spawn và điểm đến của cổng có bị kẹt trong tường/vật cản không |
| `Tools > Soulbound Gate > Debug > Map Walkable Area` | Vẽ bản đồ vùng đi được của từng màn và kiểm tra mọi quái/cổng đều tới được |
| `Tools > Soulbound Gate > Debug > Gameplay Smoke Test` | Vào Play Mode ở Scene1, chuyển sang Scene2, kiểm tra EventSystem/HUD/singleton còn sống sau khi đổi màn |
| `Tools > Soulbound Gate > Debug > Transition Smoke Test` | Đi hết chuỗi Scene1→2→3→4→5 qua đúng các cổng, kiểm tra nhân vật đáp xuống chỗ trống ở mỗi màn, và thử nhốt nhân vật vào tường để xác nhận cơ chế tự cứu hoạt động |

Tool được viết **idempotent**: bấm lần thứ hai không tạo object trùng lặp.

## Lưu ý quan trọng

1. **Unity version thực tế của máy bạn là 2022.3.3f1**, không phải 2022.3.31f1 như trong đề bài.
   Mọi thứ đã được viết và kiểm thử trên 2022.3.3f1. Nếu bạn nâng lên 2022.3.31f1,
   hãy chạy lại `Validate Project` một lần để chắc chắn.

2. **Scene1 và Scene2 gốc đã được sao lưu** tại `Assets/Scenes/Backup/`
   (`Scene1_backup.unity`, `Scene2_backup.unity`). Bản sao lưu chỉ được tạo một lần
   nên chạy lại tool không ghi đè bản gốc của bạn.

3. **Unity splash mặc định không thể tắt** với Unity Personal license.
   Đề bài yêu cầu không dùng hack để bypass — nên project giữ splash bắt buộc,
   và `SplashScene` của game chạy ngay sau đó.

4. **Keystore**: build hiện dùng debug keystore (`useCustomKeystore = false`),
   đủ cho bài tập lớn. Nếu cần đưa lên Google Play thì mới phải tạo keystore riêng
   trong `Player Settings > Publishing Settings`.

5. **Audio là do script tự sinh** (`Assets/Editor/PlaceholderAudioGenerator.cs`),
   dạng chiptune/noise đơn giản, hoàn toàn không có bản quyền bên thứ ba.
   Nếu muốn thay bằng nhạc thật, đặt file `.wav`/`.mp3` vào
   `Assets/Resources/Audio/Music/` và `Assets/Resources/Audio/SFX/`
   với **đúng tên** như enum (`Menu.wav`, `Level.wav`, `Boss.wav`, `Victory.wav`,
   `SwordSwing.wav`, `CoinPickup.wav`, ...). `AudioManager` load theo tên nên không cần sửa code.

## Tuỳ chọn — nếu muốn tinh chỉnh thêm

- **Vị trí quái / bố cục map** của Scene3/4/5: sửa toạ độ trong
  `Assets/Editor/LevelSceneBuilder.cs` (hàm `BuildScene3/4/5`) rồi chạy lại setup,
  hoặc kéo thả trực tiếp trong Editor (thay đổi thủ công sẽ bị ghi đè nếu chạy lại tool).
- **Độ khó boss**: `BossPrefabBuilder.BossHealth` (hiện 42 HP) và các tham số
  trong `BossController` (số projectile, cooldown, telegraph).
- **Bán kính auto-aim**: `PlayerAimController.autoTargetRadius` trên prefab Player.
