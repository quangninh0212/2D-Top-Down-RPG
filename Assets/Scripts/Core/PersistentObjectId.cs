using UnityEngine;

// A stable identity for anything the save file has to remember - enemies,
// destructibles, gates. The id is generated once in the editor and stored in
// the scene, so it survives play sessions and duplicating an object produces a
// new one rather than a clash.
[DisallowMultipleComponent]
public class PersistentObjectId : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id
    {
        get { return id; }
    }

    public bool HasId
    {
        get { return !string.IsNullOrEmpty(id); }
    }

#if UNITY_EDITOR
    // Called by the editor setup tool. Never runs in a build.
    public void AssignId(string newId)
    {
        id = newId;
    }

    private void Reset()
    {
        id = System.Guid.NewGuid().ToString("N");
    }
#endif

    // Last-resort fallback: an object that reached play mode without an id
    // still gets a usable one for this session, derived from its scene path so
    // it at least stays consistent while the scene is loaded.
    private void Awake()
    {
        if (!HasId)
        {
            id = gameObject.scene.name + "/" + BuildPath(transform);
            Debug.LogWarning("[PersistentObjectId] '" + name + "' had no id; using a path-based fallback.");
        }
    }

    private static string BuildPath(Transform t)
    {
        string path = t.name + "@" + t.GetSiblingIndex();

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }
}
