# BÁO CÁO THỰC HIỆN YÊU CẦU BỔ SUNG — SOULBOUND GATE

**Nhánh:** `feature/audio-combat-mechanics` (tách từ `feature/android-touch-controls`)
**Engine:** Unity 2022.3.3f1 · URP 2D · Android
**Nhóm thực hiện:** Quang Ninh và Hong Phong

Tài liệu này đối chiếu từng yêu cầu được giao với cách nó được hiện thực trong game: phần nào
đã có sẵn từ trước, phần nào được làm mới, nằm ở file nào, và được kiểm chứng ra sao.

Quy ước đặt tên trong đề bài:

| Tên trong đề | Trong Soulbound Gate |
|---|---|
| Đối tượng **A** | Nhân vật người chơi (`Player.prefab`, `PlayerController`) |
| Đối tượng **B** | Quái vật: Blue Slime, Grape, Ghost, boss Soul Warden (`EnemyHealth`) |
| Đối tượng **X** | Rune Tăng Tốc (`SpeedRune`) |
| Đối tượng **Y** | Bẫy Gai (`SpikeTrap`) |
| Đối tượng **Z** | Rương Báu (`TreasureChest`) |

---

## Tổng quan mức độ đáp ứng

| # | Yêu cầu | Trạng thái | Trước khi làm |
|---|---|---|---|
| 1a | Âm thanh ngắn khi A bắn / chém / dùng kỹ năng | ✅ Đạt | Kiếm, cung, trượng đã có tiếng; bổ sung tiếng cho 2 kỹ năng mới |
| 1b | Cảnh báo 3–6 lần khi B đi vào vùng cấm | ✅ Đạt | **Chưa có** — làm mới |
| 2a | SoundOff / SoundOn thay thế nhau | ✅ Đạt | **Chưa có** (chỉ có thanh trượt âm lượng) — làm mới |
| 2b | MusicOn / MusicOff thay thế nhau | ✅ Đạt | **Chưa có** — làm mới |
| 3a | Di chuyển của A (hướng, tốc độ) | ✅ Đạt | Đã có; bổ sung hệ thống thay đổi tốc độ tạm thời |
| 3b | ≥ 3 cơ chế tấn công | ✅ Đạt (3) | Đã có sẵn: kiếm, cung tên, trượng phép |
| 3c | ≥ 2 cơ chế phòng thủ | ✅ Đạt (2) | **Chưa có** — làm mới Khiên và Choáng |
| 3d | Va chạm với X, Y, Z tạo ≥ 6 hiệu ứng | ✅ Đạt (8 hiệu ứng) | **Chưa có** — làm mới |
| + | HUD ≥ 3 thông tin của A | ✅ Đạt (8 thông tin) | Đã có máu, stamina, vàng, vũ khí; bổ sung thêm |

---

## 1. Hiệu ứng âm thanh ngắn

### 1a. Âm thanh khi đối tượng A tấn công hoặc dùng kỹ năng

Mọi hiệu ứng âm thanh đi qua một điểm duy nhất là `AudioManager.PlaySfx(GameSfx)`. Nhờ vậy nút tắt
âm thanh (mục 2) chỉ cần chặn ở một chỗ là tắt được toàn bộ.

| Hành động của A | Âm thanh | Nơi gọi |
|---|---|---|
| Vung kiếm | `SwordSwing` | `Sword.Attack()` |
| Bắn cung tên | `BowShot` | `Bow.Attack()` |
| Phóng tia phép từ trượng | `StaffShot` | `Staff.Attack()` |
| Lướt (dash) | `Dash` | `PlayerController.Dash()` |
| Dựng khiên *(mới)* | `ShieldUp` | `PlayerSkills.TryShield()` |
| Tung đòn choáng *(mới)* | `Stun` | `PlayerSkills.TryStun()` |
| Kỹ năng đang hồi | `Denied` | `PlayerSkills` |

