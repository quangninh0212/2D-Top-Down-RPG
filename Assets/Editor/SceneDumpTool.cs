using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development helper: writes a readable hierarchy + component dump of every
// scene in Assets/Scenes to a text file, so the project layout can be inspected
// without opening the editor UI.
public static class SceneDumpTool
{
    [MenuItem("Tools/Soulbound Gate/Debug/Dump Scenes")]
    public static void DumpScenes()
    {
        StringBuilder sb = new StringBuilder();

        foreach (string path in Directory.GetFiles("Assets/Scenes", "*.unity"))
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            sb.AppendLine("================ " + path);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Dump(root.transform, 0, sb);
            }
        }

        Directory.CreateDirectory("C:/Users/Phong/AppData/Local/Temp/claude/d--Projects-2D-Top-Down-RPG/f8990103-8b59-40bd-b01b-6ec9fe5fcb7f/scratchpad/dump");
        File.WriteAllText("C:/Users/Phong/AppData/Local/Temp/claude/d--Projects-2D-Top-Down-RPG/f8990103-8b59-40bd-b01b-6ec9fe5fcb7f/scratchpad/dump/scenes.txt", sb.ToString());
        Debug.Log("[DUMP] wrote Temp/Dump/scenes.txt");
    }

    private static void Dump(Transform t, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        string prefab = PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
            ? " <prefab:" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) + ">"
            : "";

        sb.Append(indent).Append(t.name).Append(prefab)
          .Append("  pos=").Append(t.position.ToString("F2"))
          .Append(" layer=").Append(LayerMask.LayerToName(t.gameObject.layer))
          .AppendLine();

        foreach (Component c in t.GetComponents<Component>())
        {
            if (c == null) { sb.Append(indent).AppendLine("  ! MISSING SCRIPT"); continue; }
            if (c is Transform) { continue; }

            sb.Append(indent).Append("  [").Append(c.GetType().Name).Append("]");

            if (c is UnityEngine.Tilemaps.TilemapRenderer tr)
            {
                sb.Append(" sortingLayer=").Append(tr.sortingLayerName).Append(" order=").Append(tr.sortingOrder);
            }
            if (c is SpriteRenderer sr)
            {
                sb.Append(" sortingLayer=").Append(sr.sortingLayerName).Append(" order=").Append(sr.sortingOrder)
                  .Append(" sprite=").Append(sr.sprite != null ? sr.sprite.name : "none");
            }
            if (c is UnityEngine.Tilemaps.Tilemap tm)
            {
                sb.Append(" bounds=").Append(tm.cellBounds.ToString());
            }

            sb.AppendLine();
        }

        for (int i = 0; i < t.childCount; i++)
        {
            Dump(t.GetChild(i), depth + 1, sb);
        }
    }

}
