using System;
using TMPro;
using UnityEngine;

// The run's purse. Gold lives here while playing and is copied into the save
// file on every save; the shop reads and writes it through the same API.
public class EconomyManager : Singleton<EconomyManager>
{
    public static event Action<int> OnGoldChanged;

    private const string GoldTextName = "Gold Amount Text";

    private TMP_Text goldText;
    private int currentGold;

    public int CurrentGold
    {
        get { return currentGold; }
    }

    private void Start()
    {
        // A manager that survived a scene load has to re-find this scene's label.
        goldText = null;
        RefreshUI();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) { return; }

        currentGold += amount;
        RefreshUI();
        OnGoldChanged?.Invoke(currentGold);
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && currentGold >= amount;
    }

    // Returns false and changes nothing when the player cannot afford it, so
    // callers never have to guard against negative gold themselves.
    public bool SpendGold(int amount)
    {
        if (!CanAfford(amount)) { return false; }

        currentGold -= amount;
        RefreshUI();
        OnGoldChanged?.Invoke(currentGold);
        return true;
    }

    public void SetGold(int value)
    {
        currentGold = Mathf.Max(0, value);
        RefreshUI();
        OnGoldChanged?.Invoke(currentGold);
    }

    public int GetGold()
    {
        return currentGold;
    }

    // Kept so the existing Gold Coin pickup prefab keeps working unchanged.
    public void UpdateCurrentGold()
    {
        AddGold(1);
    }

    private void RefreshUI()
    {
        if (goldText == null)
        {
            GameObject labelGO = GameObject.Find(GoldTextName);
            if (labelGO == null) { return; }

            goldText = labelGO.GetComponent<TMP_Text>();
            if (goldText == null) { return; }
        }

        goldText.text = currentGold.ToString("D3");
    }
}
