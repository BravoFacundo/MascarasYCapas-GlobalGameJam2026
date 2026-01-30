using System;
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
    public int unityLayer;

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
    [SerializeField] private LayerInfo maskedLayerInfo;
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
                isVisibleInGame = !startInvisible,
                unityLayer = LayerMask.NameToLayer(id)
            };

            if (info.layerId == "Masked") maskedLayerInfo = info;
            layers.Add(info);
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
        foreach (var r in layer.layerRoot.GetComponentsInChildren<Renderer>(true)) r.enabled = visible; */

    }

    public void CancelMaskForLayer(string layerId)
    {
        Debug.Log($"Cancel mask for layer {layerId}");
    }
    public void AssignMaskToLayer(string targetLayerId)
    {
        Debug.Log($"Assign mask to layer {targetLayerId}");
    }

    public void MoveObjectToMasked(GameObject obj)
    {
        if (obj == null) return;

        var realObj = FindRealParentObject(obj);
        
        var original = realObj.GetComponent<OriginalLayer>();
        if (original == null)
        {
            original = realObj.AddComponent<OriginalLayer>();
            original.originalLayerInfo = FindLayerInfoByParsingParent(realObj);
        }

        int maskedLayer = LayerMask.NameToLayer("Masked");
        SetLayerRecursively(realObj, maskedLayer);

        realObj.transform.SetParent(maskedLayerInfo.layerRoot, true);
    }
    public void RestoreObjectsFromMasked()
    {
        int maskedLayer = LayerMask.NameToLayer("Masked");

        // Copiamos hijos porque vamos a cambiar la jerarquía
        List<Transform> maskedObjects = new List<Transform>();
        foreach (Transform child in maskedLayerInfo.layerRoot)
        {
            maskedObjects.Add(child);
        }

        foreach (Transform t in maskedObjects)
        {
            var realObj = t.gameObject;
            var original = realObj.GetComponent<OriginalLayer>();

            if (original == null || original.originalLayerInfo == null)
                continue;

            LayerInfo originalLayerInfo = original.originalLayerInfo;

            // 🔹 Restaurar layer recursivamente
            SetLayerRecursively(realObj, originalLayerInfo.unityLayer);

            // 🔹 Restaurar parent
            realObj.transform.SetParent(originalLayerInfo.layerRoot, true);

            // 🔹 Limpieza
            Destroy(original);
        }
    }


    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // ============================= Check Name Info Functions =============================

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

    private GameObject FindRealParentObject(GameObject colliderTransform)
    {
        if (colliderTransform == null) return null;
        if (colliderTransform.transform.parent.name.StartsWith("Layer=")) return colliderTransform;

        Transform current = colliderTransform.transform;
        while (current.parent != null)
        {
            Transform parent = current.parent;

            if (parent.name.StartsWith("Layer="))
            {
                return current.gameObject;
            }

            current = parent;
        }
        return null;
    }

    private LayerInfo FindLayerInfoByParsingParent(GameObject realObj)
    {
        if (realObj == null || realObj.transform.parent == null)
            return null;

        Transform parent = realObj.transform.parent;

        if (!parent.name.StartsWith("Layer="))
            return null;

        // Removemos "Layer="
        string layerData = parent.name.Substring("Layer=".Length);

        // Cortamos en el próximo '=' si existe
        int extraIndex = layerData.IndexOf('=');
        string layerId = extraIndex >= 0
            ? layerData.Substring(0, extraIndex)
            : layerData;

        // Buscamos el LayerInfo correspondiente
        foreach (var layer in layers)
        {
            if (layer.layerId == layerId)
                return layer;
        }

        return null;
    }

}
