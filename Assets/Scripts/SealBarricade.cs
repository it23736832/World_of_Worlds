using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SealBarricade : MonoBehaviour
{
    [SerializeField] private float _duration = 60f;

    private NavMeshObstacle _obstacle;
    private Animator        _animator;
    private Coroutine       _loopCoroutine;

    private void Start()
    {
        _obstacle = GetComponent<NavMeshObstacle>();
        _animator = GetComponent<Animator>();

        if (_obstacle != null)
        {
            _obstacle.carving = true;
            _obstacle.enabled = true;
        }

        if (_animator != null)
            _loopCoroutine = StartCoroutine(LoopStartAnimation());

        StartCoroutine(ExpireRoutine());
    }

    // Replays WaterSpellStart every time it finishes so the seal stays animated
    private IEnumerator LoopStartAnimation()
    {
        while (true)
        {
            _animator.Play("WaterSpellStart", 0, 0f);
            yield return null;

            // Wait until the clip finishes (normalizedTime reaches 1)
            AnimatorStateInfo state;
            do
            {
                yield return null;
                state = _animator.GetCurrentAnimatorStateInfo(0);
            }
            while (state.IsName("WaterSpellStart") && state.normalizedTime < 1f);
        }
    }

    private IEnumerator ExpireRoutine()
    {
        yield return new WaitForSeconds(_duration);

        // Stop looping and play the finish animation
        if (_loopCoroutine != null)
            StopCoroutine(_loopCoroutine);

        if (_animator != null)
            _animator.Play("WaterSpellFinish", 0, 0f);

        if (_obstacle != null)
            _obstacle.enabled = false;

        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}
