using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class SegmentDescriptor : MonoBehaviour
{
    [Header("Semantic")]
    [LabelText("主要环境类型")]
    [SerializeField] private EnvironmentType primaryEnvironment = EnvironmentType.None;

    [Header("Layout")]
    [LabelText("显式长度")]
    [SerializeField] private float explicitLength;
    [LabelText("拾取物锚点根节点")]
    [SerializeField] private Transform pickupAnchorRoot;
    [LabelText("障碍物锚点根节点")]
    [SerializeField] private Transform obstacleAnchorRoot;

    public EnvironmentType PrimaryEnvironment => primaryEnvironment;
    public float ExplicitLength => Mathf.Max(0f, explicitLength);
    public Transform PickupAnchorRoot => pickupAnchorRoot;
    public Transform ObstacleAnchorRoot => obstacleAnchorRoot;

    private void Reset()
    {
        pickupAnchorRoot = pickupAnchorRoot != null ? pickupAnchorRoot : FindChildByName("PickupAnchors", "PickupAnchorRoot", "PickupPoints");
        obstacleAnchorRoot = obstacleAnchorRoot != null ? obstacleAnchorRoot : FindChildByName("ObstacleAnchors", "ObstacleAnchorRoot", "ObstaclePoints");
    }

    public void CollectPickupAnchors(List<Transform> results)
    {
        CollectAnchors(SegmentAnchorType.Pickup, results);
    }

    public void CollectPickupAnchorsInRange(float minX, float maxX, List<Transform> results)
    {
        CollectAnchorsInRange(SegmentAnchorType.Pickup, minX, maxX, results);
    }

    public void CollectObstacleAnchors(List<Transform> results)
    {
        CollectAnchors(SegmentAnchorType.Obstacle, results);
    }

    public void CollectAnchors(SegmentAnchorType anchorType, List<Transform> results)
    {
        CollectAnchors(GetAnchorRoot(anchorType), results);
    }

    public void CollectAnchorsInRange(SegmentAnchorType anchorType, float minX, float maxX, List<Transform> results)
    {
        CollectAnchorsInRange(GetAnchorRoot(anchorType), minX, maxX, results);
    }

    public Transform GetAnchorRoot(SegmentAnchorType anchorType)
    {
        return anchorType switch
        {
            SegmentAnchorType.Obstacle => obstacleAnchorRoot,
            _ => pickupAnchorRoot
        };
    }

    private Transform FindChildByName(params string[] candidates)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform child = transforms[i];
            if (child == null || child == transform)
            {
                continue;
            }

            for (int nameIndex = 0; nameIndex < candidates.Length; nameIndex++)
            {
                if (child.name == candidates[nameIndex])
                {
                    return child;
                }
            }
        }

        return null;
    }

    private static void CollectAnchors(Transform root, List<Transform> results)
    {
        if (root == null || results == null)
        {
            return;
        }

        if (root.childCount == 0)
        {
            results.Add(root);
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform anchor = root.GetChild(i);
            if (anchor != null)
            {
                results.Add(anchor);
            }
        }
    }

    private static void CollectAnchorsInRange(Transform root, float minX, float maxX, List<Transform> results)
    {
        if (root == null || results == null)
        {
            return;
        }

        if (root.childCount == 0)
        {
            if (root.position.x >= minX && root.position.x <= maxX)
            {
                results.Add(root);
            }

            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform anchor = root.GetChild(i);
            if (anchor != null && anchor.position.x >= minX && anchor.position.x <= maxX)
            {
                results.Add(anchor);
            }
        }
    }
}
