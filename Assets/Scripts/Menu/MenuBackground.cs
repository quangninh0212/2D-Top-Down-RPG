using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The menu backdrop: a lit gateway standing behind ridgelines that slide past at
// different speeds, with embers rising through it. The speeds are the point -
// the far ridge barely moves while the near one slides, which is the same
// parallax idea the gameplay background uses, only laid out for a still screen.
public class MenuBackground : MonoBehaviour
{
    private class Ridge
    {
        public RectTransform[] Tiles;   // two copies, leapfrogging each other
        public float Speed;
        public float Width;
        public float HeightFraction;
    }

    private class Ember
    {
        public RectTransform Rect;
        public Vector2 Drift;
        public float SwayPhase;
        public float SwayWidth;
        public float Spin;
    }

    private readonly List<Ridge> ridges = new List<Ridge>();
    private readonly List<Ember> embers = new List<Ember>();

    private readonly List<RectTransform> gateParts = new List<RectTransform>();
    private readonly List<Vector2> gateFractions = new List<Vector2>();

    private RectTransform area;
    private RectTransform glow;
    private float glowPulse;
    private Vector2 laidOutFor;

    public void Build(RectTransform parent)
    {
        area = parent;
        Rect bounds = area.rect;

        AddBackdrop();
        AddGateway(bounds);

        // Far to near: dimmer and slower behind, darker and quicker in front.
        AddRidge(bounds, 0.46f, 12f, new Color(0.09f, 0.20f, 0.17f, 0.85f), 0.0f, 150f, 46f);
        AddRidge(bounds, 0.34f, 26f, new Color(0.05f, 0.13f, 0.11f, 0.92f), 1.3f, 140f, 40f);
        AddRidge(bounds, 0.22f, 48f, new Color(0.02f, 0.06f, 0.05f, 1f), 2.6f, 130f, 34f);

        AddEmbers(bounds, 22);
        AddVignette();

        laidOutFor = bounds.size;
    }

    // The canvas is only scaled to its reference resolution once CanvasScaler has
    // run, which is after everything here was built, so the sizes taken at build
    // time can be wrong until then. Redoing the layout on any size change covers
    // that first frame as well as later window resizes.
    private void Relayout(Rect bounds)
    {
        laidOutFor = bounds.size;

        for (int i = 0; i < gateParts.Count; i++)
        {
            gateParts[i].sizeDelta = new Vector2(bounds.width * gateFractions[i].x,
                                                 bounds.height * gateFractions[i].y);
        }

        foreach (Ridge ridge in ridges)
        {
            float tileWidth = bounds.width * 1.05f;
            float tileHeight = bounds.height * ridge.HeightFraction;
            ridge.Width = tileWidth;

            for (int i = 0; i < ridge.Tiles.Length; i++)
            {
                ridge.Tiles[i].sizeDelta = new Vector2(tileWidth, tileHeight);
                ridge.Tiles[i].anchoredPosition = new Vector2(i * tileWidth, 0f);
            }
        }
    }

    private void TrackGatePart(RectTransform rect, Rect bounds)
    {
        gateParts.Add(rect);
        gateFractions.Add(new Vector2(rect.sizeDelta.x / bounds.width, rect.sizeDelta.y / bounds.height));
    }

    private void AddBackdrop()
    {
        Image backdrop = NewImage("Backdrop", area);
        Stretch(backdrop.rectTransform);
        backdrop.sprite = MenuArt.VerticalGradient(64,
            new Color(0.015f, 0.035f, 0.030f), new Color(0.05f, 0.13f, 0.13f));
    }

    // The gate the game is named for: a dark arch with light behind it.
    private void AddGateway(Rect bounds)
    {
        float archHeight = bounds.height * 0.72f;
        float archWidth = archHeight * 0.62f;

        Image halo = NewImage("GateGlow", area);
        halo.sprite = MenuArt.SoftGlow(128, Color.white);
        halo.color = new Color(0.45f, 0.95f, 0.80f, 0.5f);
        halo.rectTransform.sizeDelta = new Vector2(archWidth * 2.6f, archHeight * 2.1f);
        halo.rectTransform.anchoredPosition = new Vector2(0f, -bounds.height * 0.06f);
        glow = halo.rectTransform;

        Image mouth = NewImage("GateMouth", area);
        mouth.sprite = MenuArt.SoftGlow(128, Color.white);
        mouth.color = new Color(0.75f, 1f, 0.92f, 0.30f);
        mouth.rectTransform.sizeDelta = new Vector2(archWidth * 0.95f, archHeight * 0.95f);
        mouth.rectTransform.anchoredPosition = new Vector2(0f, -bounds.height * 0.06f);

        Image arch = NewImage("GateArch", area);
        arch.sprite = MenuArt.Arch(320, 460, 26f, Color.white);
        arch.color = new Color(0.02f, 0.05f, 0.05f, 0.97f);
        arch.rectTransform.sizeDelta = new Vector2(archWidth, archHeight);
        arch.rectTransform.anchoredPosition = new Vector2(0f, -bounds.height * 0.10f);

        TrackGatePart(halo.rectTransform, bounds);
        TrackGatePart(mouth.rectTransform, bounds);
        TrackGatePart(arch.rectTransform, bounds);
    }