Toàn bộ âm thanh được **tổng hợp bằng code** (`Assets/Editor/PlaceholderAudioGenerator.cs`) từ sóng
vuông, sóng sin và nhiễu, xuất ra file WAV — không dùng tài nguyên có bản quyền của bên thứ ba.
Đợt này bổ sung 9 âm thanh: `Warning`, `ShieldUp`, `ShieldBlock`, `ShieldBreak`, `Stun`,
`ChestOpen`, `TrapHit`, `SpeedUp`, `Explosion` — tổng cộng 27 hiệu ứng.

### 1b. Cảnh báo khi đối tượng B đi vào vùng cấm

**File:** `Assets/Scripts/Levels/ForbiddenZone.cs`

Mỗi màn chơi có một **vùng cấm** hình chữ nhật 5×4 ô, hiển thị bằng nền đỏ trong suốt có sọc cảnh báo,
đặt ngay phía trước điểm xuất hiện của người chơi — khu vực quái thường xuyên đi qua.

Cách hoạt động:

1. Vùng cấm là một `BoxCollider2D` ở chế độ **trigger** — không cản đường ai cả.
2. Khi collider của một quái vật (có `EnemyHealth`) **đi vào**, `OnTriggerEnter2D` được gọi.
3. Vùng cấm phát tiếng `Warning` **đúng N lần**, mỗi lần cách nhau 0.35 giây, và nhấp nháy đỏ theo nhịp.
   Trên màn hình hiện dòng *"CẢNH BÁO: QUÁI VẬT XÂM NHẬP VÙNG CẤM!"*.
4. N được cấu hình trong Inspector với thanh trượt **giới hạn cứng từ 3 đến 6**
   (`[Range(3, 6)]`, và `ClampWarnings()` kẹp lại lần nữa trong code). Mặc định là 4.

Các chi tiết để cảnh báo đúng nghĩa *"bắt đầu di chuyển vào"*:

| Tình huống | Xử lý |
|---|---|
| Quái đã đứng sẵn trong vùng khi màn vừa tải | Bỏ qua 0.75 giây đầu — chúng không "đi vào" |
| Quái đã ở trong vùng, di chuyển qua lại | Chỉ tính một lần cho tới khi nó đi ra |
| Nhiều quái ập vào cùng lúc | Chỉ một hồi cảnh báo, sau đó chờ 1.5 giây mới có thể kêu lại |
| Người chơi đi vào vùng | Không kích hoạt — chỉ quái mới tính |

---

## 2. Bật / tắt âm thanh

**File:** `Assets/Scripts/Audio/AudioToggles.cs`, `Assets/Scripts/UI/GameplayRuntime.cs`

Góc trên bên phải màn chơi, ngay dưới nút tạm dừng, có **4 đối tượng riêng biệt** tên đúng như đề bài:
`SoundOff`, `SoundOn`, `MusicOn`, `MusicOff`. Đây là 4 GameObject khác nhau, không phải 2 nút đổi hình.

| Cặp | Kích thước | Vị trí (neo góc trên phải) |
|---|---|---|
| `SoundOff` / `SoundOn` | 100 × 100 — bằng nhau | (−70, −200) — trùng nhau |
| `MusicOn` / `MusicOff` | 100 × 100 — bằng nhau | (−70, −320) — trùng nhau |

Mỗi cặp dùng **chung một hằng số vị trí và một hằng số kích thước** trong code, nên hai đối tượng
trong cặp chắc chắn trùng khít nhau. Tại mỗi thời điểm chỉ một đối tượng trong cặp hiển thị.

### 2a. Hiệu ứng âm thanh ngắn

```
Âm thanh đang BẬT  → hiện SoundOff (biểu tượng loa gạch chéo)
     │ click SoundOff
     ▼
Âm thanh TẮT, SoundOff ẩn, SoundOn hiện ra đúng vị trí đó (biểu tượng loa có sóng)
     │ click SoundOn
     ▼
Âm thanh BẬT lại, SoundOn ẩn, SoundOff hiện ra
```

