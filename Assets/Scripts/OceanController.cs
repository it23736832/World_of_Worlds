using UnityEngine;

// Handles proximity wave sounds for the ocean boundary.
// Attach this alongside NVWaterShaders on the Ocean GameObject.
public class OceanController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] public AudioClip waveSound;
    [SerializeField] private float audioFadeStartDist = 250f;
    [SerializeField] private float audioFullVolumeDist = 80f;
    [SerializeField] private float maxVolume = 0.85f;

    private AudioSource _audio;
    private Transform   _player;

    private void Start()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();

        _audio.loop         = true;
        _audio.spatialBlend = 1f;
        _audio.volume       = 0f;
        _audio.minDistance  = audioFullVolumeDist;
        _audio.maxDistance  = audioFadeStartDist;
        _audio.rolloffMode  = AudioRolloffMode.Linear;
        if (waveSound != null) { _audio.clip = waveSound; _audio.Play(); }

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void Update()
    {
        if (_player == null || waveSound == null) return;
        float dist = Vector3.Distance(transform.position, _player.position);
        float t    = 1f - Mathf.Clamp01((dist - audioFullVolumeDist) /
                          (audioFadeStartDist - audioFullVolumeDist));
        _audio.volume = t * maxVolume;
    }
}
