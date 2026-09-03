# TÀI LIỆU BÁO CÁO DỰ ÁN — SOULBOUND GATE (2D TOP DOWN RPG)

---

## MỤC LỤC

1. [Thông tin chung](#1-thông-tin-chung)
2. [Mục tiêu và phạm vi dự án](#2-mục-tiêu-và-phạm-vi-dự-án)
3. [Công nghệ sử dụng](#3-công-nghệ-sử-dụng)
4. [Cấu trúc dự án](#4-cấu-trúc-dự-án)
5. [Kiến trúc và các mẫu thiết kế áp dụng](#5-kiến-trúc-và-các-mẫu-thiết-kế-áp-dụng)
6. [Luồng hoạt động của game](#6-luồng-hoạt-động-của-game)
7. [**PHẦN TRỌNG TÂM BÁO CÁO**](#7-phần-trọng-tâm-báo-cáo)
   - 7.1 Màn hình vào game (Splash / Home Screen)
   - 7.2 Màn hình chơi game và background chuyển động
   - 7.3 Các đối tượng trong game
8. [Phần mở rộng cá nhân: chuyển sang nền tảng Android](#8-phần-mở-rộng-cá-nhân-chuyển-sang-nền-tảng-android)
9. [Hạn chế hiện tại và hướng phát triển](#9-hạn-chế-hiện-tại-và-hướng-phát-triển)
10. [Nội dung trình bày dự kiến](#10-nội-dung-trình-bày-dự-kiến)
11. [Phụ lục: danh sách toàn bộ script](#11-phụ-lục-danh-sách-toàn-bộ-script)

---

## 1. Thông tin chung

| Hạng mục | Nội dung |
|---|---|
| Tên dự án | Soulbound Gate (game nhập vai 2D góc nhìn từ trên xuống) |
| Thể loại | Action RPG, góc nhìn từ trên xuống (top-down), đồ họa 2D pixel |
| Game engine | Unity 2022.3.3f1 (bản LTS) |
| Ngôn ngữ lập trình | C# |
| Nền tảng đích | PC (Windows) và Android |
| Số scene hiện có | 2 scene gameplay (`Scene1`, `Scene2`) |
| Nguồn tham khảo | Series tutorial "2D Top Down RPG" gồm 25 phần (tài liệu từng phần được lưu trong thư mục `Document/`) |
| Số script C# | 28 script (24 script gốc + 4 script bổ sung cho Android) |

---

## 2. Mục tiêu và phạm vi dự án

**Mục tiêu học tập:** Thông qua việc xây dựng lại một game RPG 2D hoàn chỉnh theo tutorial, nhóm nắm được các kỹ thuật nền tảng khi phát triển game 2D bằng Unity:

- Xử lý input và điều khiển nhân vật với hệ thống vật lý 2D (`Rigidbody2D`)
- Quản lý animation nhân vật theo hướng di chuyển (Animator + Blend Tree)
- Camera bám nhân vật (Cinemachine)
- Xây dựng bản đồ bằng Tilemap
- Hệ thống chiến đấu: va chạm, sát thương, hiệu ứng phản hồi (knockback, flash)
- AI đơn giản cho quái vật (state machine)
- Hệ thống vũ khí có thể mở rộng (interface + ScriptableObject)
- Chuyển cảnh giữa các khu vực trong game
- Hiệu ứng background chuyển động (parallax)

**Phạm vi đã hoàn thành:** Nhân vật di chuyển và chiến đấu, 3 loại vũ khí, quái vật có AI và máu, vật thể phá hủy được, chuyển đổi giữa 2 khu vực, hiệu ứng parallax, giao diện túi đồ chọn vũ khí, và (phần mở rộng) điều khiển cảm ứng để chạy trên điện thoại Android.

**Phạm vi chưa hoàn thành:** Màn hình vào game (splash/home screen), hệ thống máu cho người chơi, âm thanh, lưu game.

---

## 3. Công nghệ sử dụng

| Package / Công nghệ | Phiên bản | Vai trò trong dự án |
|---|---|---|
| Universal Render Pipeline (URP) | 14.0.8 | Pipeline render 2D, hỗ trợ ánh sáng 2D và material tùy chỉnh (dùng cho hiệu ứng flash trắng khi trúng đòn) |
| Cinemachine | 2.9.7 | Camera ảo tự động bám theo nhân vật, giới hạn camera trong biên bản đồ |
| Input System | 1.6.3 | Hệ thống input thế hệ mới của Unity — định nghĩa các action (Move, Attack, Dash, Inventory) tách rời khỏi thiết bị vật lý |
| 2D Feature Set | 2.0.0 | Bộ công cụ 2D: Sprite Editor, Tilemap, 2D Animation, 2D Physics |
| TextMeshPro | 3.0.6 | Hiển thị văn bản chất lượng cao trong UI |
| Visual Scripting | 1.8.0 | (Có sẵn trong project, chưa sử dụng) |

**Lý do dùng Input System mới thay vì Input Manager cũ:** Cho phép định nghĩa action một cách trừu tượng (ví dụ action "Move" trả về `Vector2`), sau đó gán (binding) vào nhiều thiết bị khác nhau — bàn phím, gamepad, cảm ứng — mà không phải sửa code xử lý. Đây chính là nền tảng giúp phần mở rộng Android ở mục 8 thực hiện được dễ dàng.

---

## 4. Cấu trúc dự án

```
Assets/
├── Scenes/
│   ├── Scene1.unity          # Khu vực chơi thứ nhất
│   └── Scene2.unity          # Khu vực chơi thứ hai
├── Scripts/
│   ├── Player/               # Nhân vật người chơi và vũ khí cận chiến
│   │   ├── PlayerController.cs
│   │   ├── ActiveWeapon.cs
│   │   ├── Sword.cs
│   │   ├── SlashAnim.cs
│   │   ├── DamageSource.cs
│   │   ├── Player Controls.inputactions   # File cấu hình Input System
│   │   └── Player Controls.cs             # Class C# Unity tự sinh từ file trên
│   ├── Enemies/              # Quái vật
│   │   ├── EnemyAI.cs
│   │   ├── EnemyPathfinding.cs
│   │   └── EnemyHealth.cs
│   ├── UI/                   # Giao diện và các vũ khí khác
│   │   ├── ActiveInventory.cs
│   │   ├── InventorySlot.cs
│   │   ├── WeaponInfo.cs
│   │   ├── IWeapon.cs
│   │   ├── Bow.cs
│   │   └── Staff.cs
│   ├── Management/           # Các hệ thống quản lý toàn cục
│   │   ├── Singleton.cs
│   │   ├── BaseSingleton.cs
│   │   ├── SceneManagement.cs
│   │   ├── CameraController.cs
│   │   └── UIFade.cs
│   ├── Misc/                 # Các thành phần phụ trợ
│   │   ├── Parallax.cs       # ← Background chuyển động
│   │   ├── Knockback.cs
│   │   ├── Flash.cs
│   │   ├── Destructible.cs
│   │   └── TransparentDetection.cs
│   ├── Mobile/               # ← Phần mở rộng cá nhân cho Android
│   │   ├── MobileInput.cs
│   │   ├── OnScreenJoystick.cs
│   │   └── MobileControlsBootstrap.cs
│   └── MouseFollow.cs
└── Settings/                 # Cấu hình URP
```

---

## 5. Kiến trúc và các mẫu thiết kế áp dụng

### 5.1 Singleton Pattern

Dự án dùng một lớp Singleton generic (`Singleton<T>`) làm nền cho tất cả các hệ thống cần truy cập toàn cục:

```csharp
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;
    public static T Instance { get { return instance; } }

    protected virtual void Awake()
    {
        if (instance != null && this.gameObject != null)
        {
            Destroy(this.gameObject);   // Chống trùng lặp khi load lại scene
        }
        else
        {
            instance = (T)this;
        }

        if (!gameObject.transform.parent)
        {
            DontDestroyOnLoad(gameObject);  // Giữ object qua các lần chuyển scene
        }
    }
}
```

Các lớp kế thừa: `PlayerController`, `ActiveWeapon`, `SceneManagement`, `CameraController`, `UIFade`. Nhờ đó, mọi script khác có thể gọi trực tiếp `PlayerController.Instance.transform` mà không cần tham chiếu thủ công.

**Ý nghĩa:** Nhân vật và giao diện không bị hủy khi chuyển scene → giữ nguyên trạng thái (vũ khí đang cầm, vị trí túi đồ) khi người chơi đi từ khu vực này sang khu vực khác.

### 5.2 Interface — đa hình cho hệ thống vũ khí

```csharp
interface IWeapon
{
    public void Attack();
}
```

Cả ba vũ khí `Sword`, `Bow`, `Staff` đều triển khai interface này. Nhờ vậy `ActiveWeapon` chỉ cần gọi:

```csharp
(CurrentActiveWeapon as IWeapon).Attack();
```

mà không cần biết đang cầm vũ khí gì. Muốn thêm vũ khí mới (ví dụ rìu, dao găm), chỉ cần tạo script mới triển khai `IWeapon` — **không phải sửa dòng code nào của hệ thống cũ** (nguyên tắc Open/Closed).

### 5.3 ScriptableObject — dữ liệu cấu hình tách rời code

```csharp
[CreateAssetMenu(menuName = "New Weapon")]
public class WeaponInfo : ScriptableObject
{
    public GameObject weaponPrefab;
    public float weaponCooldown;
}
```

Mỗi vũ khí là một asset dữ liệu riêng trong project. Người thiết kế game có thể tạo/chỉnh vũ khí ngay trong Editor mà không cần lập trình viên.

### 5.4 Component-based Architecture

Đặc trưng của Unity: mỗi đối tượng được lắp ghép từ nhiều component nhỏ, mỗi component một nhiệm vụ. Ví dụ quái vật gồm: `EnemyAI` (quyết định đi đâu) + `EnemyPathfinding` (thực thi di chuyển) + `EnemyHealth` (máu) + `Knockback` (bị hất lùi) + `Flash` (nhấp nháy). Các component này độc lập và tái sử dụng được — `Knockback` và `Flash` cũng dùng lại được cho đối tượng khác.

---

## 6. Luồng hoạt động của game

### 6.1 Khởi động

1. Unity load `Scene1` (scene đầu tiên trong Build Settings).
2. Các đối tượng Singleton (`PlayerController`, `ActiveWeapon`, `UIFade`, `SceneManagement`, `CameraController`) khởi tạo trong `Awake()` và được đánh dấu `DontDestroyOnLoad`.
3. `ActiveInventory` kích hoạt ô túi đồ đầu tiên → sinh (instantiate) prefab vũ khí tương ứng và gắn vào tay nhân vật.
4. Người chơi bắt đầu điều khiển ngay — **không qua màn hình menu nào** (xem mục 7.1).

### 6.2 Vòng lặp gameplay mỗi khung hình

| Giai đoạn | Xử lý |
|---|---|
| `Update()` | Đọc input di chuyển, cập nhật tham số Animator, kiểm tra nút tấn công, xoay vũ khí theo hướng ngắm |
| `FixedUpdate()` | Di chuyển nhân vật bằng `Rigidbody2D.MovePosition`, lật sprite theo hướng ngắm, cập nhật vị trí lớp parallax |
| Sự kiện va chạm | `OnTriggerEnter2D` xử lý gây sát thương, phá vật thể, chuyển khu vực, làm mờ vật cản |

### 6.3 Luồng chuyển khu vực (Scene1 ↔ Scene2)

```
Người chơi chạm vào cổng (AreaExit)
   → SceneManagement.SetTransitionName("...")   [ghi nhớ mình đi qua cổng nào]
   → UIFade.FadeToBlack()                       [màn hình tối dần trong 1 giây]
   → SceneManager.LoadScene(sceneToLoad)        [nạp scene mới]
   → Scene mới khởi động, AreaEntrance.Start() so khớp tên cổng
   → Đặt nhân vật vào đúng vị trí cổng tương ứng
   → CameraController.SetPlayerCameraFollow()   [camera bám lại nhân vật]
   → UIFade.FadeToClear()                       [màn hình sáng dần trở lại]
```

Đây là cơ chế đáng chú ý vì nó chính là **nền tảng kỹ thuật có thể tái sử dụng cho màn hình loading** ở mục 7.1.

---

## 7. PHẦN TRỌNG TÂM BÁO CÁO

> Theo yêu cầu chủ đề: *(1) Xây dựng màn hình vào game (splash screen/home screen) có icon/logo/introductory image/loading bar/buttons; (2) Xây dựng màn hình chơi game có ảnh background chuyển động, có một hoặc nhiều đối tượng game.*

### 7.1 Màn hình vào game (Splash / Home Screen)

#### a) Hiện trạng

**Dự án hiện chưa có màn hình vào game riêng biệt.** Cả hai scene trong project (`Scene1`, `Scene2`) đều là scene gameplay. Khi chạy game, người chơi vào thẳng khu vực chơi.

Riêng phần **splash screen mặc định của Unity** (logo Unity hiện lúc khởi động app) thì có xuất hiện trên bản build Android, nhưng đó là màn hình do engine tự sinh, không phải do nhóm xây dựng.

#### b) Các thành phần kỹ thuật đã có, có thể tái sử dụng

| Thành phần sẵn có | Mô tả | Dùng được cho màn hình vào game như thế nào |
|---|---|---|
| `UIFade.cs` | Coroutine thay đổi dần alpha của một `Image` phủ toàn màn hình, có `FadeToBlack()` và `FadeToClear()` | Làm hiệu ứng chuyển mượt từ Home Screen sang scene gameplay |
| `SceneManagement.cs` | Singleton lưu trạng thái chuyển cảnh giữa các scene | Mở rộng để quản lý luồng Menu → Game |
| Cơ chế `AreaExit`/`AreaEntrance` | Đã chứng minh mô hình "chờ hiệu ứng xong rồi mới `LoadScene`" hoạt động tốt | Áp dụng nguyên mô hình đó cho nút Start |
| Canvas + EventSystem | Đã có sẵn trong `Scene1` cho giao diện túi đồ | Không phải dựng lại hạ tầng UI từ đầu |

Trích đoạn `UIFade.cs` — kỹ thuật fade sẽ dùng lại:

```csharp
private IEnumerator FadeRoutine(float targetAlpha)
{
    while (!Mathf.Approximately(fadeScreen.color.a, targetAlpha))
    {
        float alpha = Mathf.MoveTowards(fadeScreen.color.a, targetAlpha,
                                        fadeSpeed * Time.deltaTime);
        fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g,
                                     fadeScreen.color.b, alpha);
        yield return null;
    }
}
```

#### c) Thiết kế đề xuất cho màn hình vào game

| Thành phần yêu cầu | Cách triển khai dự kiến trong Unity |
|---|---|
| **Icon** | Đặt trong `Project Settings → Player → Icon`, áp dụng cho biểu tượng app trên Android |
| **Logo** | Một `Image` UI đặt giữa phía trên Canvas của scene Menu |
| **Introductory image** | `Image` phủ toàn màn hình làm nền, đặt ở lớp dưới cùng của Canvas |
| **Loading bar** | Component `Slider` UI, cập nhật `value` theo `AsyncOperation.progress` khi nạp scene bằng `SceneManager.LoadSceneAsync()` |
| **Buttons** | Các `Button` UI: **Start** (nạp `Scene1`), **Settings**, **Quit** (`Application.Quit()`) |

Mã giả cho thanh loading:

```csharp
IEnumerator LoadGameRoutine()
{
    AsyncOperation op = SceneManager.LoadSceneAsync("Scene1");
    op.allowSceneActivation = false;

    while (op.progress < 0.9f)
    {
        loadingBar.value = op.progress / 0.9f;   // cập nhật thanh tiến trình
        yield return null;
    }

    loadingBar.value = 1f;
    UIFade.Instance.FadeToBlack();
    yield return new WaitForSeconds(1f);
    op.allowSceneActivation = true;              // chính thức vào game
}
```

Khối lượng công việc ước tính: khoảng 1 scene mới + 1 script (~60 dòng) + tài nguyên ảnh.

---

### 7.2 Màn hình chơi game và background chuyển động

Đây là phần **đã hoàn thành và demo được trực tiếp**.

#### a) Kỹ thuật Parallax Scrolling

Hiệu ứng background chuyển động được cài đặt bằng kỹ thuật **parallax scrolling** — nguyên lý: các lớp ảnh nền ở xa di chuyển chậm hơn các lớp ở gần, tạo ảo giác chiều sâu ba chiều trên mặt phẳng hai chiều. Đây chính là hiện tượng thị giác khi ta ngồi trên ô tô: cột điện gần lướt qua rất nhanh, còn dãy núi xa gần như đứng yên.

Toàn bộ hiệu ứng nằm trong script `Assets/Scripts/Misc/Parallax.cs`:

```csharp
public class Parallax : MonoBehaviour
{
    [SerializeField] private float parallaxOffset = -0.15f;

    private Camera cam;
    private Vector2 startPos;

    // Khoảng cách camera đã đi được so với vị trí ban đầu
    private Vector2 travel => (Vector2)cam.transform.position - startPos;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Start()
    {
        startPos = transform.position;   // Ghi nhớ vị trí gốc của lớp nền
    }

    private void FixedUpdate()
    {
        transform.position = startPos + travel * parallaxOffset;
    }
}
```

#### b) Phân tích cơ chế hoạt động

**Bước 1 — Ghi nhớ mốc ban đầu.** Trong `Start()`, script lưu vị trí ban đầu của lớp nền vào `startPos`. Đây là mốc để tính toán mọi dịch chuyển về sau.

**Bước 2 — Tính quãng đường camera đã đi.** Thuộc tính `travel` (dùng cú pháp expression-bodied property của C#, tính lại mỗi lần được gọi) lấy vị trí hiện tại của camera trừ đi mốc `startPos`.

**Bước 3 — Dịch chuyển lớp nền theo tỉ lệ.** Mỗi `FixedUpdate()`, lớp nền được đặt lại vị trí bằng công thức:

```
Vị trí mới = Vị trí gốc + (Quãng đường camera đi được × Hệ số parallax)
```

**Ý nghĩa của hệ số `parallaxOffset = -0.15f`:**

| Giá trị | Hiệu ứng thị giác |
|---|---|
| `0` | Lớp nền đứng yên hoàn toàn so với thế giới (không có parallax) |
| `-0.15` (đang dùng) | Lớp nền dịch nhẹ **ngược hướng** camera → trông như ở rất xa |
| Càng gần `-1` | Lớp nền càng có vẻ ở xa vô cực (gần như dán chặt vào màn hình) |
| Giá trị dương | Lớp nền chạy cùng hướng camera → trông như ở gần hơn mặt đất |

**Vì sao dùng `FixedUpdate()` chứ không phải `Update()`?** Vì nhân vật được di chuyển bằng `Rigidbody2D.MovePosition()` trong `FixedUpdate()`, và camera Cinemachine bám theo nhân vật. Cập nhật parallax cùng nhịp với vật lý giúp lớp nền không bị giật/rung so với nhân vật.

**Cách mở rộng nhiều lớp:** Gắn script này cho nhiều GameObject nền khác nhau với `parallaxOffset` khác nhau (ví dụ lớp mây `-0.4`, lớp núi xa `-0.25`, lớp cây gần `-0.1`) sẽ tạo ra chiều sâu nhiều tầng.

#### c) Các hiệu ứng khác của màn hình chơi game

Ngoài parallax, màn hình chơi game còn có hai kỹ thuật đáng trình bày:

**Camera bám nhân vật (Cinemachine):** `CameraController.cs` gán mục tiêu cho camera ảo mỗi khi vào scene mới:

```csharp
public void SetPlayerCameraFollow()
{
    cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
    cinemachineVirtualCamera.Follow = PlayerController.Instance.transform;
}
```

**Vật cản trong suốt (`TransparentDetection.cs`):** Khi nhân vật đi vào sau một vật thể cao (cây, mái nhà) hoặc một vùng Tilemap, vật đó mờ dần xuống 80% độ trong suốt trong 0.4 giây để người chơi vẫn thấy nhân vật; khi đi ra thì hiện lại như cũ. Script xử lý được cả `SpriteRenderer` lẫn `Tilemap` bằng hai coroutine nạp chồng (overload):

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (other.gameObject.GetComponent<PlayerController>())
    {
        if (spriteRenderer)
            StartCoroutine(FadeRoutine(spriteRenderer, fadeTime,
                                       spriteRenderer.color.a, transparencyAmount));
        else if (tilemap)
            StartCoroutine(FadeRoutine(tilemap, fadeTime,
                                       tilemap.color.a, transparencyAmount));
    }
}
```

---

### 7.3 Các đối tượng trong game

Yêu cầu đề bài là "một hoặc nhiều đối tượng game" — dự án hiện có **5 nhóm đối tượng** đang hoạt động.

#### a) Nhân vật người chơi (Player)

Script chính: `PlayerController.cs`

| Chức năng | Cách cài đặt |
|---|---|
| Di chuyển 8 hướng | Đọc `Vector2` từ action "Move" (bàn phím WASD), di chuyển bằng `rb.MovePosition()` trong `FixedUpdate()` |
| Animation theo hướng | Truyền `movement.x`, `movement.y` vào Animator qua `SetFloat("moveX"/"moveY")` để Blend Tree chọn animation phù hợp |
| Lật sprite theo hướng ngắm | So sánh vị trí con trỏ với vị trí nhân vật trên màn hình → đặt `spriteRenderer.flipX` |
| Dash (lướt né) | Nhân tạm `moveSpeed` với `dashSpeed = 4`, bật `TrailRenderer` tạo vệt sáng, dùng coroutine để kết thúc sau 0.2 giây và cooldown 0.25 giây |

```csharp
private IEnumerator EndDashRoutine()
{
    float dashTime = .2f;
    float dashCD = .25f;
    yield return new WaitForSeconds(dashTime);
    moveSpeed = startingMoveSpeed;          // Trả tốc độ về bình thường
    myTrailRenderer.emitting = false;
    yield return new WaitForSeconds(dashCD);
    isDashing = false;                      // Hết cooldown, cho phép dash tiếp
}
```

#### b) Hệ thống vũ khí

Gồm 3 vũ khí (`Sword` — kiếm, `Bow` — cung, `Staff` — gậy phép) cùng triển khai interface `IWeapon`, quản lý bởi `ActiveWeapon.cs` và `ActiveInventory.cs`.

**Luồng đổi vũ khí:** Người chơi bấm phím 1–5 → `ActiveInventory` bật khung sáng ở ô tương ứng → hủy vũ khí đang cầm → đọc `WeaponInfo` (ScriptableObject) của ô đó → sinh prefab vũ khí mới và gắn làm con của `ActiveWeapon`.

**Luồng tấn công:**

```
Giữ chuột trái → ActiveWeapon.attackButtonDown = true
   → Update() phát hiện đang bấm và chưa trong lúc tấn công
   → gọi (CurrentActiveWeapon as IWeapon).Attack()
   → Sword.Attack(): kích hoạt animation chém, bật hitbox (weaponCollider),
                     sinh hiệu ứng vệt chém, chờ cooldown 0.5 giây
   → Animation Event gọi DoneAttackingAnimEvent() để tắt hitbox
```

Vũ khí luôn xoay theo hướng ngắm nhờ hàm `MouseFollowWithOffset()` — tính góc bằng `Mathf.Atan2` rồi đặt `rotation`, đồng thời lật vũ khí sang trái/phải cho khớp hướng nhân vật.

#### c) Quái vật (Enemy)

Ba component phối hợp:

**`EnemyAI.cs` — bộ não.** Máy trạng thái (state machine) hiện có một trạng thái `Roaming`: cứ mỗi 2 giây chọn một hướng ngẫu nhiên rồi ra lệnh di chuyển.

```csharp
private IEnumerator RoamingRoutine()
{
    while (state == State.Roaming)
    {
        Vector2 roamPosition = GetRoamingPosition();
        enemyPathfinding.MoveTo(roamPosition);
        yield return new WaitForSeconds(roamChangeDirFloat);
    }
}

private Vector2 GetRoamingPosition()
{
    return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
}
```

Cấu trúc `enum State` cho phép mở rộng thêm các trạng thái khác (đuổi theo người chơi, tấn công, bỏ chạy) mà không phá vỡ code cũ.

**`EnemyPathfinding.cs` — đôi chân.** Nhận hướng từ AI và di chuyển bằng `Rigidbody2D`. Có một chi tiết quan trọng: nếu đang bị hất lùi thì **dừng xử lý di chuyển**, để lực knockback phát huy tác dụng:

```csharp
private void FixedUpdate()
{
    if (knockback.GettingKnockedBack) { return; }
    rb.MovePosition(rb.position + moveDir * (moveSpeed * Time.fixedDeltaTime));
}
```

**`EnemyHealth.cs` — máu và cái chết.** Máu khởi điểm 3. Khi nhận sát thương sẽ đồng thời: trừ máu, gọi knockback, gọi hiệu ứng nhấp nháy trắng, rồi sau 0.2 giây mới kiểm tra chết — cách làm này giúp người chơi **kịp nhìn thấy phản hồi** trước khi quái vật biến mất.

#### d) Vật thể phá hủy được (`Destructible.cs`)

Các vật trang trí (thùng gỗ, bụi cỏ) có collider dạng trigger; khi bị `DamageSource` (hitbox vũ khí) chạm vào thì sinh hiệu ứng vỡ và tự hủy.

#### e) Hệ thống phản hồi khi trúng đòn (game feel)

| Script | Hiệu ứng | Cách làm |
|---|---|---|
| `Flash.cs` | Nhấp nháy trắng | Đổi tạm `Material` của sprite sang material trắng trong 0.2 giây rồi trả về material gốc |
| `Knockback.cs` | Hất lùi | Tính vector từ nguồn sát thương tới nạn nhân, `AddForce` với `ForceMode2D.Impulse`, sau 0.2 giây đặt lại `velocity = 0` |
| `SlashAnim.cs` | Vệt chém | Particle System tự hủy khi phát xong (`!ps.IsAlive()`) |
| `DamageSource.cs` | Gây sát thương | `OnTriggerEnter2D` tìm `EnemyHealth` trên đối tượng va chạm và gọi `TakeDamage()` |

Ba yếu tố trên (flash + knockback + particle) là những kỹ thuật "game feel" cơ bản làm cho cú đánh có cảm giác đã tay, dù về mặt logic chỉ đơn giản là trừ một điểm máu.

---

## 8. Phần mở rộng cá nhân: chuyển sang nền tảng Android

Ngoài phạm vi tutorial, dự án đã được mở rộng để build và chơi được trên điện thoại Android.

### 8.1 Vấn đề gặp phải

Bản gốc phụ thuộc hoàn toàn vào bàn phím và chuột:

| Chức năng | Điều khiển gốc (PC) | Vấn đề trên Android |
|---|---|---|
| Di chuyển | Phím W/A/S/D | Điện thoại không có bàn phím |
| Hướng ngắm / xoay vũ khí | Vị trí con trỏ chuột (`Input.mousePosition`, dùng ở 4 chỗ trong code) | Không có con trỏ chuột |
| Tấn công | Chuột trái | Không có nút chuột |
| Lướt (Dash) | Phím Space | Không có bàn phím |
| Đổi vũ khí | Phím 1–5 | Không có bàn phím |

### 8.2 Giải pháp

Thiết kế theo mô hình **twin-stick** (hai cần điều khiển ảo) — mô hình phổ biến của game hành động góc nhìn từ trên xuống trên di động:

| Điều khiển mới | Chức năng |
|---|---|
| Joystick ảo bên trái | Di chuyển nhân vật |
| Joystick ảo bên phải | Vừa quyết định hướng ngắm, vừa tự động tấn công khi kéo (tương đương "vừa rê chuột vừa giữ chuột trái" trên PC) |
| Nút DASH | Lướt né |
| Chạm vào ô túi đồ | Đổi vũ khí (thay cho phím 1–5) |

Ba script mới trong `Assets/Scripts/Mobile/`:

- **`MobileInput.cs`** — lớp tĩnh lưu trạng thái input cảm ứng dùng chung (hướng di chuyển, hướng ngắm).
- **`OnScreenJoystick.cs`** — joystick ảo kéo-thả, cài đặt các interface `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler` của hệ thống UI Unity. Một script dùng chung cho cả hai cần, phân biệt qua thuộc tính `Role` (Move / Aim).
- **`MobileControlsBootstrap.cs`** — tự động dựng toàn bộ giao diện điều khiển (Canvas, hai joystick, nút Dash) **bằng code lúc chạy game**, nhờ thuộc tính `[RuntimeInitializeOnLoadMethod]`, nên không phải chỉnh sửa scene thủ công. Giao diện này chỉ hiện trên thiết bị di động và trong Editor để tiện thử nghiệm, không hiện trên bản build PC.

Nguyên tắc khi sửa các script gốc: **giữ nguyên đường điều khiển bằng chuột/bàn phím**, chỉ thêm nhánh xử lý cảm ứng khi joystick đang được sử dụng. Ví dụ trong `PlayerController.cs`:

```csharp
Vector2 keyboardMove = playerControls.Movement.Move.ReadValue<Vector2>();
movement = keyboardMove.sqrMagnitude > 0.01f ? keyboardMove : MobileInput.MoveInput;
```

Nhờ vậy **cùng một mã nguồn chạy được trên cả PC lẫn Android**, không cần duy trì hai phiên bản.

### 8.3 Cấu hình build Android

| Thiết lập | Giá trị | Lý do |
|---|---|---|
| Platform | Android | Chuyển bằng `Switch Platform` trong Build Settings |
| Minimum API Level | 22 (Android 5.1) | Hỗ trợ máy cũ |
| Orientation | Chỉ cho phép Landscape (đã tắt Portrait) | Bố cục gameplay và hai joystick được thiết kế cho màn hình ngang |
| Kiến trúc CPU | ARMv7 / ARM64 | Kiến trúc chip của điện thoại thật |

---

## 9. Hạn chế hiện tại và hướng phát triển

| Hạn chế | Hướng khắc phục |
|---|---|
| Chưa có màn hình vào game (splash/home screen) | Xây scene Menu theo thiết kế ở mục 7.1 |
| Người chơi chưa có máu, chưa thể chết | Thêm `PlayerHealth` tương tự `EnemyHealth`, kèm thanh máu UI |
| Cung và gậy phép mới chỉ xoay theo hướng ngắm, chưa bắn đạn | Thêm prefab đạn + script `Projectile` di chuyển và gây sát thương |
| AI quái vật mới có trạng thái đi lang thang | Bổ sung trạng thái `Chasing` và `Attacking` vào `enum State` sẵn có |
| Chưa có âm thanh | Thêm `AudioSource` cho tiếng chém, tiếng trúng đòn, nhạc nền |
| Chưa có hệ thống lưu game | Dùng `PlayerPrefs` hoặc ghi file JSON |
| Target API Level còn thấp (32) | Cài thêm SDK Platform 34 để đạt chuẩn hiện hành của Google Play |

---

## 10. Nội dung trình bày dự kiến

**Thời lượng ước tính: 7–10 phút**

| Thứ tự | Nội dung | Thời lượng | Ghi chú |
|---|---|---|---|
| 1 | Giới thiệu dự án, công nghệ dùng (mục 1–3) | 1 phút | Nói nhanh, không đi sâu |
| 2 | Demo màn hình chơi game trong Play Mode | 2 phút | Cho thấy nhân vật di chuyển, đánh quái, phá thùng |
| 3 | Trình bày kỹ thuật parallax background (mục 7.2) | 3 phút | **Trọng tâm** — mở script `Parallax.cs`, giải thích công thức, thử đổi `parallaxOffset` để thầy thấy khác biệt trực tiếp |
| 4 | Trình bày các đối tượng game (mục 7.3) | 2 phút | Nhấn mạnh interface `IWeapon` và cách phối hợp component của quái vật |
| 5 | Nêu hiện trạng màn hình vào game (mục 7.1) | 1 phút | Nói thẳng là chưa làm, trình bày thiết kế dự kiến và các thành phần đã có sẵn để tái sử dụng |
| 6 | (Nếu còn thời gian) Demo bản Android trên điện thoại | 1 phút | Điểm cộng, cho thấy phần mở rộng ngoài tutorial |

**Một số câu hỏi thầy có thể hỏi và gợi ý trả lời:**

- *"Vì sao hệ số parallax lại âm?"* → Để lớp nền dịch ngược hướng camera, làm nó có vẻ ở rất xa; nếu để dương thì nền chạy cùng hướng, trông như ở gần mặt đất hơn.
- *"Vì sao cập nhật parallax trong `FixedUpdate` mà không phải `Update`?"* → Vì nhân vật di chuyển bằng vật lý trong `FixedUpdate` và camera bám theo nhân vật; cùng nhịp cập nhật thì nền không bị rung so với nhân vật.
- *"Muốn thêm vũ khí mới thì phải sửa những gì?"* → Chỉ cần tạo script mới triển khai interface `IWeapon`, tạo prefab và một asset `WeaponInfo`; không phải sửa `ActiveWeapon` hay `ActiveInventory`.
- *"Tại sao dùng Singleton?"* → Để nhân vật và giao diện không bị hủy khi chuyển scene và để các script truy cập nhanh mà không cần gán tham chiếu thủ công. Nhược điểm là làm tăng phụ thuộc toàn cục, dự án lớn hơn nên cân nhắc dùng hệ thống sự kiện thay thế.

---

## 11. Phụ lục: danh sách toàn bộ script

| # | Script | Nhóm | Chức năng |
|---|---|---|---|
| 1 | `PlayerController.cs` | Player | Di chuyển, animation, dash, hướng quay mặt |
| 2 | `ActiveWeapon.cs` | Player | Quản lý vũ khí đang cầm, xử lý nút tấn công |
| 3 | `Sword.cs` | Player | Kiếm: animation chém, bật/tắt hitbox, xoay theo hướng ngắm |
| 4 | `SlashAnim.cs` | Player | Hiệu ứng vệt chém, tự hủy khi phát xong |
| 5 | `DamageSource.cs` | Player | Hitbox gây sát thương lên quái vật |
| 6 | `Player Controls.cs` | Player | Class do Unity sinh tự động từ file `.inputactions` |
| 7 | `EnemyAI.cs` | Enemies | Máy trạng thái, hành vi đi lang thang |
| 8 | `EnemyPathfinding.cs` | Enemies | Thực thi di chuyển bằng Rigidbody2D |
| 9 | `EnemyHealth.cs` | Enemies | Máu, nhận sát thương, hiệu ứng chết |
| 10 | `ActiveInventory.cs` | UI | Chọn ô túi đồ, sinh vũ khí tương ứng |
| 11 | `InventorySlot.cs` | UI | Lưu thông tin vũ khí của một ô, xử lý chạm |
| 12 | `WeaponInfo.cs` | UI | ScriptableObject chứa prefab và cooldown vũ khí |
| 13 | `IWeapon.cs` | UI | Interface chung cho mọi vũ khí |
| 14 | `Bow.cs` | UI | Cung (hiện mới có khung tấn công) |
| 15 | `Staff.cs` | UI | Gậy phép, xoay theo hướng ngắm |
| 16 | `Singleton.cs` | Management | Lớp Singleton generic dùng chung |
| 17 | `BaseSingleton.cs` | Management | Singleton cho object gốc giữ qua các scene |
| 18 | `SceneManagement.cs` | Management | Ghi nhớ cổng chuyển cảnh |
| 19 | `CameraController.cs` | Management | Gán mục tiêu bám cho camera Cinemachine |
| 20 | `UIFade.cs` | Management | Hiệu ứng mờ dần sang đen và ngược lại |
| 21 | `AreaExit.cs` | Management | Cổng ra: kích hoạt chuyển scene |
| 22 | `AreaEntrance.cs` | Management | Cổng vào: đặt vị trí nhân vật ở scene mới |
| 23 | `Parallax.cs` | Misc | **Background chuyển động** |
| 24 | `Knockback.cs` | Misc | Hất lùi khi trúng đòn |
| 25 | `Flash.cs` | Misc | Nhấp nháy trắng khi trúng đòn |
| 26 | `Destructible.cs` | Misc | Vật thể phá hủy được |
| 27 | `TransparentDetection.cs` | Misc | Làm mờ vật cản che nhân vật |
| 28 | `MouseFollow.cs` | Misc | Xoay đối tượng theo hướng ngắm |
| 29 | `MobileInput.cs` | Mobile | Lưu trạng thái input cảm ứng |
| 30 | `OnScreenJoystick.cs` | Mobile | Joystick ảo kéo-thả |
| 31 | `MobileControlsBootstrap.cs` | Mobile | Tự dựng giao diện điều khiển cảm ứng |