    private void AddRidge(Rect bounds, float heightFraction, float speed, Color colour,
                          float phase, float baseHeight, float amplitude)
    {
        Sprite sprite = MenuArt.HillBand(768, 256, baseHeight, amplitude, phase, Color.white);

        float tileWidth = bounds.width * 1.05f;
        float tileHeight = bounds.height * heightFraction;

        RectTransform[] tiles = new RectTransform[2];

        for (int i = 0; i < 2; i++)
        {
            Image image = NewImage("Ridge", area);
            image.sprite = sprite;
            image.color = colour;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(tileWidth, tileHeight);
            rect.anchoredPosition = new Vector2(i * tileWidth, 0f);

            tiles[i] = rect;
        }

        ridges.Add(new Ridge { Tiles = tiles, Speed = speed, Width = tileWidth, HeightFraction = heightFraction });
    }

    private void AddEmbers(Rect bounds, int count)
    {
        Sprite sprite = MenuArt.SoftGlow(64, Color.white);

        for (int i = 0; i < count; i++)
        {
            Image image = NewImage("Ember", area);
            image.sprite = sprite;
            image.color = Random.value < 0.4f
                ? new Color(0.6f, 1f, 0.85f, Random.Range(0.35f, 0.6f))
                : new Color(1f, 0.85f, 0.55f, Random.Range(0.3f, 0.55f));

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            float size = Random.Range(10f, 34f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(
                Random.Range(-bounds.width * 0.5f, bounds.width * 0.5f),
                Random.Range(-bounds.height * 0.5f, bounds.height * 0.5f));

            embers.Add(new Ember
            {
                Rect = rect,
                Drift = new Vector2(Random.Range(-6f, 6f), Random.Range(16f, 46f)),
                SwayPhase = Random.Range(0f, Mathf.PI * 2f),
                SwayWidth = Random.Range(8f, 26f),
                Spin = Random.Range(0.5f, 1.2f)
            });
        }
    }

    private void AddVignette()
    {
        Image vignette = NewImage("Vignette", area);
        Stretch(vignette.rectTransform);
        vignette.sprite = MenuArt.VerticalGradient(64,
            new Color(0f, 0f, 0f, 0.6f), new Color(0f, 0f, 0f, 0.25f));
    }

    private void Update()
    {
        if (area == null) { return; }

        Rect bounds = area.rect;
        if ((bounds.size - laidOutFor).sqrMagnitude > 1f) { Relayout(bounds); }

        float delta = Time.unscaledDeltaTime;

        ScrollRidges(delta);
        DriftEmbers(delta);
        PulseGlow(delta);
    }

    private void ScrollRidges(float delta)
    {
        foreach (Ridge ridge in ridges)
        {
            foreach (RectTransform tile in ridge.Tiles)
            {
                Vector2 position = tile.anchoredPosition;
                position.x -= ridge.Speed * delta;

                // Once a tile is fully past the left edge, send it round the back.
                if (position.x <= -ridge.Width) { position.x += ridge.Width * 2f; }

                tile.anchoredPosition = position;
            }
        }
    }

    private void DriftEmbers(float delta)
    {
        Rect bounds = area.rect;
        float edgeX = bounds.width * 0.5f + 50f;
        float edgeY = bounds.height * 0.5f + 50f;

        foreach (Ember ember in embers)
        {
            Vector2 position = ember.Rect.anchoredPosition + ember.Drift * delta;

            ember.SwayPhase += delta * ember.Spin;
            position.x += Mathf.Sin(ember.SwayPhase) * ember.SwayWidth * delta;

            if (position.y > edgeY) { position.y = -edgeY; }
            if (position.x > edgeX) { position.x = -edgeX; }
            else if (position.x < -edgeX) { position.x = edgeX; }

            ember.Rect.anchoredPosition = position;
        }
    }

    private void PulseGlow(float delta)
    {
        if (glow == null) { return; }

        glowPulse += delta * 0.7f;

        float breathe = 1f + Mathf.Sin(glowPulse) * 0.05f;
        glow.localScale = new Vector3(breathe, breathe, 1f);
    }

    private static Image NewImage(string name, Transform parent)
    {
        GameObject imageGO = new GameObject(name, typeof(Image));
        imageGO.transform.SetParent(parent, false);

        Image image = imageGO.GetComponent<Image>();
        image.raycastTarget = false;

        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