Khi tắt, `AudioManager.PlaySfx` **dừng ngay từ đầu hàm** — không âm thanh ngắn nào được phát,
kể cả tiếng cảnh báo vùng cấm. Nhạc nền không bị ảnh hưởng.

### 2b. Nhạc nền

```
Nhạc đang TẮT → hiện MusicOn (nốt nhạc)
     │ click MusicOn
     ▼
Nhạc PHÁT, MusicOn ẩn, MusicOff hiện ra đúng vị trí đó (nốt nhạc gạch chéo)
     │ click MusicOff
     ▼
Nhạc TẮT, MusicOff ẩn, MusicOn hiện ra
```

Tắt nhạc là **tắt tiếng chứ không dừng bài**, nên bật lại thì nhạc tiếp tục từ chỗ đang phát.

### Lưu trạng thái

Cả hai công tắc lưu bằng `PlayerPrefs`, nên tắt âm thanh rồi thoát game, mở lại vẫn giữ nguyên.
Công tắc tách biệt với thanh trượt âm lượng trong Cài đặt: bật lại sẽ trả về đúng mức âm lượng
người chơi đã chọn trước đó.

Biểu tượng loa và nốt nhạc được **vẽ bằng code** theo phong cách pixel (`GameplaySprites.cs`).

---

## 3. Cơ chế di chuyển, tấn công và phòng thủ của A

### 3a. Di chuyển

**File:** `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Player/SpeedModifiers.cs`

| Thành phần | Mô tả |
|---|---|
| **Hướng** | Joystick ảo (điện thoại) hoặc WASD (máy tính), đủ 360°. Hướng nhân vật quay mặt theo hướng nhắm |
| **Tốc độ cơ bản** | `moveSpeed` trên prefab |
| **Lướt (Dash)** | Nhân tốc độ ×4 trong 0.2 giây, tốn 1 stamina, có vệt sáng |
| **Thay đổi tốc độ tạm thời** *(mới)* | Lớp `SpeedModifiers`: tăng tốc và làm chậm cùng lúc thì **nhân với nhau** |
| **Trạng thái ra HUD** *(mới)* | `MoveDirection`, `CurrentSpeedMultiplier` |

Công thức tốc độ thực tế mỗi bước vật lý:

```
tốc_độ = moveSpeed × (hệ số tăng tốc nếu còn hiệu lực) × (hệ số làm chậm nếu còn hiệu lực)
```

Nhặt rune thứ hai khi đang tăng tốc sẽ **gia hạn** thời gian chứ không cộng dồn hệ số —
tránh tình trạng tốc độ quá cao khiến nhân vật xuyên tường.

### 3b. Ba cơ chế tấn công

| # | Vũ khí | Kiểu tấn công | Sát thương | Hồi chiêu | Đạn / hiệu ứng |
|---|---|---|---|---|---|
| 1 | **Kiếm** (Sword) | Chém cận chiến theo cung | 1 | 0.5 s | Hiệu ứng vệt chém |
| 2 | **Cung** (Bow) | Bắn tên bay thẳng, tầm 12 | 1 | 0.7 s | `Arrow.prefab` |
| 3 | **Trượng** (Staff) | Phóng tia phép dài dần, tầm 8 | 2 | 1.2 s | `Magic Laser.prefab` |

Trên điện thoại, giữ nút **TẤN CÔNG** để đánh liên tục theo hồi chiêu. Hệ thống **tự nhắm**
(`PlayerAimController`) hướng vào quái gần nhất trong bán kính 9; không có quái thì bắn theo hướng
di chuyển. Cung và trượng mở bán trong Cửa hàng sau khi hoàn thành màn 1 và màn 2.

### 3c. Hai cơ chế phòng thủ *(mới)*

**File:** `Assets/Scripts/Player/PlayerSkills.cs`, `Assets/Scripts/Enemies/EnemyStun.cs`

