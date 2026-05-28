using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

public class CabinForceFieldTrigger : MonoBehaviour
{
    [Header("Forcefield")]
    [SerializeField] private GameObject forceField;
    [SerializeField] private bool hideForceFieldOnStart = true;
    [SerializeField] private bool ignorePlayerCollision = true;
    [SerializeField] private bool makeForceFieldDoubleSided = true;
    [SerializeField] private bool enableRenderersWhenActive = true;
    [SerializeField] private bool showDebugMessages;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";

    private readonly HashSet<Transform> _playersInside = new HashSet<Transform>();
    private Renderer[] _forceFieldRenderers;

    private void Awake()
    {
        CacheForceFieldRenderers();
        CheckSetup();

        if (makeForceFieldDoubleSided)
        {
            MakeForceFieldDoubleSided();
        }

        if (hideForceFieldOnStart)
        {
            SetForceFieldActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        AddPlayerInside(other);
    }

    private void OnTriggerStay(Collider other)
    {
        AddPlayerInside(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Transform playerRoot = GetTaggedRoot(other, playerTag);
        if (playerRoot == null)
        {
            return;
        }

        _playersInside.Remove(playerRoot);
        IgnoreForceFieldCollision(playerRoot, false);

        if (_playersInside.Count == 0)
        {
            SetForceFieldActive(false);
            // Resume villain AI when player exits
            NotifyVillainToResume();
        }
    }

    private void NotifyVillainToResume()
    {
        AStarVillainChase villain = FindObjectOfType<AStarVillainChase>();
        if (villain != null)
        {
            villain.ResumeChase();
            if (showDebugMessages)
            {
                Debug.Log("[CabinForceFieldTrigger] Villain notified to resume chase.", this);
            }
        }
    }

    private void OnDisable()
    {
        _playersInside.Clear();
        SetForceFieldActive(false);
    }

    private void AddPlayerInside(Collider other)
    {
        Transform playerRoot = GetTaggedRoot(other, playerTag);
        if (playerRoot == null)
        {
            return;
        }

        bool wasAdded = _playersInside.Add(playerRoot);
        IgnoreForceFieldCollision(playerRoot, true);
        SetForceFieldActive(true);

        // Notify villain to stop and idle
        if (wasAdded)
        {
            NotifyVillainToStop();
            if (showDebugMessages)
            {
                Debug.Log("Cabin forcefield activated by " + playerRoot.name, this);
            }
        }
    }

    private void NotifyVillainToStop()
    {
        AStarVillainChase villain = FindObjectOfType<AStarVillainChase>();
        if (villain != null)
        {
            villain.SetIdle();
            if (showDebugMessages)
            {
                Debug.Log("[CabinForceFieldTrigger] Villain notified to stop and idle.", this);
            }
        }
    }

    private void SetForceFieldActive(bool active)
    {
        if (forceField != null && forceField.activeSelf != active)
        {
            forceField.SetActive(active);
        }

        // Always enable renderers so forcefield is visible from inside
        // Don't disable them even when not active
        if (_forceFieldRenderers != null)
        {
            foreach (Renderer forceFieldRenderer in _forceFieldRenderers)
            {
                if (forceFieldRenderer != null)
                {
                    forceFieldRenderer.enabled = true;  // Always enabled for visibility
                }
            }
        }
    }

    private void CacheForceFieldRenderers()
    {
        if (forceField != null)
        {
            _forceFieldRenderers = forceField.GetComponentsInChildren<Renderer>(true);
        }
    }

    private void CheckSetup()
    {
        if (forceField == null)
        {
            Debug.LogWarning("CabinForceFieldTrigger has no Force Field assigned.", this);
        }

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogWarning("CabinForceFieldTrigger needs a Collider on the same GameObject.", this);
        }
        else if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("CabinForceFieldTrigger collider must have Is Trigger enabled.", this);
        }
    }

    private void IgnoreForceFieldCollision(Transform playerRoot, bool ignore)
    {
        if (!ignorePlayerCollision || forceField == null || playerRoot == null)
        {
            return;
        }

        Collider[] forceFieldColliders = forceField.GetComponentsInChildren<Collider>(true);
        Collider[] playerColliders = playerRoot.GetComponentsInChildren<Collider>(true);

        foreach (Collider forceFieldCollider in forceFieldColliders)
        {
            if (forceFieldCollider == null)
            {
                continue;
            }

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider != null)
                {
                    Physics.IgnoreCollision(forceFieldCollider, playerCollider, ignore);
                }
            }
        }
    }

    private void MakeForceFieldDoubleSided()
    {
        if (forceField == null)
        {
            return;
        }

        Renderer[] renderers = forceField.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[CabinForceFieldTrigger] Found {renderers.Length} renderers in forcefield.", this);
        
        foreach (Renderer forceFieldRenderer in renderers)
        {
            Debug.Log($"[CabinForceFieldTrigger] Processing renderer: {forceFieldRenderer.name}", this);
            
            foreach (Material material in forceFieldRenderer.materials)
            {
                if (material == null)
                    continue;

                // Try multiple property names for cull mode
                string[] cullPropertyNames = { "_Cull", "_CullMode", "_CullModeForward", "Cull" };
                
                bool wasSet = false;
                foreach (string propName in cullPropertyNames)
                {
                    if (material.HasProperty(propName))
                    {
                        material.SetInt(propName, (int)CullMode.Off);
                        Debug.Log($"[CabinForceFieldTrigger] Set {propName} to Off on material {material.name}", this);
                        wasSet = true;
                        break;
                    }
                }
                
                if (!wasSet)
                {
                    Debug.LogWarning($"[CabinForceFieldTrigger] Could not find cull property on material {material.name}. Available properties:", this);
                    // Log all shader property names for debugging
                    Shader shader = material.shader;
                    for (int i = 0; i < shader.GetPropertyCount(); i++)
                    {
                        Debug.Log($"  Property {i}: {shader.GetPropertyName(i)} ({shader.GetPropertyType(i)})", this);
                    }
                }
            }
        }
    }

    private static Transform GetTaggedRoot(Collider other, string tagName)
    {
        if (other.CompareTag(tagName))
        {
            return other.transform;
        }

        Transform parent = other.transform.parent;
        while (parent != null)
        {
            if (parent.CompareTag(tagName))
            {
                return parent;
            }

            parent = parent.parent;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.85f, 1f, 0.25f);

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.matrix = previousMatrix;
        }
    }
}
