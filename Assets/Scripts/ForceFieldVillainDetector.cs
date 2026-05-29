using UnityEngine;

// Attach to the forceField GameObject (the one with the SphereCollider trigger).
// Detects when the villain enters and triggers the knockback + fall animation.
public class ForceFieldVillainDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        AStarVillainChase villain = other.GetComponent<AStarVillainChase>();
        if (villain == null) villain = other.GetComponentInParent<AStarVillainChase>();
        if (villain == null) return;

        villain.HitByForceField();
    }
}
