using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stamina : Singleton<Stamina>
{
    public static event Action<int, int> OnStaminaChanged;

    public int CurrentStamina { get; private set; }

    [SerializeField] private Sprite fullStaminaImage, emptyStaminaImage;
    [SerializeField] private int timeBetweenStaminaRefresh = 3;

    private const string StaminaContainerName = "Stamina Container";

    private Transform staminaContainer;
    private int startingStamina = 3;
    private int maxStamina;
    private Coroutine refreshRoutine;

    public int MaxStamina
    {
        get { return maxStamina; }
    }

    protected override void Awake()
    {
        base.Awake();

        maxStamina = startingStamina;
        CurrentStamina = startingStamina;
    }

    private void Start()
    {
        // The container belongs to the scene's UI canvas, which is rebuilt on
        // every load, so it is looked up again here rather than cached forever.
        staminaContainer = null;
        UpdateStaminaImages();
    }

    public void UseStamina()
    {
        if (CurrentStamina <= 0) { return; }

        CurrentStamina--;
        UpdateStaminaImages();
    }

    public void RefreshStamina()
    {
        if (CurrentStamina < maxStamina)
        {
            CurrentStamina++;
            AudioManager.PlaySfx(GameSfx.StaminaPickup);
        }

        UpdateStaminaImages();
    }

    public void ApplyLoadedStamina(int current, int max)
    {
        maxStamina = Mathf.Max(1, max);
        CurrentStamina = Mathf.Clamp(current, 0, maxStamina);
        UpdateStaminaImages();
    }

    private IEnumerator RefreshStaminaRoutine()
    {
        while (CurrentStamina < maxStamina)
        {
            yield return new WaitForSeconds(timeBetweenStaminaRefresh);
            RefreshStaminaSilently();
        }

        refreshRoutine = null;
    }

    // The pickup version plays a sound; the passive regeneration should not.
    private void RefreshStaminaSilently()
    {
        if (CurrentStamina < maxStamina) { CurrentStamina++; }
        UpdateStaminaImages();
    }

    private void UpdateStaminaImages()
    {
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);

        if (staminaContainer == null)
        {
            GameObject containerGO = GameObject.Find(StaminaContainerName);
            if (containerGO != null) { staminaContainer = containerGO.transform; }
        }

        if (staminaContainer != null)
        {
            for (int i = 0; i < maxStamina && i < staminaContainer.childCount; i++)
            {
                Image image = staminaContainer.GetChild(i).GetComponent<Image>();
                if (image == null) { continue; }

                image.sprite = i <= CurrentStamina - 1 ? fullStaminaImage : emptyStaminaImage;
            }
        }

        // Only one regeneration loop at a time; StopAllCoroutines would also
        // kill unrelated routines on this object.
        if (CurrentStamina < maxStamina && refreshRoutine == null && isActiveAndEnabled)
        {
            refreshRoutine = StartCoroutine(RefreshStaminaRoutine());
        }
    }
}
