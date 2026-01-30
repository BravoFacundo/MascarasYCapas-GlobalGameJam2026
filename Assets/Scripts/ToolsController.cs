using System;
using UnityEngine;

public interface ITool
{
    void Tick();
}
public class ToolsController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private LayersController layersController;
    [SerializeField] private Camera levelCam;

    [SerializeField] private String _activeTool;
    private ITool activeTool;

    private void Start() => SelectMagicWand();

    public void SelectMagicWand()
    {
        activeTool = new MagicWandTool(layersController, levelCam);
        _activeTool = activeTool.ToString();
    }

    private void Update()
    {
        activeTool?.Tick();
    }
}
public class MagicWandTool : ITool
{
    private readonly LayersController layersController;
    private readonly Camera levelCam;

    public MagicWandTool(LayersController controller, Camera cam)
    {
        layersController = controller;
        levelCam = cam;
    }

    public void Tick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 worldPos = levelCam.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (!hit) return;

        GameObject target = hit.collider.gameObject;
        if (target.layer == LayerMask.NameToLayer("Masked")) return;
        if (target.layer == LayerMask.NameToLayer("Default")) return;

        Debug.Log($"[MAGIC WAND] Seleccionado: {target.name}");
        layersController.MoveObjectToMasked(target);
    }
}

