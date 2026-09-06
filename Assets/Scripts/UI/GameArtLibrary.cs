using UnityEngine;

// The bridge between the code-built UI and the project's imported artwork.
// The screens are created at runtime and so have no inspector to drag sprites
// into; this asset is filled in once by the editor setup tool and loaded from
// Resources instead.
[CreateAssetMenu(menuName = "Soulbound Gate/Game Art Library")]
public class GameArtLibrary : ScriptableObject
{
    private const string ResourcePath = "UI/GameArtLibrary";

    [Header("Weapons")]
    public Sprite swordIcon;
    public Sprite bowIcon;
    public Sprite staffIcon;

    [Header("HUD")]
    public Sprite heartFull;
    public Sprite heartEmpty;
    public Sprite staminaFull;
    public Sprite staminaEmpty;
    public Sprite goldCoin;
    public Sprite box;
    public Sprite border;

    [Header("Characters")]
    public Sprite playerIdle;
    public Sprite ghost;
    public Sprite slime;
    public Sprite grape;

    [Header("Idle animation frames (for the menu previews)")]
    // The menu shows the hero and a monster idling. Playing the frames through
    // a plain Image avoids instantiating the gameplay prefabs, which are
    // singletons and would fight the menu for input.
    public Sprite[] playerIdleFrames;

    // Whichever monster in the project has the best idle loop; the builder
    // picks it rather than the menu hard-coding one.
    public Sprite[] menuMonsterFrames;

    [Header("Environment")]
    public Sprite tree;
    public Sprite bush;
    public Sprite torch;

    private static GameArtLibrary cached;
    private static bool lookedUp;

    public static GameArtLibrary Instance
    {
        get
        {
            if (!lookedUp)
            {
                lookedUp = true;
                cached = Resources.Load<GameArtLibrary>(ResourcePath);

                if (cached == null)
                {
                    Debug.LogWarning("[GameArtLibrary] Missing Resources/" + ResourcePath +
                                     " - run Tools > Soulbound Gate > Complete Android Setup.");
                }
            }

            return cached;
        }
    }

    public Sprite WeaponIcon(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Bow: return bowIcon;
            case WeaponType.Staff: return staffIcon;
            default: return swordIcon;
        }
    }
}
