# PROJECT_AUDIT — Soulbound Gate

Audit thực hiện trước khi chuyển project Unity PC hiện có sang game Android hoàn chỉnh.
Mọi thông tin dưới đây lấy từ file thật trong repo (không suy đoán), một phần được xác
minh bằng Editor script `Assets/Editor/SceneDumpTool.cs` chạy ở batch mode.

## 1. Môi trường

| Mục | Giá trị thực tế |
|---|---|
| Unity version | **2022.3.3f1** (không phải 2022.3.31f1 như dự kiến) |
| Editor path | `C:\Program Files\Unity\Hub\Editor\2022.3.3f1\Editor\Unity.exe` |
| Android Build Support | **Đã cài đủ** (AndroidPlayer + SDK + NDK + OpenJDK) |
| Render pipeline | URP 14.0.8 (2D Renderer, có Light2D + Global Volume) |
| Input | Input System 1.6.3, `activeInputHandler: 2` (Both) |
| Camera | Cinemachine 2.9.7 |
| UI | uGUI + TextMeshPro 3.0.6 |
| Product Name | `Soulbound Gate` (đã đổi ở commit trước) |
| Company Name | `PhongCompany` — **cần đổi** thành `Quang Ninh and Hong Phong` |
| Bundle id | **trống** — cần đặt |
| Orientation | `defaultScreenOrientation: 4` (AutoRotation), chỉ bật Landscape Left/Right — OK |
| Android ABI | `AndroidTargetArchitectures: 1` (ARMv7) — **cần thêm ARM64 + IL2CPP** |
| Min SDK | 22 |

## 2. Scene hiện có

| Scene | Nội dung |
|---|---|
| `Assets/Scenes/MainMenu.unity` | Chỉ 1 GameObject `MainMenu` mang script `MainMenu.cs`; toàn bộ UI dựng bằng code |
| `Assets/Scenes/Scene1.unity` | Map đồng cỏ, 6 Blue Slime, Grid 4 layer, AreaExit phía Đông |
| `Assets/Scenes/Scene2.unity` | Map rừng, 6 Ghost + 2 Grape, AreaExit phía Tây (về Scene1) |

Build Settings hiện tại: `MainMenu`, `Scene1`, `Scene2`.

**Thiếu:** SplashScene, LoadingScene, Scene3, Scene4, Scene5, VictoryScene.

### Cấu trúc Grid (giống nhau ở Scene1/Scene2)

```
Grid
├── Grass       Tilemap, sortingOrder -2
├── Foreground  Tilemap, order -1, Rigidbody2D(Static) + TilemapCollider2D + CompositeCollider2D  ← tường/biên
├── Cosmetic    Tilemap, order -1 (trang trí, không collider)
└── Top         Tilemap, order 5, TilemapCollider2D + TransparentDetection + Parallax  ← tán cây phủ lên player
```

Tile asset dùng được: `Assets/Tilemap/Rule/{Cosmetic,Foreground,Top,Water}.asset` (RuleTile)
và 82 tile lẻ trong `Assets/Tilemap/Tiles/` (`grass`, `plains_0..43`, `Water Tiles_0..`, `decor_16x16_0..3`).

## 3. Prefab cốt lõi

| Prefab | Thành phần chính |
|---|---|
| `Scene Management/Player.prefab` | SpriteRenderer, PlayerController, Rigidbody2D, Animator, CapsuleCollider2D, SortingGroup, PlayerHealth, Flash, Knockback, Stamina; con: `Active Weapon` (ActiveWeapon + MouseFollow + SlashSpawnPoint), `Weapon Collider` (PolygonCollider2D + DamageSource), `Trail Renderer` |
| `Scene Management/UICanvas.prefab` | Canvas + CanvasScaler + UIFade; con: `Fade Transition`, `Active Inventory` (GridLayoutGroup + ActiveInventory, **5 slot**), `Heart Container/Health Slider`, `Gold Coin Container/Gold Amount Text`, `Stamina Container` |
| `Scene Management/Managers.prefab` | BaseSingleton; con: SceneManagement, CameraController, ScreenShakeManager (CinemachineImpulseSource), EconomyManager |
| `Scene Management/Camera.prefab` | Main Camera (CinemachineBrain) + State-Driven Camera → Virtual Camera (CinemachineConfiner2D) + `Camera Confiner` (PolygonCollider2D) + Global Volume |

## 4. Enemy

| Enemy | Prefab | Script |
|---|---|---|
| Blue Slime | `Enemies/Blue Slime.prefab` | EnemyAI + EnemyPathfinding + EnemyHealth + Knockback + Flash + PickUpSpawner |
| **Grape** | **`Enemies/Enemie1.prefab`** (xác minh bằng GUID script `Grape.cs` = `45f3a338d8f1c8f409ac0591d5a73b71`) | + `Grape.cs`, bắn `Grape Projectile` theo quỹ đạo vòng cung |
| Ghost | `Enemies/Ghost.prefab` | + `Shooter.cs` (burst/cone projectile), `RandomIdleAnimation` |

Chưa có Boss.

## 5. Vũ khí

