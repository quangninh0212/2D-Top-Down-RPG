using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Fills in the GameArtLibrary asset that the code-built screens read from.
// Everything here points at sprites already in the project - nothing new is
// imported and nothing is copied.
public static class GameArtLibraryBuilder
{
    private const string Folder = "Assets/Resources/UI";
    private const string AssetPath = Folder + "/GameArtLibrary.asset";

    [MenuItem("Tools/Soulbound Gate/Rebuild Art Library")]
    public static GameArtLibrary Build()
    {
        Directory.CreateDirectory(Folder);

        GameArtLibrary library = AssetDatabase.LoadAssetAtPath<GameArtLibrary>(AssetPath);

        if (library == null)
        {
            library = ScriptableObject.CreateInstance<GameArtLibrary>();
            AssetDatabase.CreateAsset(library, AssetPath);
        }

        library.swordIcon = First("Assets/Sprites/UI/Sword.png");
        library.bowIcon = First("Assets/Sprites/UI/Bow.png");
        library.staffIcon = First("Assets/Sprites/UI/Staff.png");

        library.heartFull = First("Assets/Sprites/UI/Heart Full.png");
        library.heartEmpty = First("Assets/Sprites/UI/Heart Empty.png");
        library.staminaFull = First("Assets/Sprites/UI/Stamina Full.png");
        library.staminaEmpty = First("Assets/Sprites/UI/Stamina Empty.png");
        library.goldCoin = First("Assets/Sprites/UI/GoldCoin_WithOutline.png");
        library.box = First("Assets/Sprites/UI/Box.png");
        library.border = First("Assets/Sprites/UI/Border Empty.png");

        library.playerIdleFrames = All("Assets/Sprites/Player/Side animations/spr_player_right_idle.png");
        library.playerIdle = library.playerIdleFrames.Length > 0 ? library.playerIdleFrames[0] : null;

        Sprite[] ghostFrames = All("Assets/Sprites/Ghost/Ghost.png");
        library.ghost = ghostFrames.Length > 0 ? ghostFrames[0] : null;

        Sprite[] slimeFrames = AllFromFirstSheetIn("Assets/Sprites/Blue Slime");
        library.slime = slimeFrames.Length > 0 ? slimeFrames[0] : null;

        Sprite[] grapeFrames = All("Assets/Sprites/Enemie1/Grape Walk Sheet.png");
        library.grape = grapeFrames.Length > 0 ? grapeFrames[0] : null;

        // The menu wants something that actually loops. The Ghost image is a
        // single frame, so whichever monster sheet has the most frames wins.
        library.menuMonsterFrames = Longest(ghostFrames, slimeFrames, grapeFrames);

        library.tree = FirstFromAnyOf("Assets/Sprites/Vegetation");

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        SoulboundSetupLog.Step("Art library rebuilt at " + AssetPath + ".");
        return library;
    }

    // A sprite sheet imports as many sub-assets; a single image imports as one.
    private static Sprite[] All(string path)
    {
        List<Sprite> sprites = new List<Sprite>();

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite) { sprites.Add(sprite); }
        }

        // Sub-assets come back in import order, which is not always the frame
        // order, so they are sorted by name.
        sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));

        return sprites.ToArray();
    }

    private static Sprite First(string path)
    {
        Sprite[] sprites = All(path);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private static Sprite FirstFromAnyOf(string folder)
    {
        Sprite[] sprites = AllFromFirstSheetIn(folder);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private static Sprite[] AllFromFirstSheetIn(string folder)
    {
        if (!Directory.Exists(folder)) { return new Sprite[0]; }

        Sprite[] best = new Sprite[0];

        foreach (string file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
        {
            Sprite[] sprites = All(file.Replace('\\', '/'));
            if (sprites.Length > best.Length) { best = sprites; }
        }

        return best;
    }

    private static Sprite[] Longest(params Sprite[][] candidates)
    {
        Sprite[] best = new Sprite[0];

        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null && candidates[i].Length > best.Length) { best = candidates[i]; }
        }

        return best;
    }
}
