using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFlashEffectController : MonoBehaviour
{
    [Header("References")]
    [LabelText("玩家形态表现")]
    [SerializeField] private PlayerFormView formView;
    [LabelText("生命控制器")]
    [SerializeField] private PlayerHealthController healthController;
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;

    [Header("Flash")]
    [LabelText("出生闪烁时长")]
    [SerializeField] private float spawnFlashDuration = 0.5f;
    [LabelText("受伤闪烁时长")]
    [SerializeField] private float damageFlashDuration = 0.5f;
    [LabelText("闪烁次数")]
    [SerializeField] private int flashPulseCount = 2;
    [LabelText("闪烁最低透明度")]
    [SerializeField] private float flashAlpha = 0.25f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private float spawnFlashRemaining;
    private float damageFlashRemaining;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
        CacheRenderers();
        RestoreRendererColors();
    }

    private void Start()
    {
        if (sessionController != null && sessionController.HasActiveRun)
        {
            PlaySpawnFlash();
        }
    }

    private void OnEnable()
    {
        CacheReferences();

        if (healthController != null)
        {
            healthController.DamageTaken += HandleDamageTaken;
        }

        if (sessionController != null)
        {
            sessionController.RunStateChanged += HandleRunStateChanged;
        }
    }

    private void OnDisable()
    {
        if (healthController != null)
        {
            healthController.DamageTaken -= HandleDamageTaken;
        }

        if (sessionController != null)
        {
            sessionController.RunStateChanged -= HandleRunStateChanged;
        }

        RestoreRendererColors();
    }

    private void Update()
    {
        bool changed = false;
        if (spawnFlashRemaining > 0f)
        {
            spawnFlashRemaining = Mathf.Max(0f, spawnFlashRemaining - Time.deltaTime);
            changed = true;
        }

        if (damageFlashRemaining > 0f)
        {
            damageFlashRemaining = Mathf.Max(0f, damageFlashRemaining - Time.deltaTime);
            changed = true;
        }

        if (changed)
        {
            ApplyFlash();
        }
    }

    public void PlaySpawnFlash()
    {
        spawnFlashRemaining = Mathf.Max(spawnFlashRemaining, spawnFlashDuration);
        ApplyFlash();
    }

    public void PlayDamageFlash()
    {
        damageFlashRemaining = damageFlashDuration;
        ApplyFlash();
    }

    private void HandleDamageTaken(float _, float __, float ___)
    {
        PlayDamageFlash();
    }

    private void HandleRunStateChanged(GameRunState runState)
    {
        if (runState == GameRunState.Running)
        {
            PlaySpawnFlash();
        }
    }

    private void ApplyFlash()
    {
        if (renderers == null || baseColors == null)
        {
            return;
        }

        float targetAlpha = 1f;
        if (IsFlashHidden(spawnFlashRemaining, spawnFlashDuration))
        {
            targetAlpha = Mathf.Min(targetAlpha, flashAlpha);
        }

        if (IsFlashHidden(damageFlashRemaining, damageFlashDuration))
        {
            targetAlpha = Mathf.Min(targetAlpha, flashAlpha);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color color = baseColors[i];
            color.a *= targetAlpha;
            renderer.color = color;
        }
    }

    private void CacheReferences()
    {
        formView = formView != null ? formView : GetComponent<PlayerFormView>();
        healthController = healthController != null ? healthController : GetComponent<PlayerHealthController>();
        sessionController = sessionController != null ? sessionController : FindObjectOfType<GameSessionController>();
    }

    private void CacheRenderers()
    {
        renderers = formView != null ? formView.GetAllSpriteRenderers() : GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        }
    }

    private void RestoreRendererColors()
    {
        if (renderers == null || baseColors == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length && i < baseColors.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = baseColors[i];
            }
        }
    }

    private bool IsFlashHidden(float remaining, float totalDuration)
    {
        if (remaining <= 0f || totalDuration <= 0f)
        {
            return false;
        }

        int pulseCount = Mathf.Max(1, flashPulseCount);
        float elapsed = totalDuration - remaining;
        float normalizedProgress = Mathf.Clamp01(elapsed / totalDuration);
        float cycleProgress = normalizedProgress * pulseCount;
        float phase = cycleProgress - Mathf.Floor(cycleProgress);
        return phase >= 0.5f;
    }
}
