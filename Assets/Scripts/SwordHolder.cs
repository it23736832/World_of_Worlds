using UnityEngine;

public class SwordHolder : MonoBehaviour
{
    [SerializeField] private GameObject _swordPrefab;

    [Header("Grip Adjustment")]
    [SerializeField] private Vector3 _positionOffset = new Vector3(0f, 0.001f, 0.0005f);
    [SerializeField] private Vector3 _rotationOffset = new Vector3(0f, 51.4f, 91.32f);
    [SerializeField] private float   _scale = 0.005f;

    private GameObject _sword;

    private void Start()
    {
        Attach();
    }

    private void OnDestroy() => Detach();

    private void Attach()
    {
        Detach();
        if (_swordPrefab == null) return;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null) return;

        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand == null) return;

        // Destroy any orphaned swords from previous edit-mode runs
        for (int i = rightHand.childCount - 1; i >= 0; i--)
        {
            if (rightHand.GetChild(i).name == "Sword_InHand")
                Destroy(rightHand.GetChild(i).gameObject);
        }

        _sword = Instantiate(_swordPrefab, rightHand);
        _sword.name = "Sword_InHand";
        _sword.transform.localPosition = _positionOffset;
        _sword.transform.localRotation = Quaternion.Euler(_rotationOffset);
        _sword.transform.localScale    = Vector3.one * _scale;
    }

    private void Detach()
    {
        if (_sword != null)
        {
            Destroy(_sword);
            _sword = null;
        }
    }
}
