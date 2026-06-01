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

    [Header("Barricade")]
    [SerializeField] private GameObject _barricadePrefab;
    [SerializeField] private int        _maxBarricades     = 5;
    [SerializeField] private float      _spawnScale        = 1.5f;
    [SerializeField] private float      _spawnDistance     = 3f;
    [SerializeField] private float      _barricadeDuration = 30f;
    [SerializeField] private LayerMask  _floorMask         = ~0;

    private Animator _animator;
    private int      _attackLayerIndex = -1;
    private bool     _slashing;
    private int      _barricadesRemaining;

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
        _barricadesRemaining = _maxBarricades;
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
        if (StartStoryUI.IsActive) return;
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

        SpawnBarricade();

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

    private void SpawnBarricade()
    {
        if (_barricadePrefab == null)
        {
            Debug.LogWarning("[SwordAttack] Barricade Prefab not assigned — assign it in the Inspector.", this);
            return;
        }
        if (_barricadesRemaining <= 0)
        {
            Debug.Log("[SwordAttack] No barricades remaining.");
            return;
        }

        // Spawn in front of RUMI on the ground surface
        Vector3 forward2D = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 spawnPos  = transform.position + forward2D * _spawnDistance;

        if (Physics.Raycast(spawnPos + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f, _floorMask))
            spawnPos.y = hit.point.y;
        else
            spawnPos.y = transform.position.y;

        spawnPos.y += _spawnScale * 0.1f;    // slight lift so it doesn't clip the floor

        Quaternion spawnRot = Quaternion.LookRotation(forward2D);
        GameObject spawned  = Instantiate(_barricadePrefab, spawnPos, spawnRot);
        spawned.transform.localScale = Vector3.one * _spawnScale;

        if (_barricadeDuration > 0f)
            Destroy(spawned, _barricadeDuration);

        _barricadesRemaining--;
        Debug.Log($"[SwordAttack] Barricade spawned at {spawnPos} scale={_spawnScale} ({_barricadesRemaining} remaining).", this);
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
