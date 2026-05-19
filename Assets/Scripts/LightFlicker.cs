using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float _minIntensity = 0.5f;
    [SerializeField] private float _maxIntensity = 2f;
    [SerializeField] private float _flickerSpeed = 0.05f;
    [SerializeField] private bool _randomizeOffset = true;

    private Light _light;
    private float _timer;

    void Awake()
    {
        _light = GetComponent<Light>();
        if (_randomizeOffset)
            _timer = Random.Range(0f, 100f);
    }

    void Update()
    {
        _timer += Time.deltaTime;
        float noise = Mathf.PerlinNoise(_timer / _flickerSpeed, 0f);
        _light.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, noise);
    }
}