#### Phòng thủ 1 — KHIÊN

| | |
|---|---|
| Kích hoạt | Nút **KHIÊN** (điện thoại) hoặc phím **Q** |
| Thời gian hiệu lực | 3 giây |
| Hồi chiêu | 9 giây (tính từ lúc dùng) |
| Tác dụng | **Chặn mọi đòn**: tên, đạn của Ghost và boss, vũng nho độc của Grape, va chạm với quái |
| Hình ảnh | Bong bóng xanh bao quanh nhân vật; nhấp nháy trong giây cuối để báo sắp tắt |
| Âm thanh | `ShieldUp` khi dựng, `ShieldBlock` mỗi lần đỡ đòn |

Khiên chặn được **tất cả** nguồn sát thương vì mọi sát thương trong game đều đi qua một hàm duy nhất
`PlayerHealth.TakeDamage()`, và khiên được kiểm tra ở đầu hàm đó.

#### Phòng thủ 2 — CHOÁNG (vô hiệu hoá quái vật)

| | |
|---|---|
| Kích hoạt | Nút **CHOÁNG** hoặc phím **E** |
| Phạm vi | Bán kính 5 ô quanh nhân vật |
| Thời gian choáng | 2.5 giây (boss chỉ bị 1.25 giây) |
| Hồi chiêu | 12 giây |
| Tác dụng lên quái | Đứng yên, **không tấn công**, **không gây sát thương khi chạm**, hoạt ảnh đóng băng |
| Hình ảnh | Vòng sóng lan rộng; quái bị ám màu xanh lạnh nhấp nháy |
| Âm thanh | `Stun` |

Boss Soul Warden khi bị choáng sẽ **huỷ đòn đang vận sức** và ngừng di chuyển.

Cả hai nút đều có **vòng hồi chiêu** tối dần trên mặt nút và hiện số giây còn lại.

### 3d. Hiệu ứng khi A va chạm với X, Y, Z *(mới — 8 hiệu ứng)*

Cả ba đối tượng dùng **collider trigger**: người chơi đi qua được, không bị cản.

#### Đối tượng X — Rune Tăng Tốc (`SpeedRune.cs`)

Viên kim cương xanh lơ lửng, nhấp nháy.

| # | Hiệu ứng | Chi tiết |
|---|---|---|
| **1** | **Tăng tốc độ di chuyển** | ×1.6 trong 5 giây |
| **2** | **Biến mất** | Tan thành vòng sáng; đã nhặt thì không xuất hiện lại khi quay lại màn |

#### Đối tượng Y — Bẫy Gai (`SpikeTrap.cs`)

Tấm sắt có gai; gai bật lên khi dẫm vào rồi thụt xuống. Bẫy **không mất đi**, tự nạp lại sau 1.2 giây.

| # | Hiệu ứng | Chi tiết |
|---|---|---|
| **3** | **Giảm máu** | −1 máu, kèm rung màn hình và bật lùi |
| **4** | **Giảm tốc độ di chuyển** | ×0.5 trong 2.5 giây |
| **5** | **Mất khiên** | Nếu đang bật khiên: **khiên vỡ thay cho máu** |

Hiệu ứng 5 nối trực tiếp cơ chế va chạm với cơ chế phòng thủ: khiên bảo vệ được một lần dẫm bẫy,
nhưng phải trả giá bằng chính cái khiên.

#### Đối tượng Z — Rương Báu (`TreasureChest.cs`)

Dùng sprite rương có sẵn trong dự án.

| # | Hiệu ứng | Chi tiết |
|---|---|---|
| **6** | **Nổ tung** | Tiếng nổ, hiệu ứng mảnh vỡ, vòng sáng vàng, rung màn hình |
| **7** | **Tăng vàng** | +10 vàng |
| **8** | **Xuất hiện vật phẩm mới** | Một bình máu văng ra để nhặt |

