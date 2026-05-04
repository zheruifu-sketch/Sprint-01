using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class FormStatusUI : HudUIBase
{
    [Header("References")]
    [LabelText("玩家运行时上下文")]
    [SerializeField] private PlayerRuntimeContext runtimeContext;
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot playerFormRoot;
    [LabelText("环境规则控制器")]
    [SerializeField] private PlayerRuleController ruleController;
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;

    [Header("State Roots")]
    [LabelText("人形节点")]
    [SerializeField] private Transform humanRoot;
    [LabelText("汽车节点")]
    [SerializeField] private Transform carRoot;
    [LabelText("飞机节点")]
    [SerializeField] private Transform planeRoot;
    [LabelText("船形节点")]
    [SerializeField] private Transform boatRoot;

    private GameObject humanActive;
    private GameObject carActive;
    private GameObject planeActive;
    private GameObject boatActive;
    private GameObject humanLock;
    private GameObject carLock;
    private GameObject planeLock;
    private GameObject boatLock;
    private GameObject humanDisabled;
    private GameObject carDisabled;
    private GameObject planeDisabled;
    private GameObject boatDisabled;
    private PlayerFormType lastForm = (PlayerFormType)(-1);

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
        CacheActiveObjects();
        Refresh(true);
    }

    public override void Initialize()
    {
        AutoBind();
        CacheActiveObjects();
        Refresh(true);
    }

    private void LateUpdate()
    {
        Refresh(false);
    }

    private void AutoBind()
    {
        runtimeContext = runtimeContext != null ? runtimeContext : PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null)
        {
            runtimeContext.RefreshReferences();
            playerFormRoot = playerFormRoot != null ? playerFormRoot : runtimeContext.FormRoot;
            ruleController = ruleController != null ? ruleController : runtimeContext.RuleController;
        }

        if (playerFormRoot == null)
        {
            playerFormRoot = FindObjectOfType<PlayerFormRoot>();
        }

        if (ruleController == null)
        {
            ruleController = FindObjectOfType<PlayerRuleController>();
        }

        if (levelController == null)
        {
            levelController = FindObjectOfType<GameLevelController>();
        }

        if (humanRoot == null)
        {
            humanRoot = transform.Find("Human");
        }

        if (carRoot == null)
        {
            carRoot = transform.Find("Car");
        }

        if (planeRoot == null)
        {
            planeRoot = transform.Find("Plane");
        }

        if (boatRoot == null)
        {
            boatRoot = transform.Find("Boat");
        }
    }

    private void CacheActiveObjects()
    {
        humanActive = FindActiveChild(humanRoot);
        carActive = FindActiveChild(carRoot);
        planeActive = FindActiveChild(planeRoot);
        boatActive = FindActiveChild(boatRoot);
        humanLock = FindChildObject(humanRoot, "Lock");
        carLock = FindChildObject(carRoot, "Lock");
        planeLock = FindChildObject(planeRoot, "Lock");
        boatLock = FindChildObject(boatRoot, "Lock");
        humanDisabled = FindChildObject(humanRoot, "Disabled");
        carDisabled = FindChildObject(carRoot, "Disabled");
        planeDisabled = FindChildObject(planeRoot, "Disabled");
        boatDisabled = FindChildObject(boatRoot, "Disabled");
    }

    private static GameObject FindActiveChild(Transform root)
    {
        return FindChildObject(root, "Active");
    }

    private static GameObject FindChildObject(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        Transform child = root.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private void Refresh(bool force)
    {
        if (playerFormRoot == null)
        {
            return;
        }

        PlayerFormType currentForm = playerFormRoot.CurrentForm;
        if (!force && currentForm == lastForm)
        {
            RefreshRootVisibility();
            return;
        }

        RefreshRootVisibility();
        SetVisible(humanActive, currentForm == PlayerFormType.Human);
        SetVisible(carActive, currentForm == PlayerFormType.Car);
        SetVisible(planeActive, currentForm == PlayerFormType.Plane);
        SetVisible(boatActive, currentForm == PlayerFormType.Boat);
        lastForm = currentForm;
    }

    private static void SetVisible(GameObject target, bool visible)
    {
        if (target == null || target.activeSelf == visible)
        {
            return;
        }

        target.SetActive(visible);
    }

    private void RefreshRootVisibility()
    {
        RefreshState(humanLock, humanDisabled, humanActive, PlayerFormType.Human);
        RefreshState(carLock, carDisabled, carActive, PlayerFormType.Car);
        RefreshState(planeLock, planeDisabled, planeActive, PlayerFormType.Plane);
        RefreshState(boatLock, boatDisabled, boatActive, PlayerFormType.Boat);
    }

    private bool IsFormUnlocked(PlayerFormType formType)
    {
        if (levelController == null)
        {
            return true;
        }

        return levelController.IsFormUnlocked(formType);
    }

    private bool IsFormDisabled(PlayerFormType formType)
    {
        if (ruleController == null)
        {
            return false;
        }

        return !ruleController.CanUseForm(formType);
    }

    private void RefreshState(GameObject lockObject, GameObject disabledObject, GameObject activeObject, PlayerFormType formType)
    {
        bool unlocked = IsFormUnlocked(formType);
        bool disabled = unlocked && IsFormDisabled(formType);

        SetVisible(lockObject, !unlocked);
        SetVisible(disabledObject, disabled);

        if (unlocked)
        {
            return;
        }

        SetVisible(activeObject, false);
    }
}
