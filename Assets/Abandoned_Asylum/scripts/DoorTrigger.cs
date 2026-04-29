using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [Header("Door Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Door Behavior")]
    [SerializeField] private bool autoCloseWhenPlayerLeaves = false;
    [SerializeField] private string playerTag = "Player";

    private bool isOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isOpen)
        {
            OpenDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && autoCloseWhenPlayerLeaves && isOpen)
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(openTriggerName);
            isOpen = true;
        }
        else
        {
            Debug.LogWarning($"DoorTrigger on '{name}' has no Animator assigned.");
        }
    }

    private void CloseDoor()
    {
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(closeTriggerName);
            isOpen = false;
        }
    }
}
