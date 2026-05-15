using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] private AudioSource footstepsAudio;
    [SerializeField] private AudioClip cementClip;
    [SerializeField] private AudioClip forestClip;
    [SerializeField] private AudioClip woodClip;
    [SerializeField] private string cementTag = "Cement";
    [SerializeField] private string forestTag = "Forest";
    [SerializeField] private string woodTag = "wooden";
    [SerializeField] private string footstepsAudioName = "footstepsAudio";
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody rigidbodySource;
    [SerializeField] private float minMoveSpeed = 0.1f;
    [SerializeField] private bool requireGrounded = true;
    [SerializeField] private bool requireSurfaceTag = true;
    [SerializeField] private bool debugLog;
    [SerializeField] private float debugInterval = 0.5f;
    [SerializeField] private float minStepInterval = 0.25f;
    [SerializeField] private float maxStepInterval = 0.5f;
    [SerializeField] private float speedForMinInterval = 5.5f;

    private float timer;
    private string lastHitTag = "<none>";
    private string lastHitName = "<none>";
    private float nextDebugTime;
    private Vector3 lastPosition;

    private void Awake()
    {
        if (footstepsAudio == null && !string.IsNullOrWhiteSpace(footstepsAudioName))
        {
            GameObject audioObject = GameObject.Find(footstepsAudioName);
            if (audioObject != null)
            {
                footstepsAudio = audioObject.GetComponent<AudioSource>();
            }
        }

        if (characterController == null)
        {
            characterController = GetComponentInParent<CharacterController>();
        }

        if (rigidbodySource == null)
        {
            rigidbodySource = GetComponentInParent<Rigidbody>();
        }

        lastPosition = transform.position;

        if (debugLog)
        {
            Debug.LogWarning($"Footsteps Awake on {gameObject.name}: audio={(footstepsAudio != null)} clip={(cementClip != null)} controller={(characterController != null)}");
        }
    }

    private void Update()
    {
        float planarSpeed = GetPlanarSpeed();
        bool isMoving = planarSpeed > minMoveSpeed;
        bool grounded = IsGrounded();
        bool hasSurfaceClip = TryGetSurfaceClip(out AudioClip surfaceClip);
        if (!hasSurfaceClip && !requireSurfaceTag)
        {
            surfaceClip = GetFallbackClip();
        }

        bool canPlay = (!requireGrounded || grounded) &&
                       (!requireSurfaceTag || hasSurfaceClip) &&
                       footstepsAudio != null &&
                       surfaceClip != null;

        if (debugLog && Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + Mathf.Max(0.1f, debugInterval);
            bool audioEnabled = footstepsAudio != null && footstepsAudio.enabled;
            float volume = footstepsAudio != null ? footstepsAudio.volume : 0f;
            float spatialBlend = footstepsAudio != null ? footstepsAudio.spatialBlend : 0f;
            float maxDistance = footstepsAudio != null ? footstepsAudio.maxDistance : 0f;
            float interval = GetStepInterval(planarSpeed);
            string clipName = surfaceClip != null ? surfaceClip.name : "<none>";
            Debug.LogWarning($"Footsteps: speed={planarSpeed:F2} interval={interval:F2} moving={isMoving} grounded={grounded} hasSurfaceClip={hasSurfaceClip} lastHit={lastHitName}/{lastHitTag} clip={clipName} audio={audioEnabled} vol={volume:F2} 3D={spatialBlend:F2} maxDist={maxDistance:F1}");
        }

        if (isMoving)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                if (debugLog)
                {
                    string clipName = surfaceClip != null ? surfaceClip.name : "<none>";
                    Debug.LogWarning($"Footsteps: moving={isMoving} grounded={grounded} hasSurfaceClip={hasSurfaceClip} lastHit={lastHitName}/{lastHitTag} audio={(footstepsAudio != null)} clip={clipName}");
                }

                if (canPlay)
                {
                    footstepsAudio.PlayOneShot(surfaceClip);
                }

                timer = GetStepInterval(planarSpeed);
            }
        }
        else
        {
            timer = 0f;
        }

        lastPosition = transform.position;
    }

    private string GetSurfaceTag()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float distance = 1.5f;

        if (characterController != null)
        {
            Bounds bounds = characterController.bounds;
            origin = bounds.center;
            distance = bounds.extents.y + 0.5f;
        }

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hitInfo, distance))
        {
            lastHitTag = hitInfo.collider.tag;
            lastHitName = hitInfo.collider.name;
            return lastHitTag;
        }

        lastHitTag = "<none>";
        lastHitName = "<none>";

        return string.Empty;
    }

    private bool TryGetSurfaceClip(out AudioClip surfaceClip)
    {
        surfaceClip = null;
        string surfaceTag = GetSurfaceTag();

        if (!string.IsNullOrEmpty(surfaceTag))
        {
            if (surfaceTag == cementTag)
            {
                surfaceClip = cementClip;
            }
            else if (surfaceTag == forestTag)
            {
                surfaceClip = forestClip;
            }
            else if (surfaceTag == woodTag)
            {
                surfaceClip = woodClip;
            }
        }

        return surfaceClip != null;
    }

    private AudioClip GetFallbackClip()
    {
        if (cementClip != null)
        {
            return cementClip;
        }

        if (forestClip != null)
        {
            return forestClip;
        }

        if (woodClip != null)
        {
            return woodClip;
        }

        return null;
    }

    private bool IsGrounded()
    {
        if (characterController != null)
        {
            return characterController.isGrounded;
        }

        if (rigidbodySource != null)
        {
            return Mathf.Abs(rigidbodySource.linearVelocity.y) < 0.05f;
        }

        return true;
    }

    private float GetPlanarSpeed()
    {
        float speed = 0f;

        if (characterController != null)
        {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            speed = Mathf.Max(speed, velocity.magnitude);
        }

        if (rigidbodySource != null)
        {
            Vector3 velocity = rigidbodySource.linearVelocity;
            velocity.y = 0f;
            speed = Mathf.Max(speed, velocity.magnitude);
        }

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        speed = Mathf.Max(speed, delta.magnitude / dt);

        return speed;
    }

    private float GetStepInterval(float planarSpeed)
    {
        float clampedSpeed = Mathf.Max(0f, planarSpeed);
        float t = speedForMinInterval > 0f ? Mathf.Clamp01(clampedSpeed / speedForMinInterval) : 0f;
        return Mathf.Lerp(maxStepInterval, minStepInterval, t);
    }
}
