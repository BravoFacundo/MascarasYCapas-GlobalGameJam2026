using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LayerInfo
{
    [Header("Identity")]
    public string layerId;

    [Header("World")]
    public Transform layerRoot;
    public Camera previewCamera;

    [Header("State")]
    public bool isVisibleInGame;
    public bool isLocked;
}

public class LayersController : MonoBehaviour
{
    [Header("Level Root")]
    [SerializeField] private Transform levelRoot;
    
    [Header("Level Camera (composite view)")]
    [SerializeField] private Camera levelCam;

    [Header("Runtime Data (Read Only)")]
    [SerializeField] private List<LayerInfo> layers = new();

    // ============================= Unity =============================

    private void Awake()
    {
        BuildLayers();
        ApplyInitialState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            BuildLayers();
    }
#endif

    // ============================= Build =============================

    private void BuildLayers()
    {
        layers.Clear();

        if (levelRoot == null)
            return;

        foreach (Transform layerTransform in levelRoot)
        {
            if (!layerTransform.name.StartsWith("Layer="))
                continue;

            ParseLayerName(
                layerTransform.name,
                out string id,
                out bool locked,
                out bool startInvisible
            );

            LayerInfo info = new LayerInfo
            {
                layerId = id,
                layerRoot = layerTransform,
                previewCamera = layerTransform.GetComponentInChildren<Camera>(true),
                isLocked = locked,
                isVisibleInGame = !startInvisible
            };

            layers.Add(info);
        }
    }

    private void ParseLayerName(
        string name,
        out string id,
        out bool isLocked,
        out bool startInvisible)
    {
        id = string.Empty;
        isLocked = false;
        startInvisible = false;

        string[] parts = name.Split('=');
        if (parts.Length < 2)
            return;

        id = parts[1];

        for (int i = 2; i < parts.Length; i++)
        {
            if (parts[i].Equals("Locked", System.StringComparison.OrdinalIgnoreCase))
                isLocked = true;

            if (parts[i].Equals("StartInvisible", System.StringComparison.OrdinalIgnoreCase))
                startInvisible = true;
        }
    }

    // ============================= Initial State =============================

    private void ApplyInitialState()
    {
        foreach (var layer in layers)
            ApplyLayerVisibility(layer, layer.isVisibleInGame);
    }

    // ============================= Public API =============================

    public IReadOnlyList<LayerInfo> GetLayers()
    {
        return layers;
    }

    public void ToggleLayerVisibility(string layerId)
    {
        LayerInfo layer = GetLayer(layerId);
        if (layer == null || layer.isLocked)
            return;

        SetLayerVisible(layerId, !layer.isVisibleInGame);
    }

    public void SetLayerVisible(string layerId, bool visible)
    {
        LayerInfo layer = GetLayer(layerId);
        if (layer == null || layer.isLocked)
            return;

        layer.isVisibleInGame = visible;
        ApplyLayerVisibility(layer, visible);
    }

    public bool IsLayerVisible(string layerId)
    {
        LayerInfo layer = GetLayer(layerId);
        return layer != null && layer.isVisibleInGame;
    }

    public bool IsLayerLocked(string layerId)
    {
        LayerInfo layer = GetLayer(layerId);
        return layer != null && layer.isLocked;
    }

    // ============================= Internal =============================

    private LayerInfo GetLayer(string layerId)
    {
        return layers.Find(l => l.layerId == layerId);
    }

    private void ApplyLayerVisibility(LayerInfo layer, bool visible)
    {
        if (layer.layerRoot == null)
            return;

        // 1. Gameplay: colliders
        foreach (var col2D in layer.layerRoot.GetComponentsInChildren<Collider2D>(true))
            col2D.enabled = visible;
        foreach (var col3D in layer.layerRoot.GetComponentsInChildren<Collider>(true))
            col3D.enabled = visible;

        // 2. Visual: composición del nivel (levelCam)
        if (levelCam == null)
            return;

        int unityLayer = LayerMask.NameToLayer(layer.layerId);
        if (unityLayer == -1)
        {
            Debug.LogWarning($"Unity Layer '{layer.layerId}' no existe.");
            return;
        }

        if (visible)
            levelCam.cullingMask |= (1 << unityLayer);   // agregar
        else
            levelCam.cullingMask &= ~(1 << unityLayer);  // quitar

        /* Renderers
        foreach (var r in layer.layerRoot.GetComponentsInChildren<Renderer>(true))
            r.enabled = visible; */

    }

}
