using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Drifting motes of light over a dark gradient, echoing the fireflies in the
// game world. They are spread across three layers moving at different speeds,
// which is the same parallax idea the gameplay background uses: the slow, small,
// dim ones read as far away and the quick, large, bright ones as close.
public class MenuBackground : MonoBehaviour
{
    private class Mote
    {
        public RectTransform Rect;
        public Vector2 Drift;
        public float BobPhase;
        public float BobWidth;
    }

    private readonly List<Mote> motes = new List<Mote>();
    private RectTransform area;

    public void Build(RectTransform parent, Sprite glowSprite)
    {
        area = parent;

        // far, middle, near
        AddLayer(glowSprite, 26, 10f, 14f, new Color(0.65f, 0.9f, 0.7f, 0.30f));
        AddLayer(glowSprite, 16, 20f, 26f, new Color(0.85f, 1f, 0.75f, 0.45f));
        AddLayer(glowSprite, 9, 34f, 44f, new Color(1f, 0.96f, 0.7f, 0.65f));
    }

    private void AddLayer(Sprite glowSprite, int count, float speed, float size, Color colour)
    {
        Rect bounds = area.rect;

        for (int i = 0; i < count; i++)
        {
            GameObject moteGO = new GameObject("Mote", typeof(Image));
            moteGO.transform.SetParent(area, false);

            RectTransform rect = moteGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * Random.Range(size * 0.6f, size * 1.4f);
            rect.anchoredPosition = new Vector2(
                Random.Range(-bounds.width * 0.5f, bounds.width * 0.5f),
                Random.Range(-bounds.height * 0.5f, bounds.height * 0.5f));

            Image image = moteGO.GetComponent<Image>();
            image.sprite = glowSprite;
            image.color = colour;
            image.raycastTarget = false;

            motes.Add(new Mote
            {
                Rect = rect,
                Drift = new Vector2(Random.Range(-0.25f, 0.25f), 1f) * Random.Range(speed * 0.7f, speed * 1.3f),
                BobPhase = Random.Range(0f, Mathf.PI * 2f),
                BobWidth = Random.Range(6f, 20f)
            });
        }
    }

    private void Update()
    {
        if (area == null) { return; }

        Rect bounds = area.rect;
        float edgeX = bounds.width * 0.5f + 60f;
        float edgeY = bounds.height * 0.5f + 60f;

        foreach (Mote mote in motes)
        {
            Vector2 position = mote.Rect.anchoredPosition + mote.Drift * Time.unscaledDeltaTime;

            mote.BobPhase += Time.unscaledDeltaTime * 0.8f;
            position.x += Mathf.Sin(mote.BobPhase) * mote.BobWidth * Time.unscaledDeltaTime;

            // Wrap round rather than respawn, so the field never thins out.
            if (position.y > edgeY) { position.y = -edgeY; }
            if (position.x > edgeX) { position.x = -edgeX; }
            else if (position.x < -edgeX) { position.x = edgeX; }

            mote.Rect.anchoredPosition = position;
        }
    }
}