Rương đã mở thì biến mất, và được ghi vào file save — quay lại màn **không thể mở lại lần hai**.

#### Tổng hợp

| | X — Rune | Y — Bẫy gai | Z — Rương |
|---|---|---|---|
| Tăng tốc độ | ✅ | | |
| Biến mất | ✅ | | ✅ |
| Giảm máu | | ✅ | |
| Giảm tốc độ | | ✅ | |
| Mất khiên | | ✅ | |
| Nổ tung | | | ✅ |
| Tăng vàng | | | ✅ |
| Xuất hiện vật phẩm mới | | | ✅ |

**8 hiệu ứng khác nhau** — vượt yêu cầu tối thiểu là 6.

Ngoài ra game vẫn giữ các va chạm có sẵn từ trước: đồng vàng (+vàng), cầu máu (+máu),
cầu stamina (+stamina), và chạm quái (−máu, bật lùi).

---

## Yêu cầu bổ sung — HUD

**File:** `Assets/Scripts/UI/HudLayout.cs`, `Assets/Scripts/UI/GameplayRuntime.cs`

HUD hiển thị **8 nhóm thông tin** của đối tượng A:

| # | Thông tin | Vị trí | Dạng hiển thị |
|---|---|---|---|
| 1 | **Máu** | Trên trái, hàng 1 | Biểu tượng tim + thanh máu |
| 2 | **Stamina** | Trên trái, hàng 2 | Các ô sét đầy / rỗng |
| 3 | **Vàng** | Trên trái, hàng 3 | Đồng xu + số |
| 4 | **Chỉ dẫn nhiệm vụ** *(mới)* | Dưới vàng | *"QUÁI CÒN LẠI: 3 / 5"*, *"CỔNG ĐÃ MỞ"*, *"ĐÁNH BẠI SOUL WARDEN"* |
| 5 | **Hiệu ứng đang chịu** *(mới)* | Dòng tiếp theo | *"TĂNG TỐC 3s   BỊ LÀM CHẬM 2s   (TỐC ĐỘ x0.8)"* |
| 6 | **Kỹ năng khả dụng** *(mới)* | Dòng tiếp theo | *"[Q] KHIÊN: SẴN SÀNG   [E] CHOÁNG: HỒI 7s"* |
| 7 | **Tình trạng vũ khí** | Trên phải | 3 ô Kiếm / Cung / Trượng; ô đang cầm viền vàng, ô chưa mua có ổ khoá |
| 8 | **Hồi chiêu kỹ năng** *(mới)* | Nút KHIÊN, CHOÁNG | Vòng tối dần + số giây |

HUD nằm trong **Safe Area** nên không bị tai thỏ hay camera đục lỗ che, và dùng Canvas Scaler khớp theo
chiều cao để bố cục giống nhau trên mọi tỉ lệ màn hình.

---

## Kiến trúc và danh sách file

### File mới

