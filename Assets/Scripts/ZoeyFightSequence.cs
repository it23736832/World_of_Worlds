using System.Collections;
using UnityEngine;

public class ZoeyFightSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private AStarVillainChase _aStarVillain;
    [SerializeField] private UCSVillainChase _ucsVillain;

    [Header("Fight Audio")]
    [SerializeField] private AudioClip _fightMusic;
    [SerializeField] [Range(0f, 1f)] private float _fightMusicVolume = 0.8f;

    private AudioSource _audioSource;

    [Header("Fight Timing")]
    [SerializeField] private float _fightDuration = 8f;
    [SerializeField] private float _animSwitchInterval = 1.8f;
    [SerializeField] private float _deathAnimDuration = 2.5f;
    [SerializeField] private float _minFightDistance = 2.5f;

    // State names must match exactly what you name the states in Zoey's Animator Controller
    [Header("Animation State Names")]
    [SerializeField] private string[] _fightClips = { "Fist Fight A", "Fist Fight B", "Kicking" };
    [SerializeField] private string _deathClip = "Standing React Death Backward";

    private Animator _villainAnimator;

    public bool IsFightActive { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_aStarVillain == null)
            _aStarVillain = FindObjectOfType<AStarVillainChase>();
        if (_ucsVillain == null)
            _ucsVillain = FindObjectOfType<UCSVillainChase>();

        if (_aStarVillain != null)
            _villainAnimator = _aStarVillain.GetComponentInChildren<Animator>();
        else if (_ucsVillain != null)
            _villainAnimator = _ucsVillain.GetComponentInChildren<Animator>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = false;       // plays once for the clip's duration
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;  // 2D — audible everywhere
    }

    private void Start()
    {
        // The Humanoid avatar retargeting places the animated skeleton 27 m above
        // the root transform (because the rig has scale 4 on the root but the Avatar
        // was built against the inner 0.03 FBX scale).  Wait one frame so the
        // Animator evaluates its first body pose, then shift the root transform so
        // the lowest foot sits exactly on the ground surface.
        StartCoroutine(SnapFeetToGroundNextFrame());
    }

    private IEnumerator SnapFeetToGroundNextFrame()
    {
        yield return null; // let Animator apply the first frame's body pose

        if (_animator == null || !_animator.isHuman) yield break;

        Transform lFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (lFoot == null || rFoot == null) yield break;

        float lowestFootY = Mathf.Min(lFoot.position.y, rFoot.position.y);

        // The spawn Y (transform.position.y) was already grounded by ZoeyHelpUI's
        // terrain raycast before instantiation.  The Humanoid retargeting then lifts
        // the skeleton above that Y by a fixed offset.  Shift the root back down by
        // exactly that offset so the feet land at the original grounded spawn Y.
        // (Avoids a second raycast that could accidentally hit the villain's collider.)
        float footOffset = lowestFootY - transform.position.y;
        transform.position -= new Vector3(0f, footOffset, 0f);
    }

    public void TriggerFight()
    {
        if (IsFightActive || IsDead) return;
        StartCoroutine(FightCoroutine());
    }

    private IEnumerator FightCoroutine()
    {
        IsFightActive = true;

        _aStarVillain?.EnterFight();
        _ucsVillain?.EnterFight();

        if (_fightMusic != null)
        {
            _audioSource.clip = _fightMusic;
            _audioSource.volume = _fightMusicVolume;
            _audioSource.Play();
        }

        Transform villainTransform = GetVillainTransform();

        float elapsed = 0f;
        float nextSwitch = 0f;

        while (elapsed < _fightDuration)
        {
            if (villainTransform != null)
            {
                FaceToward(transform, villainTransform.position);
                FaceToward(villainTransform, transform.position);

                // Keep Zoey from merging into the villain — XZ only, Y never touched.
                Vector3 toZoey = transform.position - villainTransform.position;
                toZoey.y = 0f;
                float flatDist = toZoey.magnitude;
                if (flatDist < _minFightDistance && flatDist > 0.001f)
                {
                    Vector3 p = transform.position;
                    Vector3 offset = toZoey.normalized * _minFightDistance;
                    p.x = villainTransform.position.x + offset.x;
                    p.z = villainTransform.position.z + offset.z;
                    // p.y is not touched — keeps root underground exactly as placed
                    transform.position = p;
                }
            }

            if (elapsed >= nextSwitch)
            {
                PlayRandomClip(_animator, _fightClips);
                PlayVillainFightReaction();
                nextSwitch = elapsed + _animSwitchInterval;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fight over — fade out music then play death animation
        _audioSource.Stop();

        if (_animator != null)
            _animator.CrossFade(_deathClip, 0.15f);

        yield return new WaitForSeconds(_deathAnimDuration);

        // Freeze on the last frame of the death pose
        if (_animator != null)
            _animator.speed = 0f;

        // Lay Zoey flat on the ground: find the lowest point of her mesh in the
        // death pose and shift the root so that point sits on the terrain surface.
        yield return null; // one extra frame so bounds reflect the frozen death pose
        SnapBodyToGround();

        IsDead = true;
        IsFightActive = false;

        // Villain wins — resume chasing RUMI
        _aStarVillain?.ExitFight();
        _ucsVillain?.ExitFight();
    }

    private void PlayRandomClip(Animator anim, string[] clips)
    {
        if (anim == null || clips == null || clips.Length == 0) return;
        anim.CrossFade(clips[Random.Range(0, clips.Length)], 0.15f);
    }

    private void PlayVillainFightReaction()
    {
        if (_villainAnimator == null) return;
        string[] attacks = { "Attack", "Swiping" };
        string pick = attacks[Random.Range(0, attacks.Length)];
        if (HasAnimatorParam(_villainAnimator, pick))
            _villainAnimator.SetTrigger(pick);
    }

    private Transform GetVillainTransform()
    {
        if (_aStarVillain != null) return _aStarVillain.transform;
        if (_ucsVillain != null) return _ucsVillain.transform;
        return null;
    }

    private static void FaceToward(Transform source, Vector3 targetPos)
    {
        Vector3 dir = targetPos - source.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            source.rotation = Quaternion.Slerp(
                source.rotation,
                Quaternion.LookRotation(dir.normalized),
                10f * Time.deltaTime);
    }

    // After the death animation freezes, find the lowest point of Zoey's mesh
    // and shift the root transform so that point lands on the ground surface.
    private void SnapBodyToGround()
    {
        float meshMinY = float.MaxValue;
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.enabled) meshMinY = Mathf.Min(meshMinY, smr.bounds.min.y);

        if (meshMinY >= float.MaxValue) return;

        // Cast from well above the lowest mesh point down to terrain.
        // Offset XZ slightly behind Zoey so the ray misses the villain.
        Vector3 behind = transform.position - transform.forward * 0.5f;
        Vector3 rayOrigin = new Vector3(behind.x, meshMinY + 5f, behind.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 30f))
            transform.position += Vector3.up * (hit.point.y - meshMinY);
    }

    private static bool HasAnimatorParam(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.name == paramName) return true;
        return false;
    }
}
