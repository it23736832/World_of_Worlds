using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordAttack : MonoBehaviour
{
    [SerializeField] private string _slashParam = "Slash";
    [SerializeField] private string _slashState = "Slash";
    [SerializeField] private string _attackLayer = "Attack";
    [SerializeField] private float _crossFadeDuration = 0.05f;
    [SerializeField] private float _maxSlashDuration = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip _slashSound;
    [SerializeField] private AudioSource _audioSource;

    [Header("Seal Barricade")]
    [SerializeField] private GameObject _sealPrefab;
    [SerializeField] private int        _maxSeals       = 5;
    [SerializeField] private float      _spawnDistance  = 1.5f;
    [SerializeField] private LayerMask  _floorMask      = ~0;

    private Animator _animator;
    private int      _attackLayerIndex = -1;
    private bool     _slashing;
    private int      _sealsRemaining;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        ResolveAnimator();
        ResolveAttackLayer();
        _sealsRemaining = _maxSeals;
    }

    private void OnEnable()
    {
        ResolveAnimator();
        ResolveAttackLayer();
        _slashing = false;
    }

    private void ResolveAnimator()
    {
        if (_animator != null)
            return;

        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    private void ResolveAttackLayer()
    {
        if (_animator != null)
        {
            _attackLayerIndex = _animator.GetLayerIndex(_attackLayer);
            Debug.Log($"[SwordAttack] Animator found: {_animator.name} | Attack layer index: {_attackLayerIndex}");
        }
        else
        {
            Debug.LogError("[SwordAttack] No Animator found!");
        }
    }

    private void Update()
    {
        if (_animator == null || _slashing) return;
        if (ReadAttackInput())
        {
            Debug.Log("[SwordAttack] Attack input detected — starting slash");
            StartCoroutine(SlashRoutine());
        }
    }

    private IEnumerator SlashRoutine()
    {
        _slashing = true;

        if (_slashSound != null && _audioSource != null)
            _audioSource.PlayOneShot(_slashSound);

        if (_attackLayerIndex >= 0)
        {
            _animator.SetLayerWeight(_attackLayerIndex, 1f);
            _animator.CrossFadeInFixedTime(_slashState, _crossFadeDuration, _attackLayerIndex, 0f);
        }
        else
        {
            Debug.LogWarning($"[SwordAttack] Attack layer '{_attackLayer}' not found — trigger only");
        }

        _animator.SetTrigger(_slashParam);

        // Spawn seal immediately so Jinu has to reroute right away
        SpawnSeal();

        // Notify villains for RunToStop reaction
        FindObjectOfType<AStarVillainChase>()?.OnPlayerSwordSwing();
        FindObjectOfType<UCSVillainChase>()?.OnPlayerSwordSwing();

        if (_attackLayerIndex >= 0)
        {
            yield return null;

            float elapsed = 0f;
            bool enteredSlash = false;
            while (elapsed < _maxSlashDuration
                   && (!enteredSlash
                       || _animator.GetCurrentAnimatorStateInfo(_attackLayerIndex).IsName(_slashState)
                       || _animator.IsInTransition(_attackLayerIndex)))
            {
                if (_animator.GetCurrentAnimatorStateInfo(_attackLayerIndex).IsName(_slashState))
                    enteredSlash = true;

                elapsed += Time.deltaTime;
                yield return null;
            }

            _animator.SetLayerWeight(_attackLayerIndex, 0f);
        }

        _slashing = false;
    }

    private void SpawnSeal()
    {
        if (_sealPrefab == null)
        {
            Debug.LogWarning("[SwordAttack] Seal Prefab not assigned — assign it in the Inspector.", this);
            return;
        }
        if (_sealsRemaining <= 0)
        {
            Debug.Log("[SwordAttack] No seals remaining.");
            return;
        }

        Vector3 spawnOrigin = transform.position + transform.forward * _spawnDistance + Vector3.up * 1f;
        if (Physics.Raycast(spawnOrigin, Vector3.down, out RaycastHit hit, 3f, _floorMask))
        {
            Instantiate(_sealPrefab, hit.point, Quaternion.identity);
            _sealsRemaining--;
            Debug.Log($"[SwordAttack] Seal spawned at {hit.point} ({_sealsRemaining} remaining).", this);
        }
        else
        {
            Debug.LogWarning($"[SwordAttack] Raycast missed — no floor found in front of Rumi. Check Floor Mask and spawn distance.", this);
        }
    }

    private static bool ReadAttackInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            return true;
        return false;
    }
}
