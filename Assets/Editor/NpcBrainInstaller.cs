using UnityEditor;
using UnityEngine;

// Puts the right brain on each enemy prefab. Done here rather than by hand so
// that running the setup tool on a fresh clone produces the same enemies, and
// so every instance already placed in a level inherits the brain from its
// prefab instead of needing to be touched one by one.
public static class NpcBrainInstaller
{
    private const string SlimePrefab = "Assets/Prefabs/Enemies/Blue Slime.prefab";
    private const string GrapePrefab = "Assets/Prefabs/Enemies/Enemie1.prefab";
    private const string GhostPrefab = "Assets/Prefabs/Enemies/Ghost.prefab";

    [MenuItem("Tools/Soulbound Gate/Steps/Install NPC Brains")]
    public static void InstallAll()
    {
        int installed = 0;

        installed += Install<SlimePackBrain>(SlimePrefab) ? 1 : 0;
        installed += Install<GrapeThrowerBrain>(GrapePrefab) ? 1 : 0;
        installed += Install<GhostAmbusherBrain>(GhostPrefab) ? 1 : 0;

        AssetDatabase.SaveAssets();

        SoulboundSetupLog.Step("NPC brains installed (" + installed + " prefab(s) changed).");
    }

    // True when the prefab had to be changed, so re-running the tool on an
    // already-set-up project is silent.
    private static bool Install<T>(string prefabPath) where T : NpcBrain
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        if (root == null)
        {
            SoulboundSetupLog.Warn("Enemy prefab not found, no brain installed: " + prefabPath);
            return false;
        }

        bool changed = false;

        if (root.GetComponent<T>() == null)
        {
            root.AddComponent<T>();
            changed = true;
        }

        // The brain switches the old component off at runtime as well, but
        // leaving it enabled in the asset makes the prefab misleading to read.
        EnemyAI legacy = root.GetComponent<EnemyAI>();
        if (legacy != null && legacy.enabled)
        {
            legacy.enabled = false;
            changed = true;
        }

        if (changed) { PrefabUtility.SaveAsPrefabAsset(root, prefabPath); }

        PrefabUtility.UnloadPrefabContents(root);

        return changed;
    }
}
