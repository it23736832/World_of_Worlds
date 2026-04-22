using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PortalTeleporter : MonoBehaviour
{
    [Header("Portal Link")]
    [SerializeField] private PortalTeleporter linkedPortal;
    [SerializeField] private Transform exitPoint;

    [Header("Traveller Filter")]
    [SerializeField] private string travellerTag = "Player";

    [Header("Teleport Settings")]
    [SerializeField] private float exitForwardOffset = 1.2f;
    [SerializeField] private float reentryBlockSeconds = 0.25f;
    [SerializeField] private bool matchExitRotation = true;
    [SerializeField] private bool alignYawOnly = true;

    private readonly HashSet<Transform> blockedTravellers = new HashSet<Transform>();

    private void OnTriggerEnter(Collider other)
    {
        Transform traveller = FindTravellerRoot(other.transform);

        if (traveller == null || blockedTravellers.Contains(traveller))
        {
            return;
        }

        if (linkedPortal == null)
        {
            Debug.LogWarning($"Portal '{name}' has no linked portal assigned.");
            return;
        }

        TeleportTraveller(traveller);
    }

    private void TeleportTraveller(Transform traveller)
    {
        CharacterController characterController = traveller.GetComponent<CharacterController>();
        Vector3 targetPosition = linkedPortal.GetSpawnPosition();
        Quaternion targetRotation = matchExitRotation ? linkedPortal.GetSpawnRotation() : traveller.rotation;

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        traveller.SetPositionAndRotation(targetPosition, targetRotation);
        Physics.SyncTransforms();

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        BlockTraveller(traveller);
        linkedPortal.BlockTraveller(traveller);
    }

    private Vector3 GetSpawnPosition()
    {
        Transform anchor = exitPoint != null ? exitPoint : transform;
        return anchor.position + anchor.forward * exitForwardOffset;
    }

    private Quaternion GetSpawnRotation()
    {
        Transform anchor = exitPoint != null ? exitPoint : transform;

        if (!alignYawOnly)
        {
            return anchor.rotation;
        }

        return Quaternion.Euler(0f, anchor.eulerAngles.y, 0f);
    }

    private Transform FindTravellerRoot(Transform hitTransform)
    {
        CharacterController characterController = hitTransform.GetComponentInParent<CharacterController>();
        Transform traveller = characterController != null ? characterController.transform : hitTransform.root;

        if (!string.IsNullOrWhiteSpace(travellerTag) && !traveller.CompareTag(travellerTag))
        {
            return null;
        }

        return traveller;
    }

    private void BlockTraveller(Transform traveller)
    {
        StartCoroutine(UnblockAfterDelay(traveller));
    }

    private IEnumerator UnblockAfterDelay(Transform traveller)
    {
        blockedTravellers.Add(traveller);
        yield return new WaitForSeconds(reentryBlockSeconds);
        blockedTravellers.Remove(traveller);
    }
}