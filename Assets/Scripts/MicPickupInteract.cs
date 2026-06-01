using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MicPickupInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject promptUI;
    [SerializeField] private float pickupDelay = 0.4f;

    [Header("Mic")]
    [SerializeField] private GameObject micObject;
    [SerializeField] private Vector3 micHandPosition = Vector3.zero;
    [SerializeField] private Vector3 micHandRotation = Vector3.zero;

    [Header("Animator")]
    [SerializeField] private string pickupAnimTrigger = "PickupItem";

    private bool _playerNear;
    private bool _pickedUp;

    private static readonly string[] _handBoneNames =
        { "hand_r", "RightHand", "Hand_R", "Hand.R", "mixamorig:RightHand" };

    private void Awake()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            _playerNear = true;
            if (promptUI != null && !_pickedUp) promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            _playerNear = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (_pickedUp || !_playerNear) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(PickupRoutine());
        }
    }

    private IEnumerator PickupRoutine()
    {
        _pickedUp = true;
        if (promptUI != null) promptUI.SetActive(false);

        GameObject player = GameObject.FindWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("[MicPickup] Player not found.", this);
            yield break;
        }

        Animator anim = player.GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrWhiteSpace(pickupAnimTrigger))
        {
            bool triggerExists = false;
            foreach (var p in anim.parameters)
            {
                if (p.name == pickupAnimTrigger) { triggerExists = true; break; }
            }

            if (triggerExists)
                anim.SetTrigger(pickupAnimTrigger);
            else
                Debug.LogWarning($"[MicPickup] Trigger '{pickupAnimTrigger}' not found.", this);
        }

        yield return new WaitForSeconds(pickupDelay);

        if (micObject == null)
        {
            Debug.LogWarning("[MicPickup] Mic object not assigned.", this);
            yield break;
        }

        Transform rightHand = anim != null ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (rightHand == null)
            rightHand = FindBoneByName(player.transform, _handBoneNames);

        if (rightHand == null)
        {
            Debug.LogWarning("[MicPickup] Right hand bone not found.", this);
            yield break;
        }

        AttachMicToHand(rightHand);
    }

    private void AttachMicToHand(Transform hand)
    {
        float micWorldSize = micObject.transform.lossyScale.x;
        float handWorldSize = Mathf.Abs(hand.lossyScale.x) > 1e-6f ? hand.lossyScale.x : 1f;

        micObject.transform.SetParent(hand, false);
        micObject.transform.localPosition = micHandPosition;
        micObject.transform.localRotation = Quaternion.Euler(micHandRotation);
        micObject.transform.localScale = Vector3.one * (micWorldSize / handWorldSize);
    }

    private static Transform FindBoneByName(Transform root, string[] names)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            foreach (string n in names)
                if (string.Equals(t.name, n, System.StringComparison.OrdinalIgnoreCase))
                    return t;
        return null;
    }
}
