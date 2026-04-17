using UnityEngine;

public class PlayerFormView : MonoBehaviour
{
    [Header("Form Objects")]
    [SerializeField] private GameObject humanObject;
    [SerializeField] private GameObject carObject;
    [SerializeField] private GameObject planeObject;
    [SerializeField] private GameObject boatObject;

    [Header("Visual Targets")]
    [SerializeField] private Transform humanVisual;
    [SerializeField] private Transform carVisual;
    [SerializeField] private Transform planeVisual;
    [SerializeField] private Transform boatVisual;

    [Header("Facing")]
    [SerializeField] private bool humanFacesLeftByDefault = true;
    [SerializeField] private bool carFacesLeftByDefault = true;
    [SerializeField] private bool planeFacesLeftByDefault = true;
    [SerializeField] private bool boatFacesLeftByDefault = true;

    [Header("Animation")]
    [SerializeField] private string runParameterName = "Run";

    private Vector3 humanScale = Vector3.one;
    private Vector3 carScale = Vector3.one;
    private Vector3 planeScale = Vector3.one;
    private Vector3 boatScale = Vector3.one;
    private Animator humanAnimator;
    private Animator carAnimator;
    private Animator planeAnimator;
    private Animator boatAnimator;

    private void Awake()
    {
        ResolveVisualTargets();
        CacheAnimators();
        CacheBaseScales();
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

    private void CacheBaseScales()
    {
        humanScale = humanVisual != null ? humanVisual.localScale : Vector3.one;
        carScale = carVisual != null ? carVisual.localScale : Vector3.one;
        planeScale = planeVisual != null ? planeVisual.localScale : Vector3.one;
        boatScale = boatVisual != null ? boatVisual.localScale : Vector3.one;
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
}
