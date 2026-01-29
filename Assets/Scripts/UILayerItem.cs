using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILayerItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button visibilityButton;
    [SerializeField] private Button lockButton;

    [SerializeField] private RawImage primaryPreview;
    [SerializeField] private RawImage secondaryPreview;

    [SerializeField] private GameObject lockIcon;
    [SerializeField] private TMP_Text titleText;

    // ============================= Runtime =============================

    private LayerInfo layerInfo;
    private LayersController layersController;

    // ============================= Binding =============================

    public void Bind(LayerInfo info, LayersController controller)
    {
        layerInfo = info;
        layersController = controller;

        // Texto
        titleText.text = layerInfo.layerId;

        // Preview principal
        if (layerInfo.previewCamera != null)
            primaryPreview.texture = layerInfo.previewCamera.targetTexture;

        // Estado inicial
        lockIcon.SetActive(layerInfo.isLocked);
        secondaryPreview.gameObject.SetActive(false);

        UpdateVisibilityIcon();

        // Botones
        visibilityButton.onClick.RemoveAllListeners();
        visibilityButton.onClick.AddListener(OnVisibilityClicked);

        lockButton.onClick.RemoveAllListeners();
        lockButton.onClick.AddListener(OnLockClicked);
    }

    // ============================= UI Events =============================

    private void OnVisibilityClicked()
    {
        if (layerInfo == null || layersController == null)
            return;

        if (layerInfo.isLocked)
        {
            // Feedback visual / sonoro en el futuro
            Debug.Log($"Layer {layerInfo.layerId} está bloqueada");
            return;
        }

        layersController.ToggleLayerVisibility(layerInfo.layerId);
        UpdateVisibilityIcon();
    }

    private void OnLockClicked()
    {
        // Solo feedback (tooltip, shake, sonido, etc.)
        Debug.Log($"Layer {layerInfo.layerId} está bloqueada");
    }

    // ============================= Visual Updates =============================

    private void UpdateVisibilityIcon()
    {
        // Acá luego podés cambiar sprite ojo abierto / cerrado
        bool visible = layersController.IsLayerVisible(layerInfo.layerId);
        //primaryPreview.color = visible ? Color.white : new Color(1, 1, 1, 0.35f);
    }

    // ============================= Mask Preview (futuro) =============================

    public void ShowMaskedPreview(RenderTexture maskedTexture)
    {
        secondaryPreview.texture = maskedTexture;
        secondaryPreview.gameObject.SetActive(true);
    }
    public void HideMaskedPreview()
    {
        secondaryPreview.gameObject.SetActive(false);
        secondaryPreview.texture = null;
    }
}
