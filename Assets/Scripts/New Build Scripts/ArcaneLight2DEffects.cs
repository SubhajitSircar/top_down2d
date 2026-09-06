using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class ArcaneLight2DEffects : MonoBehaviour
{
    public enum PresetMode { ArcaneFlame, FaultyBulb, ArcanePulse }

    [Header("--- Preset Selection ---")]
    [SerializeField] private PresetMode mode = PresetMode.ArcaneFlame;

    [Header("--- Base Light Settings ---")]
    [SerializeField] private float baseIntensity = 1.6f;
    [SerializeField] private Color baseColor = new Color32(105, 65, 159, 255);

    [Header("--- Arcane Color & Flicker ---")]
    [SerializeField] private Color arcaneHotColor = new Color32(188, 139, 255, 255);
    [SerializeField] private Color arcaneCoolColor = new Color32(46, 21, 72, 255);
    [SerializeField] private float fireSpeed = 8f;
    [SerializeField] private float radiusJitter = 0.35f;

    [Header("--- Feature 1: Shadow Wobble ---")]
    [SerializeField] private bool enableShadowWobble = true;
    [SerializeField] private float positionJitterAmount = 0.04f;

    [Header("--- Feature 2: Particle Sync ---")]
    [SerializeField] private ParticleSystem magicParticles;
    [SerializeField] private float maxEmissionRate = 25f;

    [Header("--- Feature 3: Player Proximity Resonance ---")]
    [SerializeField] private bool enableProximityResonance = false;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float proximitySpeedBoost = 10f;

    private Light2D m_Light2D;
    private float originalOuterRadius;
    private Vector3 initialLocalPos;
    private float noiseSeed;
    private bool isBursting = false;
    private Coroutine burstCoroutine;

    private void Awake()
    {
        m_Light2D = GetComponent<Light2D>();
        noiseSeed = Random.Range(0f, 100f);
        initialLocalPos = transform.localPosition;

        if (m_Light2D != null)
        {
            originalOuterRadius = m_Light2D.pointLightOuterRadius;
        }
    }

    private void OnEnable()
    {
        // Automatically subscribe to gameplay events across all scripts
        NewPlayerCombat.OnSpellCast += HandleLightBurst;
        NewPlayerHealth.OnPlayerHurt += HandleLightBurst;
        NewPlayerMovement.OnDash += HandleLightBurst;
    }

    private void OnDisable()
    {
        // Unsubscribe to clean up references on object destroy or scene change
        NewPlayerCombat.OnSpellCast -= HandleLightBurst;
        NewPlayerHealth.OnPlayerHurt -= HandleLightBurst;
        NewPlayerMovement.OnDash -= HandleLightBurst;
    }

    private void HandleLightBurst(float multiplier, float duration)
    {
        TriggerBurst(multiplier, duration);
    }

    private void Update()
    {
        if (m_Light2D == null) return;

        float proximityFactor = 0f;
        if (enableProximityResonance && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            proximityFactor = 1f - Mathf.Clamp01(dist / detectionRadius);
        }

        float currentSpeed = fireSpeed + (proximityFactor * proximitySpeedBoost);

        switch (mode)
        {
            case PresetMode.ArcaneFlame:
                UpdateArcaneFlame(currentSpeed, proximityFactor);
                break;
            case PresetMode.FaultyBulb:
                UpdateFaultyBulb(currentSpeed);
                break;
            case PresetMode.ArcanePulse:
                UpdateArcanePulse(currentSpeed, proximityFactor);
                break;
        }

        if (enableShadowWobble)
        {
            ApplyShadowWobble(currentSpeed);
        }
    }

    private void UpdateArcaneFlame(float speed, float proximityFactor)
    {
        float primaryNoise = Mathf.PerlinNoise(noiseSeed + Time.time * speed, 0f);
        float secondaryNoise = Mathf.PerlinNoise(0f, noiseSeed + Time.time * (speed * 1.8f)) * 0.5f;
        float combinedNoise = Mathf.Clamp01((primaryNoise + secondaryNoise) / 1.5f);

        if (!isBursting)
        {
            float effectiveBase = baseIntensity + (proximityFactor * 0.5f);
            m_Light2D.intensity = effectiveBase * (0.7f + combinedNoise * 0.6f);

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
        }

        if (m_Light2D.lightType == Light2D.LightType.Point)
        {
            m_Light2D.pointLightOuterRadius = originalOuterRadius + (combinedNoise - 0.5f) * radiusJitter;
        }

        SyncParticles(combinedNoise + proximityFactor * 0.3f);
    }

    private void UpdateFaultyBulb(float speed)
    {
        if (!isBursting)
        {
            if (Random.value < 0.05f)
            {
                m_Light2D.intensity = Random.Range(0f, baseIntensity * 0.2f);
            }
            else
            {
                float noise = Mathf.PerlinNoise(Time.time * speed * 3f, noiseSeed);
                m_Light2D.intensity = baseIntensity * Mathf.Lerp(0.6f, 1.3f, noise);
            }
            m_Light2D.color = baseColor;
        }
    }

    private void UpdateArcanePulse(float speed, float proximityFactor)
    {
        float wave = (Mathf.Sin(Time.time * speed * 0.4f) + 1f) * 0.5f;

        if (!isBursting)
        {
            float effectiveBase = baseIntensity + (proximityFactor * 0.5f);
            m_Light2D.intensity = effectiveBase + (wave * 0.8f);
            m_Light2D.color = Color.Lerp(baseColor, arcaneHotColor, wave * 0.7f);
        }

        if (m_Light2D.lightType == Light2D.LightType.Point)
        {
            m_Light2D.pointLightOuterRadius = originalOuterRadius + (wave * radiusJitter);
        }

        SyncParticles(wave);
    }

    private void ApplyShadowWobble(float speed)
    {
        Vector3 jitter = new Vector3(
            (Mathf.PerlinNoise(Time.time * speed, noiseSeed) - 0.5f) * positionJitterAmount,
            (Mathf.PerlinNoise(noiseSeed, Time.time * speed) - 0.5f) * positionJitterAmount,
            0f
        );
        transform.localPosition = initialLocalPos + jitter;
    }

    private void SyncParticles(float energyLevel)
    {
        if (magicParticles == null) return;
        var emission = magicParticles.emission;
        emission.rateOverTime = Mathf.Clamp01(energyLevel) * maxEmissionRate;
    }

    public void TriggerBurst(float burstMultiplier = 2.5f, float duration = 0.5f)
    {
        if (burstCoroutine != null) StopCoroutine(burstCoroutine);
        burstCoroutine = StartCoroutine(BurstRoutine(burstMultiplier, duration));
    }

    private IEnumerator BurstRoutine(float multiplier, float duration)
    {
        isBursting = true;
        float elapsed = 0f;
        float peakIntensity = baseIntensity * multiplier;
        Color peakColor = Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            m_Light2D.intensity = Mathf.Lerp(peakIntensity, baseIntensity, t);
            m_Light2D.color = Color.Lerp(peakColor, baseColor, t);
            yield return null;
        }

        isBursting = false;
    }
}