using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using System.Collections.Generic;

public class PlayerFormView : MonoBehaviour
{
    [Header("Form Objects")]
    [LabelText("人形对象")]
    [SerializeField] private GameObject humanObject;
    [LabelText("汽车对象")]
    [SerializeField] private GameObject carObject;
    [LabelText("飞机对象")]
    [SerializeField] private GameObject planeObject;
    [LabelText("船对象")]
    [SerializeField] private GameObject boatObject;

    [Header("Visual Targets")]
    [LabelText("人形朝向节点")]
    [SerializeField] private Transform humanVisual;
    [LabelText("汽车朝向节点")]
    [SerializeField] private Transform carVisual;
    [LabelText("飞机朝向节点")]
    [SerializeField] private Transform planeVisual;
    [LabelText("船朝向节点")]
    [SerializeField] private Transform boatVisual;

    [Header("Facing")]
    [LabelText("人形默认朝左")]
    [SerializeField] private bool humanFacesLeftByDefault = true;
    [LabelText("汽车默认朝左")]
    [SerializeField] private bool carFacesLeftByDefault = true;
    [LabelText("飞机默认朝左")]
    [SerializeField] private bool planeFacesLeftByDefault = true;
    [LabelText("船默认朝左")]
    [SerializeField] private bool boatFacesLeftByDefault = true;

    [Header("Animation")]
    [LabelText("跑步参数名")]
    [SerializeField] private string runParameterName = "Run";

    [Header("Boat Float")]
    [LabelText("船只上下浮动")]
    [SerializeField] private bool enableBoatFloat = true;
    [LabelText("船只浮动幅度")]
    [SerializeField] private float boatFloatAmplitude = 0.08f;
    [LabelText("船只浮动频率")]
    [SerializeField] private float boatFloatFrequency = 1.35f;
    [LabelText("船只晃动角度")]
    [SerializeField] private float boatRockAngle = 2.5f;
    [LabelText("船只晃动频率")]
    [SerializeField] private float boatRockFrequency = 0.9f;

    [Header("References")]
    [LabelText("环境规则控制器")]
    [SerializeField] private PlayerRuleController ruleController;

    private Vector3 humanScale = Vector3.one;
    private Vector3 carScale = Vector3.one;
    private Vector3 planeScale = Vector3.one;
    private Vector3 boatScale = Vector3.one;
    private Animator humanAnimator;
    private Animator carAnimator;
    private Animator planeAnimator;
    private Animator boatAnimator;
    private SpriteRenderer[] cachedSpriteRenderers;
    private Vector3 boatBaseLocalPosition;
    private Quaternion boatBaseLocalRotation;

    private void Awake()
    {
        CacheReferences();
        ResolveVisualTargets();
        CacheAnimators();
        CacheBaseScales();
        CacheBoatVisualBasePose();
        CacheSpriteRenderers();
    }

    private void LateUpdate()
    {
        UpdateBoatFloat();
    }

    public void ShowForm(PlayerFormType formType)
    {
        SetFormActive(humanObject, formType == PlayerFormType.Human);
        SetFormActive(carObject, formType == PlayerFormType.Car);
        SetFormActive(planeObject, formType == PlayerFormType.Plane);
        SetFormActive(boatObject, formType == PlayerFormType.Boat);
    }

    public void SetFacing(bool movingLeft)
    {
        ApplyScale(humanVisual, humanScale, ResolveVisualFacing(humanFacesLeftByDefault, movingLeft));
        ApplyScale(carVisual, carScale, ResolveVisualFacing(carFacesLeftByDefault, movingLeft));
        ApplyScale(planeVisual, planeScale, ResolveVisualFacing(planeFacesLeftByDefault, movingLeft));
        ApplyScale(boatVisual, boatScale, ResolveVisualFacing(boatFacesLeftByDefault, movingLeft));
    }

    public void SetRunState(PlayerFormType activeForm, bool isRunning)
    {
        SetRun(ResolveAnimator(activeForm), isRunning);
    }

    public SpriteRenderer[] GetAllSpriteRenderers()
    {
        if (cachedSpriteRenderers == null || cachedSpriteRenderers.Length == 0)
        {
            CacheSpriteRenderers();
        }

        return cachedSpriteRenderers;
    }

    private void CacheBaseScales()
    {
        humanScale = humanVisual != null ? humanVisual.localScale : Vector3.one;
        carScale = carVisual != null ? carVisual.localScale : Vector3.one;
        planeScale = planeVisual != null ? planeVisual.localScale : Vector3.one;
        boatScale = boatVisual != null ? boatVisual.localScale : Vector3.one;
    }

    private void CacheBoatVisualBasePose()
    {
        if (boatVisual == null)
        {
            boatBaseLocalPosition = Vector3.zero;
            boatBaseLocalRotation = Quaternion.identity;
            return;
        }

        boatBaseLocalPosition = boatVisual.localPosition;
        boatBaseLocalRotation = boatVisual.localRotation;
    }

