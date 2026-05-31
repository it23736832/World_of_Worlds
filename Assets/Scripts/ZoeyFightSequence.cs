using System.Collections;
using UnityEngine;

public class ZoeyFightSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private AStarVillainChase _aStarVillain;
    [SerializeField] private UCSVillainChase _ucsVillain;

    [Header("Fight Timing")]
    [SerializeField] private float _fightDuration = 8f;
    [SerializeField] private float _animSwitchInterval = 1.8f;
    [SerializeField] private float _deathAnimDuration = 2.5f;

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

        Transform villainTransform = GetVillainTransform();

        float elapsed = 0f;
        float nextSwitch = 0f;

        while (elapsed < _fightDuration)
        {
            // Keep both facing each other throughout the fight
            if (villainTransform != null)
            {
                FaceToward(transform, villainTransform.position);
                FaceToward(villainTransform, transform.position);
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

        // Zoey loses — play death animation
        if (_animator != null)
            _animator.CrossFade(_deathClip, 0.15f);

        yield return new WaitForSeconds(_deathAnimDuration);

        // Freeze on the last frame of the death pose — don't disappear
        if (_animator != null)
            _animator.speed = 0f;

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

    private static bool HasAnimatorParam(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.name == paramName) return true;
        return false;
    }
}
