using UnityEngine;
using System.Collections.Generic;

public class CanvasLayersHandler : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform layersContainer;
    [SerializeField] private UILayerItem layerItemPrefab;

    private LayersController layersController;
    private readonly List<UILayerItem> spawnedItems = new();

    private void OnEnable()
    {
        TryBuildUI();
    }

    // ============================= Core =============================

    private void TryBuildUI()
    {
        layersController = FindObjectOfType<LayersController>();

        if (layersController == null)
        {
            Debug.LogWarning("CanvasLayersHandler: No se encontró LayersController activo");
            return;
        }

        BuildUI(layersController);
    }

    public void BuildUI(LayersController controller)
    {
        ClearUI();
        layersController = controller;

        foreach (var layerInfo in layersController.GetLayers())
        {
            if (layerInfo.layerId != "Masked")
            {
                UILayerItem item = Instantiate(layerItemPrefab, layersContainer);
                item.Bind(layerInfo, layersController);
                spawnedItems.Add(item);
            }
        }
    }

    private void ClearUI()
    {
        // Limpia referencias runtime
        spawnedItems.Clear();

        // Destruye TODOS los hijos del contenedor
        for (int i = layersContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = layersContainer.GetChild(i);
            Destroy(child.gameObject);
        }
    }

}
