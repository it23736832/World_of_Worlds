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

        if (_attackLayerIndex >= 0)
        {
            _animator.SetLayerWeight(_attackLayerIndex, 1f);
            Debug.Log($"[SwordAttack] Layer weight set to 1 on layer {_attackLayerIndex}");
            _animator.CrossFadeInFixedTime(_slashState, _crossFadeDuration, _attackLayerIndex, 0f);
        }
        else
        {
            Debug.LogWarning($"[SwordAttack] Attack layer '{_attackLayer}' not found — trigger only");
        }

        _animator.SetTrigger(_slashParam);
        Debug.Log($"[SwordAttack] Trigger '{_slashParam}' set");

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
        SpawnSeal();
    }

    private void SpawnSeal()
    {
        if (_sealPrefab == null || _sealsRemaining <= 0) return;

        // Raycast down from a point in front of Rumi to find the floor
        Vector3 spawnOrigin = transform.position + transform.forward * _spawnDistance + Vector3.up * 1f;
        if (Physics.Raycast(spawnOrigin, Vector3.down, out RaycastHit hit, 3f, _floorMask))
        {
            Instantiate(_sealPrefab, hit.point, Quaternion.identity);
            _sealsRemaining--;
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