| File | Vai trò |
|---|---|
| `Scripts/Audio/AudioToggles.cs` | Hai công tắc âm thanh / nhạc, lưu `PlayerPrefs`, phát sự kiện khi đổi |
| `Scripts/Player/PlayerSkills.cs` | Khiên và Choáng |
| `Scripts/Player/SkillCooldown.cs` | Thời gian hiệu lực + hồi chiêu (C# thuần, test được) |
| `Scripts/Player/SpeedModifiers.cs` | Tăng tốc / làm chậm tạm thời (C# thuần, test được) |
| `Scripts/Enemies/EnemyStun.cs` | Trạng thái choáng của quái |
| `Scripts/Levels/ForbiddenZone.cs` | Vùng cấm và còi cảnh báo |
| `Scripts/Levels/SpeedRune.cs` | Đối tượng X |
| `Scripts/Levels/SpikeTrap.cs` | Đối tượng Y |
| `Scripts/Levels/TreasureChest.cs` | Đối tượng Z |
| `Scripts/Misc/GameplaySprites.cs` | Sprite pixel sinh bằng code: bong bóng khiên, vùng cấm, rune, gai, biểu tượng loa và nốt nhạc |
| `Scripts/Misc/ExpandingRing.cs` | Hiệu ứng vòng sáng lan rộng |
| `Editor/MechanicsSmokeTest.cs` | Kiểm thử tự động trong Play Mode |
| `Editor/Tests/MechanicsTests.cs` | Unit test |

### File sửa

| File | Thay đổi |
|---|---|
| `Audio/AudioManager.cs` | Chặn hiệu ứng khi tắt âm; tắt tiếng nhạc khi tắt nhạc |
| `Audio/GameSfx.cs` | Thêm 9 âm thanh |
| `Player/PlayerController.cs` | Hệ số tốc độ; tự gắn `PlayerSkills` |
| `Player/PlayerHealth.cs` | Khiên chặn sát thương; quái bị choáng không gây sát thương khi chạm |
| `Enemies/EnemyAI.cs`, `EnemyPathfinding.cs` | Dừng di chuyển và tấn công khi bị choáng |
| `Boss/BossController.cs` | Boss dừng và huỷ đòn khi bị choáng |
| `Levels/LevelManager.cs` | Ghi nhớ rune đã nhặt, rương đã mở; đếm tổng số quái cho HUD |
| `UI/GameplayRuntime.cs` | 4 công tắc âm thanh, 2 nút kỹ năng, 3 dòng thông tin |
| `Editor/LevelSceneBuilder.cs` | Đặt vùng cấm + X/Y/Z vào các màn |
| `Editor/PlaceholderAudioGenerator.cs` | Sinh 9 âm thanh mới |
| `Editor/ProjectValidator.cs` | Kiểm tra mỗi màn có đủ vùng cấm, X, Y, Z |

### Cách đưa đối tượng mới vào các màn

Công cụ `Tools > Soulbound Gate > Complete Android Game Setup` tự đặt một vùng cấm và một bộ X / Y / Z
vào **cả 5 màn**, gom dưới GameObject `Gameplay Additions`. Vị trí được chọn tự động sao cho:

- người chơi **đi tới được** (dùng bản đồ vùng đi được),
- **không nằm trong** tường hay vật cản,
- cách điểm xuất hiện, cách nhau và cách quái một khoảng an toàn.

Công cụ **chỉ thêm, không sửa**: nếu màn đã có `Gameplay Additions`, nó để nguyên. Bạn có thể kéo các
đối tượng này đi chỗ khác trong Editor mà chạy lại công cụ cũng không bị đặt lại. Số prefab trang trí
trong mỗi màn được xác nhận **không đổi** trước và sau khi chạy (35 / 35 / 32 / 38 / 38).

Rune và bẫy gai dùng sprite sinh lúc chạy nên trong Scene view được vẽ bằng **gizmo**
(hình thoi xanh, ô vuông đỏ) để dễ nhìn và kéo thả.

---

## Kết quả kiểm thử tự động

Tất cả chạy bằng Unity 2022.3.3f1 ở chế độ batch trên nhánh `feature/audio-combat-mechanics`.

| Bộ kiểm thử | Lệnh (`-executeMethod`) | Kết quả |
|---|---|---|
| Kiểm tra dự án | `ProjectValidator.Validate` | **Đạt hết** — mỗi màn có đủ vùng cấm, X, Y, Z |
| Unit test EditMode | Test Runner | **24 / 24 đạt** (8 test mới trong `MechanicsTests.cs`) |
| Cơ chế mới | `MechanicsSmokeTest.Run` | **Đạt hết** (xem chi tiết bên dưới) |
| Gameplay | `GameplaySmokeTest.Run` | **Đạt hết** — HUD máu / stamina / vàng xếp y = 14 / 66 / 118, không đè nhau |
| Chuyển màn | `TransitionSmokeTest` | **Đạt hết** — điểm đến thông thoáng, vùng đi được 241–362 ô mỗi màn |
| Menu | Menu smoke test | **Đạt hết** |
| Điểm xuất hiện / vùng đi được | Spawn & Walkability check | **Đạt hết** |
| Build Android | `AndroidBuilder.BuildApk` | **Thành công** — `Builds/Android/SoulboundGate.apk` (~43 MB) |

Chi tiết `MechanicsSmokeTest` (chạy thật trong Play Mode):

- SoundOff/SoundOn và MusicOn/MusicOff: **cùng kích thước, cùng vị trí, luôn chỉ hiện 1 cái**; bấm thì
  đổi trạng thái và biểu tượng thay nhau; khi tắt hiệu ứng thì **không có âm nào được phát**, bật lại thì có tiếng.
- Khiên: bật được; trúng đòn khi có khiên **không mất máu**.
- Bẫy gai (Y) khi có khiên → **khiên vỡ, không mất máu**, vẫn bị làm chậm; khi không có khiên → máu 3 → 2.
- Rune (X): tăng tốc; kết hợp với làm chậm ra hệ số **x0.80** (1.6 × 0.5); rune biến mất sau khi nhặt.
- Rương (Z): vàng 0 → 10, rơi ra 1 vật phẩm mới, rương biến mất.
- Choáng: trúng ít nhất 1 quái; quái di chuyển **0.000** đơn vị trong 0.8 giây bị choáng.
- Vùng cấm: quái đi vào → còi kêu **4 lần** (nằm trong khoảng 3–6).
- HUD có đủ: thanh máu, stamina, vàng, nút KHIÊN, nút CHOÁNG, dòng mục tiêu.

> Ghi chú: ở chế độ batch (không có màn hình) Unity trả về kích thước canvas vô nghĩa, nên test HUD
> kiểm tra giá trị bố cục đã áp dụng thay vì đọc toạ độ trên màn hình. Bố cục thực tế nên xem thêm
> bằng tay trên điện thoại.

---

## Hướng dẫn thao tác để kiểm tra bằng tay

| Kiểm tra | Cách làm | Kết quả mong đợi |
|---|---|---|
| Tiếng tấn công | Bấm TẤN CÔNG với kiếm, cung, trượng | Mỗi vũ khí một tiếng khác nhau |
| Cảnh báo vùng cấm | Đứng xa, chờ quái lang thang vào vùng đỏ | Còi kêu 4 lần, vùng nhấp nháy, hiện dòng cảnh báo |
| SoundOff | Bấm biểu tượng loa gạch chéo | Mất tiếng hiệu ứng; biểu tượng đổi thành loa có sóng, cùng chỗ cùng cỡ |
| SoundOn | Bấm loa có sóng | Có tiếng lại; biểu tượng đổi về loa gạch chéo |
| MusicOff / MusicOn | Bấm nốt nhạc | Nhạc nền tắt / bật, biểu tượng thay nhau |
| Lưu công tắc | Tắt âm, thoát game, mở lại | Vẫn tắt |
| Khiên | Bấm KHIÊN / phím Q, để quái bắn | Bong bóng xanh, không mất máu |
| Choáng | Đứng gần quái, bấm CHOÁNG / phím E | Quái xanh lại, đứng im, chạm vào không mất máu |
| X — Rune | Đi qua viên kim cương xanh | Chạy nhanh hơn, HUD hiện "TĂNG TỐC 5s", rune biến mất |
| Y — Bẫy | Dẫm lên tấm gai | −1 máu, chậm lại, HUD hiện "BỊ LÀM CHẬM" |
| Y khi có khiên | Bật khiên rồi dẫm bẫy | Khiên vỡ, **không mất máu**, vẫn bị chậm |
| Z — Rương | Đi qua rương | Nổ, +10 vàng, rơi ra bình máu, rương biến mất |
| Không farm lại | Mở rương, sang màn khác rồi quay lại | Rương không còn |
