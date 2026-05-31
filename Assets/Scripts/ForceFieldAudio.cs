using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ForceFieldAudio : MonoBehaviour
{
    [SerializeField] private AudioClip _clip;
    [SerializeField] private float     _volume     = 0.7f;
    [SerializeField] private float     _fadeSpeed  = 2f;
    [SerializeField] private float     _hearRadius = 150f;
    [SerializeField] private string    _playerTag  = "Player";

    private AudioSource _audio;
    private Transform   _player;

    private void Awake()
    {
        _audio             = GetComponent<AudioSource>();
        _audio.clip        = _clip;
        _audio.loop        = true;
        _audio.playOnAwake = false;
        _audio.volume      = 0f;
    }

    private void Start()
    {
        GameObject p = GameObject.FindWithTag(_playerTag);
        if (p != null) _player = p.transform;
    }

    private void Update()
    {
        bool near = _player != null &&
                    Vector3.Distance(_player.position, transform.position) <= _hearRadius;

        float target = near ? _volume : 0f;
        _audio.volume = Mathf.MoveTowards(_audio.volume, target, _fadeSpeed * Time.deltaTime);

        if (near && !_audio.isPlaying)  _audio.Play();
        if (!near && _audio.volume <= 0f && _audio.isPlaying) _audio.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, _hearRadius);
    }
}