`Sword` (dmg 1, CD 0.5), `Bow` (dmg 1, CD 0.7, range 12), `Staff` (dmg 2, CD 1.2, range 8).
`Magic Laser` là projectile của Staff, **không phải** vũ khí thứ tư. ScriptableObject nằm ở
`Assets/Scriptable Objects/{Sword,Bow,Staff}.asset`.

## 6. Những gì đã được thêm ở các commit trước (phải tái sử dụng, không tạo trùng)

- `Assets/Scripts/Mobile/` — `MobileInput` (static aim/move state), `OnScreenJoystick` (floating stick),
  `TouchAimZone` (tap-to-attack), `MobileControlsBootstrap` (dựng UI điều khiển bằng code,
  tự cài qua `[RuntimeInitializeOnLoadMethod]`, có SafeArea + debug readout).
- `Assets/Scripts/Menu/` — `MainMenu` (splash/home/settings/loading dựng bằng code),
  `MenuArt` (vẽ sprite bằng code), `MenuBackground` (nền động), `GameProgress` (PlayerPrefs `lastScene`).
- Font `Assets/Resources/Fonts/Gixel.ttf` (load bằng `Resources.Load`).

**Chưa hề có:** `SaveData.cs`, `SaveSystem.cs`, `GameSaveManager.cs`, `LoadingScreenController.cs`,
`SplashScreenController.cs`, `SettingsMenuController.cs`, AudioManager, hệ thống rung/haptic.
`GameProgress` chỉ lưu tên scene bằng PlayerPrefs — sẽ được thay bằng save system JSON thật.

## 7. Vấn đề phải sửa để Android hoạt động

| # | Vấn đề | File |
|---|---|---|
| 1 | Dash chỉ bind `<Keyboard>/space` | `Player Controls.inputactions`, `PlayerController` |
| 2 | Hướng nhân vật lấy từ `Input.mousePosition` | `PlayerController.AdjustPlayerFacingDirection` |
| 3 | Sword xoay theo chuột | `Sword.MouseFollowWithOffset` |
| 4 | Staff xoay theo chuột | `Staff.MouseFollowWithOffset` |
| 5 | Magic Laser lấy hướng từ chuột | `Magic Laser.cs` |
| 6 | `MouseFollow` bám chuột | `MouseFollow.cs` |
| 7 | Attack bind `<Mouse>/leftButton` | `ActiveWeapon` |
| 8 | Đổi vũ khí bằng phím 1..5 | `ActiveInventory` |
| 9 | Camera follow Player | `CameraController.SetPlayerCameraFollow` — yêu cầu là **camera cố định** |
| 10 | Chết → `SceneManager.LoadScene("Scene1")` trực tiếp | `PlayerHealth.DeathLoadSceneRoutine` |
| 11 | `EconomyManager` chỉ có `UpdateCurrentGold()` (+1) | `EconomyManager` |
| 12 | `EnemyHealth` chết là `Destroy` ngay, không có event | `EnemyHealth.DetectDeath` |
| 13 | `AreaExit` load scene ngay, không khoá cổng, không qua LoadingScene | `AreaExit` |
| 14 | Không có persistence — quay lại scene là quái hồi sinh | toàn bộ |
| 15 | `PlayerHealth.UpdateHealthSlider` dùng `GameObject.Find(...).GetComponent` không null-check | `PlayerHealth` |
| 16 | `Stamina.Start` dùng `GameObject.Find` không null-check | `Stamina` |
| 17 | `EconomyManager` dùng `GameObject.Find` không null-check | `EconomyManager` |
| 18 | Số quái sai: Scene1 có 6 slime (cần 5), Scene2 có 6 Ghost + 2 Grape (cần 5 Grape) | scene |
| 19 | `ActiveInventory` phụ thuộc thứ tự child của 5 slot UI | `ActiveInventory` |
| 20 | `MobileControlsBootstrap.ShowDebugReadout = true` — còn hiển thị dòng debug trên màn hình | `MobileControlsBootstrap` |
| 21 | Chưa có hệ thống âm thanh, `Assets/Audio/` chưa tồn tại | — |
| 22 | Chưa có Pause / Game Over / Shop / Guide | — |
| 23 | `Time.timeScale` chưa được reset ở bất kỳ đâu | — |

## 8. Hướng xử lý đã chọn

- **Không** viết lại game. Mở rộng script hiện có; thêm lớp mới ở `Core/`, `Save/`, `Levels/`, `Audio/`, `Boss/`.
- Tất cả UI runtime (HUD mobile, Pause, Game Over, Shop, Guide, Settings, Loading, Splash, Victory)
  **dựng bằng code** theo đúng pattern `MobileControlsBootstrap`/`MainMenu` đã có sẵn trong project,
  nên không phải sửa YAML scene/prefab một cách rủi ro.
- Aim tập trung vào `PlayerAimController` — desktop dùng chuột, mobile dùng auto-target;
  `Sword`/`Staff`/`Bow`/`MagicLaser`/`MouseFollow` đều đọc từ đó.
- Persistence dựa trên `PersistentObjectId` (GUID sinh ở Editor, ổn định giữa các lần chạy).
- Scene 3/4/5 + Splash/Loading/Victory sinh bằng Editor script (`Tools > Soulbound Gate`),
  vẽ tilemap bằng `Tilemap.SetTile` với RuleTile sẵn có.
