using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LoopingBackgroundStrip2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer sourceRenderer;

    [Header("Follow")]
    [SerializeField] private float horizontalParallax = 0.2f;
    [SerializeField] private float verticalParallax = 0.05f;
    [SerializeField] private bool followCameraY = true;

    [Header("Looping")]
    [SerializeField] private int extraTilesPerSide = 1;

    private readonly Dictionary<int, SpriteRenderer> generatedTiles = new Dictionary<int, SpriteRenderer>();
    private Vector3 basePosition;
    private Vector3 cameraStartPosition;
    private float tileWidth;
    private int centerTileIndex;

    private void Reset()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        CleanupLoopTileChildren();
        Initialize();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        CleanupLoopTileChildren();
        Initialize();
        UpdateTiles();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ClearGeneratedTiles();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!EnsureReady())
        {
            return;
        }

        UpdateTiles();
    }

    private void OnValidate()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Initialize()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        basePosition = transform.position;
        cameraStartPosition = targetCamera != null ? targetCamera.transform.position : Vector3.zero;

        if (!EnsureReady())
        {
            return;
        }

        sourceRenderer.drawMode = SpriteDrawMode.Simple;
        tileWidth = sourceRenderer.bounds.size.x;
        centerTileIndex = 0;
        generatedTiles[centerTileIndex] = sourceRenderer;
    }

    private bool EnsureReady()
    {
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return false;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return false;
        }

        tileWidth = sourceRenderer.bounds.size.x;
        return tileWidth > 0.001f;
    }

    private void UpdateTiles()
    {
        if (generatedTiles.Count == 0)
        {
            return;
        }

        float cameraOffsetX = (targetCamera.transform.position.x - cameraStartPosition.x) * horizontalParallax;
        float centerX = basePosition.x + cameraOffsetX;

        float targetY = basePosition.y;
        if (followCameraY)
        {
            targetY += (targetCamera.transform.position.y - cameraStartPosition.y) * verticalParallax;
        }

        float halfCameraWidth = GetCameraWidth() * 0.5f;
        int leftTileIndex = Mathf.FloorToInt((centerX - halfCameraWidth - extraTilesPerSide * tileWidth - basePosition.x) / tileWidth);
        int rightTileIndex = Mathf.CeilToInt((centerX + halfCameraWidth + extraTilesPerSide * tileWidth - basePosition.x) / tileWidth);

        EnsureTileRange(leftTileIndex, rightTileIndex);
        RecycleTilesOutsideRange(leftTileIndex, rightTileIndex);

        foreach (KeyValuePair<int, SpriteRenderer> pair in generatedTiles)
        {
            int tileIndex = pair.Key;
            SpriteRenderer tile = pair.Value;
            if (tile == null)
            {
                continue;
            }

            float offsetX = tileIndex * tileWidth;
            Transform tileTransform = tile.transform;
            tileTransform.position = new Vector3(basePosition.x + offsetX, targetY, basePosition.z);
        }
    }

    private float GetCameraWidth()
    {
        if (targetCamera.orthographic)
        {
            return targetCamera.orthographicSize * 2f * targetCamera.aspect;
        }

        float distance = Mathf.Abs(basePosition.z - targetCamera.transform.position.z);
        float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return height * targetCamera.aspect;
    }

    private void ClearGeneratedTiles()
    {
        foreach (KeyValuePair<int, SpriteRenderer> pair in generatedTiles)
        {
            SpriteRenderer tile = pair.Value;
            if (tile == null || tile == sourceRenderer)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(tile.gameObject);
            }
            else
            {
                DestroyImmediate(tile.gameObject);
            }
        }

        generatedTiles.Clear();
        if (sourceRenderer != null)
        {
            generatedTiles[centerTileIndex] = sourceRenderer;
        }
    }

    private void CleanupLoopTileChildren()
    {
        List<GameObject> staleLoopTiles = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || !child.name.StartsWith("LoopTile_"))
            {
                continue;
            }

            staleLoopTiles.Add(child.gameObject);
        }

        for (int i = 0; i < staleLoopTiles.Count; i++)
        {
            Destroy(staleLoopTiles[i]);
        }
    }

    private void EnsureTileRange(int leftTileIndex, int rightTileIndex)
    {
        for (int tileIndex = leftTileIndex; tileIndex <= rightTileIndex; tileIndex++)
        {
            if (generatedTiles.ContainsKey(tileIndex))
            {
                continue;
            }

            SpriteRenderer tileRenderer;
            if (tileIndex == centerTileIndex)
            {
                tileRenderer = sourceRenderer;
            }
            else
            {
                GameObject tileObject = new GameObject($"LoopTile_{tileIndex}");
                tileObject.transform.SetParent(transform, false);
                tileRenderer = tileObject.AddComponent<SpriteRenderer>();
                CopyRendererSettings(tileRenderer, sourceRenderer);
            }

            generatedTiles[tileIndex] = tileRenderer;
        }
    }

    private void RecycleTilesOutsideRange(int leftTileIndex, int rightTileIndex)
    {
        List<int> keysToRemove = new List<int>();

        foreach (KeyValuePair<int, SpriteRenderer> pair in generatedTiles)
        {
            int tileIndex = pair.Key;
            if (tileIndex >= leftTileIndex && tileIndex <= rightTileIndex)
            {
                continue;
            }

            if (tileIndex == centerTileIndex)
            {
                continue;
            }

            SpriteRenderer tile = pair.Value;
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }

            keysToRemove.Add(tileIndex);
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            generatedTiles.Remove(keysToRemove[i]);
        }
    }

    private static void CopyRendererSettings(SpriteRenderer target, SpriteRenderer source)
    {
        target.sprite = source.sprite;
        target.color = source.color;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
        target.material = source.sharedMaterial;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
        target.maskInteraction = source.maskInteraction;
        target.drawMode = SpriteDrawMode.Simple;
    }
}
