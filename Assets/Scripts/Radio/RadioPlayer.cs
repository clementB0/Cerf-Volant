using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RadioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] playlist;
    [SerializeField, Range(0f, 1f)] private float volume = 0.1f;
    private AudioSource source;
    private int index = -1;
    private bool playing;
    private bool paused;

    private void Awake() => source = GetComponent<AudioSource>();

    private void Start()
    {
        source.volume = volume;
        playing = true;
        Skip(1);
    }

    private void Update()
    {
        source.volume = volume;
        if (playing && !source.isPlaying && !AudioListener.pause)
            Skip(1);
    }

    public void Skip(int direction)
    {
        int count = playlist?.Length ?? 0;
        for (int i = 0; i < count; i++)
        {
            index = (index + direction + count) % count;
            if (playlist[index] == null)
                continue;
            source.Stop();
            paused = false;
            source.clip = playlist[index];
            if (playing)
                source.Play();
            return;
        }
        source.Stop();
        source.clip = null;
        playing = paused = false;
        index = -1;
    }

    public void Toggle()
    {
        if (playing)
        {
            source.Pause();
            paused = true;
            playing = false;
            return;
        }
        playing = true;
        if (source.clip == null)
            Skip(1);
        else if (paused)
            source.UnPause();
        else
            source.Play();
        paused = false;
    }
}
