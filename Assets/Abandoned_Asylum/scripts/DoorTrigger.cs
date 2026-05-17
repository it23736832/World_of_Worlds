using UnityEngine;
using UnityEngine.InputSystem;

public class DoorTrigger : MonoBehaviour
{
    [Header("Door Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName  = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Player Animation")]
    [SerializeField] private string playerOpenTrigger = "OpenDoor";  // trigger on RUMI's Animator

    [Header("Interaction")]
    [SerializeField] private string playerTag     = "Player";
    [SerializeField] private GameObject interactPrompt;

    private bool _playerInRange  = false;
    private bool _isOpen         = false;
    private Animator _playerAnimator;

    private void Awake()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponentInParent<Animator>();
    }

    private void Update()
    {
        if (!_playerInRange) return;
        if (!IsInteractPressed()) return;

        if (_isOpen)
            CloseDoor();
        else
            OpenDoor();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInRange   = true;
        _playerAnimator  = other.GetComponentInChildren<Animator>();

        if (interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInRange  = false;
        _playerAnimator = null;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void OpenDoor()
    {
        if (doorAnimator == null)
        {
            Debug.LogWarning($"DoorTrigger on '{name}' has no Animator assigned.");
            return;
        }

        doorAnimator.SetTrigger(openTriggerName);

        // Play RUMI's door-open animation if the trigger name is set.
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

    private static bool IsInteractPressed()
    {
        if (Keyboard.current != null)
            return Keyboard.current.eKey.wasPressedThisFrame;

        return Input.GetKeyDown(KeyCode.E);
    }
}
