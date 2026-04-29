using UnityEngine;

public class PlayerFormStatusUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRuntimeContext runtimeContext;
    [SerializeField] private PlayerFormRoot playerFormRoot;
    [SerializeField] private GameLevelController levelController;

    [Header("State Roots")]
    [SerializeField] private Transform humanRoot;
    [SerializeField] private Transform carRoot;
    [SerializeField] private Transform planeRoot;
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

    private void Reset()
    {
        AutoBind();
    }

    private void Awake()
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
            levelController = GameLevelController.GetOrCreateInstance();
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
