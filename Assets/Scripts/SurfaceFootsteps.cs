using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SurfaceFootsteps : MonoBehaviour
{
    private enum FootstepTriggerMode
    {
        CharacterControllerDistance,
        AnimationEvents,
        TimedFallback
    }

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cementFootstep;
    [SerializeField] private AudioClip forestFootstep;
    [SerializeField] private AudioClip woodenFootstep;
    [SerializeField] private float cementVolumeMultiplier = 1f;
    [SerializeField] private float forestVolumeMultiplier = 2f;
    [SerializeField] private float woodenVolumeMultiplier = 4f;

    [Header("Surface Tags")]
    [SerializeField] private string cementTag = "Cement";
    [SerializeField] private string forestTag = "Forest";
    [SerializeField] private string woodenTag = "wooden";

    [Header("Timing")]
    [SerializeField] private FootstepTriggerMode triggerMode = FootstepTriggerMode.CharacterControllerDistance;
    [SerializeField] private float minMoveSpeed = 0.15f;
    [SerializeField] private float walkStepDistance = 7f;
    [SerializeField] private float sprintStepDistance = 11f;
    [SerializeField, Range(0f, 1f)] private float firstStepDistanceMultiplier = 0.5f;
    [SerializeField] private float walkStepInterval = 1.6f;
    [SerializeField] private float sprintStepInterval = 0.65f;
    [SerializeField] private float sprintSpeed = 38f;
    [SerializeField] private float sprintStartSpeed = 28f;
    [SerializeField] private float maxClipPlayTime = 0.35f;
    [SerializeField] private float woodenMaxClipPlayTime = 0f;
    [SerializeField] private float minimumTimeBetweenSteps = 0.3f;
    [SerializeField] private float sprintMinimumTimeBetweenSteps = 0.22f;
    [SerializeField] private float groundCheckExtraDistance = 1.5f;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;
    [SerializeField] private bool forceLocalAudioSource = true;
    [SerializeField] private bool allowFallbackClipIfSurfaceMissed = true;
    [SerializeField] private bool debugMissingSurface = true;
    [SerializeField] private bool debugSurfaceHits;

    private CharacterController _controller;
    private float _stepTimer;
    private float _distanceUntilNextStep;
    private float _nextDebugTime;
    private float _nextSurfaceDebugTime;
    private float _lastStepTime = -999f;
    private Vector3 _lastPosition;
    private AudioSource _oneShotAudioSource;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (forceLocalAudioSource || audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.volume = volume;
        audioSource.spatialBlend = spatialBlend;

        _oneShotAudioSource = gameObject.AddComponent<AudioSource>();
        _oneShotAudioSource.playOnAwake = false;
        _oneShotAudioSource.loop = false;
        _oneShotAudioSource.rolloffMode = AudioRolloffMode.Linear;
        _oneShotAudioSource.volume = volume;
        _oneShotAudioSource.spatialBlend = spatialBlend;

        _lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 controllerVelocity = _controller.velocity;
        controllerVelocity.y = 0f;

        Vector3 positionDelta = transform.position - _lastPosition;
        positionDelta.y = 0f;

        float distanceMoved = positionDelta.magnitude;
        float speedFromPosition = distanceMoved / Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = Mathf.Max(controllerVelocity.magnitude, speedFromPosition);
        bool hasGroundBelow = HasGroundBelow();

        if (!hasGroundBelow || speed < minMoveSpeed)
        {
            _stepTimer = 0f;
            _distanceUntilNextStep = 0f;
            _lastPosition = transform.position;
            return;
        }

        if (triggerMode == FootstepTriggerMode.AnimationEvents)
        {
            _lastPosition = transform.position;
            return;
        }

        if (!TryGetSurfaceClip(out AudioClip clip, out float volumeMultiplier) &&
            !TryGetFallbackClip(out clip, out volumeMultiplier))
        {
            DebugMissingSurface();
            _lastPosition = transform.position;
            return;
        }

        if (triggerMode == FootstepTriggerMode.CharacterControllerDistance)
        {
            UpdateDistanceBasedFootsteps(speed, distanceMoved, clip, volumeMultiplier);
            _lastPosition = transform.position;
            return;
        }

        _stepTimer -= Time.deltaTime;
        if (_stepTimer > 0f)
        {
            _lastPosition = transform.position;
            return;
        }

        TryPlaySingleFootstep(clip, volumeMultiplier, minimumTimeBetweenSteps);

        float speed01 = sprintSpeed > 0f ? Mathf.Clamp01(speed / sprintSpeed) : 0f;
        _stepTimer = Mathf.Lerp(walkStepInterval, sprintStepInterval, speed01);
        _lastPosition = transform.position;
    }

    private void UpdateDistanceBasedFootsteps(float speed, float distanceMoved, AudioClip clip, float volumeMultiplier)
    {
        bool sprinting = speed >= sprintStartSpeed;
        float stepDistance = sprinting ? sprintStepDistance : walkStepDistance;
        float minTimeBetweenSteps = sprinting ? sprintMinimumTimeBetweenSteps : minimumTimeBetweenSteps;

        if (_distanceUntilNextStep <= 0f)
        {
            _distanceUntilNextStep = stepDistance * firstStepDistanceMultiplier;
        }

        _distanceUntilNextStep -= distanceMoved;
        if (_distanceUntilNextStep > 0f)
        {
            return;
        }

        TryPlaySingleFootstep(clip, volumeMultiplier, minTimeBetweenSteps);
        _distanceUntilNextStep += stepDistance;
    }

    public void PlayFootstep()
    {
        Vector3 horizontalVelocity = _controller.velocity;
        horizontalVelocity.y = 0f;

        if (!HasGroundBelow() || horizontalVelocity.magnitude < minMoveSpeed)
        {
            return;
        }

        if (!TryGetSurfaceClip(out AudioClip clip, out float volumeMultiplier) &&
            !TryGetFallbackClip(out clip, out volumeMultiplier))
        {
            DebugMissingSurface();
            return;
        }

        TryPlaySingleFootstep(clip, volumeMultiplier, minimumTimeBetweenSteps);
    }

    public void Footstep()
    {
        PlayFootstep();
    }

    private void TryPlaySingleFootstep(AudioClip clip, float volumeMultiplier, float minTimeBetweenSteps)
    {
        if (Time.time - _lastStepTime < minTimeBetweenSteps)
        {
            return;
        }

        _lastStepTime = Time.time;
        PlaySingleFootstep(clip, volumeMultiplier);
    }

    private void PlaySingleFootstep(AudioClip clip, float volumeMultiplier)
    {
        if (clip == woodenFootstep)
        {
            PlayWoodenFootstep(clip);
            return;
        }

        CancelInvoke(nameof(StopFootstepSound));
        audioSource.Stop();
        audioSource.pitch = Random.Range(0.96f, 1.04f);
        float playTime = maxClipPlayTime > 0f ? Mathf.Min(maxClipPlayTime, clip.length) : clip.length;
        audioSource.clip = clip;
        audioSource.time = 0f;
        audioSource.volume = Mathf.Clamp01(volume * volumeMultiplier);
        audioSource.Play();

        if (maxClipPlayTime > 0f)
        {
            Invoke(nameof(StopFootstepSound), playTime);
        }
    }

    private void PlayWoodenFootstep(AudioClip clip)
    {
        AudioSource source = _oneShotAudioSource != null ? _oneShotAudioSource : audioSource;
        source.pitch = Random.Range(0.96f, 1.04f);
        source.volume = volume;
        source.PlayOneShot(clip, volume * woodenVolumeMultiplier);
    }

    private void StopFootstepSound()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private bool TryGetSurfaceClip(out AudioClip clip, out float volumeMultiplier)
    {
        clip = null;
        volumeMultiplier = 1f;

        Vector3 origin = _controller.bounds.center;
        float distance = _controller.bounds.extents.y + groundCheckExtraDistance;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            return false;
        }

        Transform surface = hit.collider.transform;
        while (surface != null && clip == null)
        {
            if (surface.CompareTag(cementTag))
            {
                clip = cementFootstep;
                volumeMultiplier = cementVolumeMultiplier;
            }
            else if (surface.CompareTag(forestTag))
            {
                clip = forestFootstep;
                volumeMultiplier = forestVolumeMultiplier;
            }
            else if (surface.CompareTag(woodenTag))
            {
                clip = woodenFootstep;
                volumeMultiplier = woodenVolumeMultiplier;
            }

            surface = surface.parent;
        }

        if (clip == null)
        {
            TryMatchSurfaceByName(hit.collider, out clip, out volumeMultiplier);
        }

        DebugSurfaceHit(hit.collider, clip);

        return clip != null;
    }

    private bool TryMatchSurfaceByName(Collider collider, out AudioClip clip, out float volumeMultiplier)
    {
        clip = null;
        volumeMultiplier = 1f;

        string surfaceName = collider.name.ToLowerInvariant();
        Renderer renderer = collider.GetComponentInParent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            surfaceName += " " + renderer.sharedMaterial.name.ToLowerInvariant();
        }

        if (surfaceName.Contains("wood") || surfaceName.Contains("cabin") || surfaceName.Contains("plank") || surfaceName.Contains("floor"))
        {
            clip = woodenFootstep;
            volumeMultiplier = woodenVolumeMultiplier;
        }
        else if (surfaceName.Contains("cement") || surfaceName.Contains("concrete") || surfaceName.Contains("tile") || surfaceName.Contains("stone") || surfaceName.Contains("asylum") || surfaceName.Contains("brick"))
        {
            clip = cementFootstep;
            volumeMultiplier = cementVolumeMultiplier;
        }
        else if (surfaceName.Contains("forest") || surfaceName.Contains("ground") || surfaceName.Contains("terrain") || surfaceName.Contains("grass") || surfaceName.Contains("dirt"))
        {
            clip = forestFootstep;
            volumeMultiplier = forestVolumeMultiplier;
        }

        return clip != null;
    }

    private bool HasGroundBelow()
    {
        Vector3 origin = _controller.bounds.center;
        float distance = _controller.bounds.extents.y + groundCheckExtraDistance;
        return Physics.Raycast(origin, Vector3.down, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
    }

    private bool TryGetFallbackClip(out AudioClip clip, out float volumeMultiplier)
    {
        clip = null;
        volumeMultiplier = 1f;

        if (!allowFallbackClipIfSurfaceMissed)
        {
            return false;
        }

        if (forestFootstep != null)
        {
            clip = forestFootstep;
            volumeMultiplier = forestVolumeMultiplier;
        }
        else if (cementFootstep != null)
        {
            clip = cementFootstep;
            volumeMultiplier = cementVolumeMultiplier;
        }
        else if (woodenFootstep != null)
        {
            clip = woodenFootstep;
            volumeMultiplier = woodenVolumeMultiplier;
        }

        return clip != null;
    }

    private void DebugMissingSurface()
    {
        if (!debugMissingSurface || Time.time < _nextDebugTime)
        {
            return;
        }

        _nextDebugTime = Time.time + 0.75f;

        Vector3 origin = _controller.bounds.center;
        float distance = _controller.bounds.extents.y + groundCheckExtraDistance;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            Debug.LogWarning($"SurfaceFootsteps hit '{hit.collider.name}' with tag '{hit.collider.tag}', but no clip matched or the clip field is empty.", this);
        }
        else
        {
            Debug.LogWarning("SurfaceFootsteps did not hit any floor collider below Rumi.", this);
        }
    }

    private void DebugSurfaceHit(Collider collider, AudioClip clip)
    {
        if (!debugSurfaceHits || Time.time < _nextSurfaceDebugTime)
        {
            return;
        }

        _nextSurfaceDebugTime = Time.time + 0.75f;

        string clipName = clip != null ? clip.name : "<none>";
        Debug.LogWarning($"SurfaceFootsteps surface hit '{collider.name}' tag '{collider.tag}' clip '{clipName}'.", this);
    }
}