    private void CacheReferences()
    {
        ruleController = ruleController != null ? ruleController : GetComponentInParent<PlayerRuleController>();
    }

    private void ResolveVisualTargets()
    {
        humanVisual = ResolveVisualTarget(humanObject, humanVisual);
        carVisual = ResolveVisualTarget(carObject, carVisual);
        planeVisual = ResolveVisualTarget(planeObject, planeVisual);
        boatVisual = ResolveVisualTarget(boatObject, boatVisual);
    }

    private void CacheAnimators()
    {
        humanAnimator = humanObject != null ? humanObject.GetComponent<Animator>() : null;
        carAnimator = carObject != null ? carObject.GetComponent<Animator>() : null;
        planeAnimator = planeObject != null ? planeObject.GetComponent<Animator>() : null;
        boatAnimator = boatObject != null ? boatObject.GetComponent<Animator>() : null;
    }

    private void CacheSpriteRenderers()
    {
        List<SpriteRenderer> renderers = new List<SpriteRenderer>(16);
        AppendSpriteRenderers(humanObject, renderers);
        AppendSpriteRenderers(carObject, renderers);
        AppendSpriteRenderers(planeObject, renderers);
        AppendSpriteRenderers(boatObject, renderers);
        cachedSpriteRenderers = renderers.ToArray();
    }

    private static void SetFormActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static Transform ResolveVisualTarget(GameObject rootObject, Transform explicitVisual)
    {
        if (explicitVisual != null)
        {
            return explicitVisual;
        }

        if (rootObject == null)
        {
            return null;
        }

        Transform rootTransform = rootObject.transform;
        return rootTransform.childCount > 0 ? rootTransform.GetChild(0) : rootTransform;
    }

    private static bool ResolveVisualFacing(bool facesLeftByDefault, bool movingLeft)
    {
        return facesLeftByDefault ? movingLeft : !movingLeft;
    }

    private Animator ResolveAnimator(PlayerFormType formType)
    {
        return formType switch
        {
            PlayerFormType.Human => humanAnimator,
            PlayerFormType.Car => carAnimator,
            PlayerFormType.Plane => planeAnimator,
            PlayerFormType.Boat => boatAnimator,
            _ => null
        };
    }

    private void SetRun(Animator animator, bool isRunning)
    {
        if (!CanWriteAnimatorParameter(animator) || string.IsNullOrEmpty(runParameterName))
        {
            return;
        }

        animator.SetBool(runParameterName, isRunning);
    }

    private static bool CanWriteAnimatorParameter(Animator animator)
    {
        if (animator == null)
        {
            return false;
        }

        return animator.isActiveAndEnabled
               && animator.gameObject.activeInHierarchy
               && animator.runtimeAnimatorController != null;
    }

    private void UpdateBoatFloat()
    {
        if (boatVisual == null)
        {
            return;
        }

        bool shouldFloat = enableBoatFloat
                           && ruleController != null
                           && ruleController.IsInWater()
                           && gameObject.activeInHierarchy;

        if (!shouldFloat)
        {
            ResetBoatVisualPose();
            return;
        }

        float bobOffset = Mathf.Sin(Time.time * Mathf.Max(0f, boatFloatFrequency) * Mathf.PI * 2f) * Mathf.Max(0f, boatFloatAmplitude);
        float rockAngle = Mathf.Sin(Time.time * Mathf.Max(0f, boatRockFrequency) * Mathf.PI * 2f) * boatRockAngle;

        Vector3 nextPosition = boatBaseLocalPosition;
        nextPosition.y += bobOffset;
        boatVisual.localPosition = nextPosition;
        boatVisual.localRotation = boatBaseLocalRotation * Quaternion.Euler(0f, 0f, rockAngle);
    }

    private void ResetBoatVisualPose()
    {
        if (boatVisual == null)
        {
            return;
        }

        boatVisual.localPosition = boatBaseLocalPosition;
        boatVisual.localRotation = boatBaseLocalRotation;
    }

    private static void ApplyScale(Transform target, Vector3 baseScale, bool faceLeft)
    {
        if (target == null)
        {
            return;
        }

        Vector3 nextScale = baseScale;
        nextScale.x = Mathf.Abs(baseScale.x) * (faceLeft ? 1f : -1f);
        target.localScale = nextScale;
    }

    private static void AppendSpriteRenderers(GameObject rootObject, List<SpriteRenderer> renderers)
    {
        if (rootObject == null || renderers == null)
        {
            return;
        }

        SpriteRenderer[] targets = rootObject.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < targets.Length; i++)
        {
            SpriteRenderer renderer = targets[i];
            if (renderer != null)
            {
                renderers.Add(renderer);
            }
        }
    }
}
