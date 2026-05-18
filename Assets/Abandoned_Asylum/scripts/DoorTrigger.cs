using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [Header("Door Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName  = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Player Animation")]
    [SerializeField] private string playerOpenTrigger = "OpenDoor";

    [Header("Interaction")]
    [SerializeField] private string playerTag  = "Player";
    [SerializeField] private string villainTag = "Villain";
    [SerializeField] private GameObject interactPrompt;

    private int _charactersInRange = 0;
    private bool _isOpen           = false;
    private Animator _playerAnimator;

    private void Awake()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponentInParent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) && !other.CompareTag(villainTag)) return;

        _charactersInRange++;

        if (other.CompareTag(playerTag))
        {
            _playerAnimator = other.GetComponentInChildren<Animator>();
            if (interactPrompt != null)
                interactPrompt.SetActive(true);
        }

        if (!_isOpen)
            OpenDoor();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag) && !other.CompareTag(villainTag)) return;

        _charactersInRange = Mathf.Max(0, _charactersInRange - 1);

        if (other.CompareTag(playerTag))
        {
            _playerAnimator = null;
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }

        if (_charactersInRange == 0 && _isOpen)
            CloseDoor();
    }

    private void OpenDoor()
    {
        if (doorAnimator == null)
        {
            Debug.LogWarning($"DoorTrigger on '{name}' has no Animator assigned.");
            return;
        }

        doorAnimator.SetTrigger(openTriggerName);

        if (_playerAnimator != null && !string.IsNullOrEmpty(playerOpenTrigger))
            _playerAnimator.SetTrigger(playerOpenTrigger);

        _isOpen = true;
    }

    private void CloseDoor()
    {
        if (doorAnimator == null) return;

        doorAnimator.SetTrigger(closeTriggerName);
        _isOpen = false;
    }
}
