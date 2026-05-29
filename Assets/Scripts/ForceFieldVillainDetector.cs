using UnityEngine;

// Attach to any force field GameObject with a SphereCollider trigger.
// _isEndGame = true  → villain stops permanently (goal cabin / end game).
// _isEndGame = false → villain is stunned for 10 s then resumes (Rumi's shield).
public class ForceFieldVillainDetector : MonoBehaviour
{
    [SerializeField] private bool _isEndGame = false;

    private void OnTriggerEnter(Collider other)
    {
        AStarVillainChase villain = other.GetComponent<AStarVillainChase>();
        if (villain == null) villain = other.GetComponentInParent<AStarVillainChase>();
        if (villain == null) return;

        if (_isEndGame)
            villain.SetIdle();
        else
            villain.HitByForceField();
    }
}
