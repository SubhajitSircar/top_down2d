using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class AdvancedLight2DEffects : MonoBehaviour
{
    public enum PresetMode { ArcaneFire, FaultyBulb, ArcanePulse }

    [Header("Preset Selection")]
    [SerializeField] private PresetMode mode = PresetMode.ArcaneFire;

    [Header("Base Light Settings")]
    [SerializeField] private float baseIntensity = 1.6f;
    // Base Origin Purple: #69419F
    [SerializeField] private Color baseColor = new Color32(105, 65, 159, 255);

    [Header("Arcane Flame Settings")]
    // Bright glowing lavender burst during intensity spikes
    [SerializeField] private Color arcaneHotColor = new Color32(188, 139, 255, 255); // #BC8BFF
    // Deep void purple during low energy dips
    [SerializeField] private Color arcaneCoolColor = new Color32(46, 21, 72, 255);   // #2E1548
    [SerializeField] private float fireSpeed = 8f;
    [SerializeField] private float radiusJitter = 0.35f;

    [Header("Faulty Bulb Settings")]
    [Range(0f, 0.2f)]
    [SerializeField] private float stutterChance = 0.05f;
    [SerializeField] private float fastFlickerSpeed = 30f;

    [Header("Arcane Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float pulseIntensityRange = 0.8f;

    private Light2D m_Light2D;
    private float originalOuterRadius;
    private float noiseSeed;

    private void Awake()
    {
        m_Light2D = GetComponent<Light2D>();
        noiseSeed = Random.Range(0f, 100f);

        if (m_Light2D != null)
        {
            originalOuterRadius = m_Light2D.pointLightOuterRadius;
        }
    }

    private void Update()
    {
        if (m_Light2D == null) return;

        switch (mode)
        {
            case PresetMode.ArcaneFire:
                UpdateArcaneFire();
                break;
            case PresetMode.FaultyBulb:
                UpdateFaultyBulb();
                break;
            case PresetMode.ArcanePulse:
                UpdateArcanePulse();
                break;
        }
    }

    private void UpdateArcaneFire()
    {
        // Dual-layer noise for organic magic energy flicker
        float primaryNoise = Mathf.PerlinNoise(noiseSeed + Time.time * fireSpeed, 0f);
        float secondaryNoise = Mathf.PerlinNoise(0f, noiseSeed + Time.time * (fireSpeed * 1.8f)) * 0.5f;
        float combinedNoise = Mathf.Clamp01((primaryNoise + secondaryNoise) / 1.5f);

        // Intensity modulation
        m_Light2D.intensity = baseIntensity * (0.7f + combinedNoise * 0.6f);

        // Shift color from deep void purple -> base purple (#69419F) -> bright flare
        if (combinedNoise > 0.5f)
        {
            float t = (combinedNoise - 0.5f) * 2f;
            m_Light2D.color = Color.Lerp(baseColor, arcaneHotColor, t);
        }
        else
        {
            float t = combinedNoise * 2f;
            m_Light2D.color = Color.Lerp(arcaneCoolColor, baseColor, t);
        }

        // Modulate point light radius to simulate expanding magic aura
        if (m_Light2D.lightType == Light2D.LightType.Point)
        {
            m_Light2D.pointLightOuterRadius = originalOuterRadius + (combinedNoise - 0.5f) * radiusJitter;
        }
    }

    private void UpdateFaultyBulb()
    {
        if (Random.value < stutterChance)
        {
            m_Light2D.intensity = Random.Range(0f, baseIntensity * 0.2f);
        }
        else
        {
            float noise = Mathf.PerlinNoise(Time.time * fastFlickerSpeed, noiseSeed);
            m_Light2D.intensity = baseIntensity * Mathf.Lerp(0.6f, 1.3f, noise);
        }
        m_Light2D.color = baseColor;
    }

    private void UpdateArcanePulse()
    {
        // Smooth sine breathing with color temperature shift around #69419F
        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        m_Light2D.intensity = baseIntensity + (wave * pulseIntensityRange);
        m_Light2D.color = Color.Lerp(baseColor, arcaneHotColor, wave * 0.7f);

        if (m_Light2D.lightType == Light2D.LightType.Point)
        {
            m_Light2D.pointLightOuterRadius = originalOuterRadius + (wave * radiusJitter);
        }
    }
}