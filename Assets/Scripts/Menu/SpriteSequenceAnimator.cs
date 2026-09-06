using UnityEngine;
using UnityEngine.UI;

// Plays a sprite sheet through an Image at a fixed frame rate. Used for the
// menu previews, where an Animator would mean carrying a controller asset.
public class SpriteSequenceAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 8f;

    private Image image;
    private float timer;
    private int index;

    public void Play(Sprite[] sequence, float fps)
    {
        frames = sequence;
        framesPerSecond = Mathf.Max(1f, fps);

        image = GetComponent<Image>();

        if (image != null && frames != null && frames.Length > 0)
        {
            image.sprite = frames[0];
        }
    }

    private void Update()
    {
        if (image == null || frames == null || frames.Length < 2) { return; }

        timer += Time.unscaledDeltaTime;

        float step = 1f / framesPerSecond;
        if (timer < step) { return; }

        timer -= step;
        index = (index + 1) % frames.Length;
        image.sprite = frames[index];
    }
}
