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
        }

        if (playerFormRoot == null)
        {
            playerFormRoot = FindObjectOfType<PlayerFormRoot>();
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
        RefreshLock(humanLock, PlayerFormType.Human);
        RefreshLock(carLock, PlayerFormType.Car);
        RefreshLock(planeLock, PlayerFormType.Plane);
        RefreshLock(boatLock, PlayerFormType.Boat);
    }

    private bool IsFormUnlocked(PlayerFormType formType)
    {
        if (levelController == null)
        {
            return true;
        }

        return levelController.IsFormUnlocked(formType);
    }

    private void RefreshLock(GameObject lockObject, PlayerFormType formType)
    {
        bool unlocked = IsFormUnlocked(formType);
        SetVisible(lockObject, !unlocked);

        if (unlocked)
        {
            return;
        }

        switch (formType)
        {
            case PlayerFormType.Human:
                SetVisible(humanActive, false);
                break;
            case PlayerFormType.Car:
                SetVisible(carActive, false);
                break;
            case PlayerFormType.Plane:
                SetVisible(planeActive, false);
                break;
            case PlayerFormType.Boat:
                SetVisible(boatActive, false);
                break;
        }
    }
}
