using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [Tooltip("The exact name of the scene you want to load (e.g., 'altar'). Make sure it is added to the Build Settings!")]
    [SerializeField] private string targetSceneName = "altar";
    
    [Header("Traveller Filter")]
    [Tooltip("Only objects with this tag can trigger the portal.")]
    [SerializeField] private string travellerTag = "Player";

    private bool isLoading = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"Portal '{gameObject.name}' has a collider but 'Is Trigger' is false. Automatically fixing it so the portal works.");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Prevent loading multiple times if the player collides with multiple parts of the trigger
        if (isLoading) return;

        // Check if the colliding object or its root has the correct tag
        if (other.CompareTag(travellerTag) || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(travellerTag)) || other.transform.root.CompareTag(travellerTag))
        {
            Debug.Log($"Portal entered! Loading scene: {targetSceneName}");
            isLoading = true;
            LoadTargetScene();
        }
    }

    private void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("Target Scene Name is empty on the ScenePortal component!");
            isLoading = false;
        }
    }
}
