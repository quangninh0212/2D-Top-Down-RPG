using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The shop, reached from the main menu. It spends the run's gold directly and
// writes the save immediately, so a purchase can never be lost by closing the
// app afterwards.
public class ShopPanel : MonoBehaviour
{
    private const int HealPrice = 5;

    private class Row
    {
        public WeaponType Weapon;
        public bool IsHeal;
        public Button Action;
        public Text ActionLabel;
        public Text Status;
    }

    private CanvasGroup group;
    private Text goldLabel;
    private readonly List<Row> rows = new List<Row>();

    public bool IsOpen
    {
        get { return group != null && group.gameObject.activeSelf; }
    }

    public void Build()
    {
        group = PixelUI.NewFullScreenGroup("ShopRoot", transform);

        Image shade = PixelUI.NewImage("Shade", group.transform);
        PixelUI.Stretch(shade.rectTransform);
        shade.color = new Color(0f, 0f, 0f, 0.78f);

        RectTransform panel = PixelUI.NewPanel("Panel", group.transform, new Vector2(1120f, 700f));

        PixelUI.NewTitle("Heading", panel, "CỬA HÀNG", 56)
               .rectTransform.anchoredPosition = new Vector2(0f, 276f);

        goldLabel = PixelUI.NewBody("Gold", panel, "0 VÀNG", 38);
        goldLabel.rectTransform.sizeDelta = new Vector2(500f, 50f);
        goldLabel.rectTransform.anchoredPosition = new Vector2(0f, 210f);
        goldLabel.color = PixelUI.Gold;

        AddWeaponRow(panel, WeaponType.Bow, 110f);
        AddWeaponRow(panel, WeaponType.Staff, 10f);
        AddHealRow(panel, -90f);

        PixelUI.NewButton("Close", panel, "ĐÓNG", new Vector2(360f, 88f), new Vector2(0f, -270f), Close);

        PixelUI.SetGroupVisible(group, false);
    }

    private void AddWeaponRow(Transform parent, WeaponType weapon, float y)
    {
        GameArtLibrary art = GameArtLibrary.Instance;

        Image icon = PixelUI.NewImage("Icon_" + weapon, parent);
        icon.rectTransform.sizeDelta = new Vector2(76f, 76f);
        icon.rectTransform.anchoredPosition = new Vector2(-450f, y);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.sprite = art != null ? art.WeaponIcon(weapon) : null;

        Text name = PixelUI.NewBody("Name_" + weapon, parent, WeaponTypeInfo.DisplayName(weapon), 38);
        name.alignment = TextAnchor.MiddleLeft;
        name.rectTransform.sizeDelta = new Vector2(300f, 50f);
        name.rectTransform.anchoredPosition = new Vector2(-250f, y + 16f);

        Text status = PixelUI.NewBody("Status_" + weapon, parent, "", 28);
        status.alignment = TextAnchor.MiddleLeft;
        status.rectTransform.sizeDelta = new Vector2(620f, 44f);
        status.rectTransform.anchoredPosition = new Vector2(-90f, y - 22f);
        status.color = PixelUI.Muted;

        Button action = PixelUI.NewButton("Buy_" + weapon, parent, "MUA", new Vector2(240f, 76f),
                                          new Vector2(400f, y), () => BuyWeapon(weapon));

        rows.Add(new Row
        {
            Weapon = weapon,
            Action = action,
            ActionLabel = action.GetComponentInChildren<Text>(),
            Status = status
        });
    }

    private void AddHealRow(Transform parent, float y)
    {
        GameArtLibrary art = GameArtLibrary.Instance;

        Image icon = PixelUI.NewImage("Icon_Heal", parent);
        icon.rectTransform.sizeDelta = new Vector2(76f, 76f);
        icon.rectTransform.anchoredPosition = new Vector2(-450f, y);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.sprite = art != null ? art.heartFull : null;

        Text name = PixelUI.NewBody("Name_Heal", parent, "HỒI MÁU", 38);
        name.alignment = TextAnchor.MiddleLeft;
        name.rectTransform.sizeDelta = new Vector2(300f, 50f);
        name.rectTransform.anchoredPosition = new Vector2(-250f, y + 16f);

        Text status = PixelUI.NewBody("Status_Heal", parent, "", 28);
        status.alignment = TextAnchor.MiddleLeft;
        status.rectTransform.sizeDelta = new Vector2(620f, 44f);
        status.rectTransform.anchoredPosition = new Vector2(-90f, y - 22f);
        status.color = PixelUI.Muted;

        Button action = PixelUI.NewButton("Buy_Heal", parent, "MUA", new Vector2(240f, 76f),
                                          new Vector2(400f, y), BuyHeal);

        rows.Add(new Row
        {
            IsHeal = true,
            Action = action,
            ActionLabel = action.GetComponentInChildren<Text>(),
            Status = status
        });
    }

