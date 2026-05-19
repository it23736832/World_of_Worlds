using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DoorAudio : MonoBehaviour
{
    [SerializeField] private AudioClip _openClip;
    [SerializeField] private AudioClip _closeClip;
    [SerializeField] private float     _volume = 0.8f;

    private AudioSource _audio;
    private DoorTrigger _door;
    private bool        _wasOpen;

    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;
        _door = GetComponent<DoorTrigger>();
    }

    void Update()
    {
        if (_door == null) return;

        bool isOpen = IsOpen();

        if (isOpen && !_wasOpen)
            Play(_openClip);
        else if (!isOpen && _wasOpen)
            Play(_closeClip);

        _wasOpen = isOpen;
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
            _audio.PlayOneShot(clip, _volume);
    }

    // Reads DoorTrigger._isOpen via reflection so we don't modify the original script.
    private bool IsOpen()
    {
        var field = typeof(DoorTrigger).GetField("_isOpen",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null && (bool)field.GetValue(_door);
    }
}
