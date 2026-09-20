using System;
using System.Collections.Generic;
using UnityEngine;

// What one NPC knows, its neighbours find out. A brain that spots the player
// reports it here; every brain within earshot picks the report up and starts
// looking, which is what turns a room full of separate enemies into something
// that behaves like a group.
public static class NpcAlertNetwork
{
    // position of the sighting, and who reported it.
    public static event Action<Vector2, GameObject> OnSighting;

    // Counted for the smoke test, and reset with the scene.
    public static int SightingsReported { get; private set; }

    // Flank slots, handed out so a pack surrounds the player instead of every
    // member walking into the same spot behind it.
    private static readonly List<GameObject> slotOwners = new List<GameObject>();

    private static float lastAlertSoundTime = -10f;

    public static void Report(Vector2 position, GameObject reporter)
    {
        SightingsReported++;

        // One bark per group, not one per member: five slimes spotting the
        // player in the same frame should not sound like five.
        if (Time.time - lastAlertSoundTime > 0.6f)
        {
            lastAlertSoundTime = Time.time;
            AudioManager.PlaySfx(GameSfx.NpcAlert);
        }

        OnSighting?.Invoke(position, reporter);
    }

    // A stable index per NPC, so each one keeps its own approach angle for as
    // long as it lives.
    public static int SlotFor(GameObject npc)
    {
        if (npc == null) { return 0; }

        for (int i = 0; i < slotOwners.Count; i++)
        {
            if (slotOwners[i] == npc) { return i; }
        }

        // Reuse the place of an NPC that has died rather than growing forever.
        for (int i = 0; i < slotOwners.Count; i++)
        {
            if (slotOwners[i] == null)
            {
                slotOwners[i] = npc;
                return i;
            }
        }

        slotOwners.Add(npc);
        return slotOwners.Count - 1;
    }

    public static void Release(GameObject npc)
    {
        for (int i = 0; i < slotOwners.Count; i++)
        {
            if (slotOwners[i] == npc) { slotOwners[i] = null; }
        }
    }

    // Called when a level loads: slots and counters belong to one room.
    public static void Reset()
    {
        slotOwners.Clear();
        SightingsReported = 0;
        lastAlertSoundTime = -10f;
    }
}