    // ----- state ----------------------------------------------------------

    public void Open()
    {
        Refresh();
        PixelUI.SetGroupVisible(group, true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        PixelUI.SetGroupVisible(group, false);
    }

    // With no run in progress the shop still opens, but there is no purse to
    // spend from - everything reads as locked rather than erroring.
    private bool HasRun
    {
        get
        {
            GameSaveManager save = GameSaveManager.Instance;
            return save != null && save.HasValidSave();
        }
    }

    private SaveData Data
    {
        get
        {
            GameSaveManager save = GameSaveManager.Instance;
            if (save == null) { return null; }

            // Reading a shop without a live run should reflect what is on disk.
            if (!save.RunEnded && save.Data != null && save.Data.runActive) { return save.Data; }

            SaveData onDisk = SaveSystem.Load();
            return onDisk ?? save.Data;
        }
    }

    private void Refresh()
    {
        SaveData data = Data;
        int gold = data != null && HasRun ? data.gold : 0;

        goldLabel.text = gold + " VÀNG";

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].IsHeal) { RefreshHealRow(rows[i], data, gold); }
            else { RefreshWeaponRow(rows[i], data, gold); }
        }
    }

    private void RefreshWeaponRow(Row row, SaveData data, int gold)
    {
        bool owned = data != null && HasRun && data.OwnsWeapon(row.Weapon);
        bool unlocked = HasRun && WeaponTypeInfo.IsUnlockedInShop(data, row.Weapon);
        int price = WeaponTypeInfo.ShopPrice(row.Weapon);

        if (owned)
        {
            row.Status.text = "ĐÃ SỞ HỮU";
            row.ActionLabel.text = "ĐÃ MUA";
            PixelUI.SetButtonEnabled(row.Action, false);
            return;
        }

        if (!unlocked)
        {
            int level = WeaponTypeInfo.UnlockedByLevel(row.Weapon);
            row.Status.text = HasRun
                ? "Hoàn thành Màn " + level + " để mở khóa."
                : "Cần một lượt chơi đang diễn ra.";
            row.ActionLabel.text = "KHÓA";
            PixelUI.SetButtonEnabled(row.Action, false);
            return;
        }

        row.Status.text = price + " Gold";
        row.ActionLabel.text = "MUA";
        PixelUI.SetButtonEnabled(row.Action, gold >= price);
    }

    private void RefreshHealRow(Row row, SaveData data, int gold)
    {
        if (!HasRun || data == null)
        {
            row.Status.text = "Cần một lượt chơi đang diễn ra.";
            row.ActionLabel.text = "KHÓA";
            PixelUI.SetButtonEnabled(row.Action, false);
            return;
        }

        bool full = data.currentHealth >= data.maxHealth;

        row.Status.text = full
            ? "Máu đã đầy (" + data.currentHealth + "/" + data.maxHealth + ")"
            : HealPrice + " Gold - hồi đầy máu (" + data.currentHealth + "/" + data.maxHealth + ")";

        row.ActionLabel.text = "MUA";
        PixelUI.SetButtonEnabled(row.Action, !full && gold >= HealPrice);
    }

    // ----- purchases ------------------------------------------------------

    private void BuyWeapon(WeaponType weapon)
    {
        SaveData data = Data;
        int price = WeaponTypeInfo.ShopPrice(weapon);

        if (!HasRun || data == null) { return; }
        if (data.OwnsWeapon(weapon) || !WeaponTypeInfo.IsUnlockedInShop(data, weapon)) { return; }

        if (data.gold < price)
        {
            AudioManager.PlaySfx(GameSfx.Denied);
            return;
        }

        data.gold -= price;
        data.SetOwnsWeapon(weapon, true);

        Commit(data);
        AudioManager.PlaySfx(GameSfx.Purchase);
        Refresh();
    }

    private void BuyHeal()
    {
        SaveData data = Data;

        if (!HasRun || data == null) { return; }
        if (data.currentHealth >= data.maxHealth) { return; }

        if (data.gold < HealPrice)
        {
            AudioManager.PlaySfx(GameSfx.Denied);
            return;
        }

        data.gold -= HealPrice;
        data.currentHealth = data.maxHealth;

        Commit(data);
        AudioManager.PlaySfx(GameSfx.Purchase);
        Refresh();
    }

    // Purchases happen in the menu, where the live gameplay objects do not
    // exist, so the change is written straight to disk.
    private void Commit(SaveData data)
    {
        GameSaveManager save = GameSaveManager.Instance;

        if (save != null && save.Data == data)
        {
            SaveSystem.Save(data);
            save.RaiseChanged();
            return;
        }

        SaveSystem.Save(data);

        if (save != null) { save.RaiseChanged(); }
    }
}
