using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UILayerItem : MonoBehaviour, IDropHandler
{
    [Header("Main Component")]
    [SerializeField] private RawImage primaryPreview;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button stateButton;
    [SerializeField] private Transform leftContainer;

    [Header("Mask Component")]
    [SerializeField] private GameObject secondaryContainer;
    [SerializeField] private RawImage secondaryPreview;
    [SerializeField] private Button linkButton;

    [Header("Icons")]
    [SerializeField] private Sprite showIcon;
    [SerializeField] private Sprite hideIcon;
    [SerializeField] private Sprite lockIcon;
    [SerializeField] private Sprite linkIcon;
    [SerializeField] private Sprite unlinkIcon;

    [Header("References")]
    private LayerInfo layerInfo;
    private LayersController layersController;
    private Image stateButtonIcon;
    private Image linkButtonIcon;

    private bool hasMaskedGroup;
    private bool isMasked;
    private bool isVisible = true;
    private bool isLocked;

    // ============================= Binding =============================

    public void Bind(LayerInfo info, LayersController controller)
    {
        layerInfo = info;
        layersController = controller;

        // Cache icons
        stateButtonIcon = stateButton.GetComponent<Image>();
        linkButtonIcon = linkButton.GetComponent<Image>();

        // Title
        titleText.text = layerInfo.layerId;
        name = "LayerItem=" + layerInfo.layerId;

        // Primary preview
        if (layerInfo.previewCamera != null)
            primaryPreview.texture = layerInfo.previewCamera.targetTexture;

        // Secondary (masked) starts hidden
        //secondaryContainer.SetActive(false);
        secondaryPreview.texture = null;

        // Initial visuals
        RefreshStateButton();
        RefreshLinkButton(false);

        // Button bindings
        stateButton.onClick.RemoveAllListeners();
        stateButton.onClick.AddListener(OnStateButtonClicked);

        linkButton.onClick.RemoveAllListeners();
        linkButton.onClick.AddListener(OnLinkButtonClicked);
    }

    // ============================= UI Events =============================

    private void OnStateButtonClicked()
    {
        if (layerInfo == null || layersController == null)
            return;

        if (layerInfo.isLocked) // Feedback negativo (visual/sonoro luego)
        {
            Debug.Log($"Layer {layerInfo.layerId} está bloqueada");
            return;
        }

        layersController.ToggleLayerVisibility(layerInfo.layerId);
        RefreshStateButton();
    }

    private void OnLinkButtonClicked()
    {
        if (!hasMaskedGroup)
            return;

        // Emitimos intención
        layersController.CancelMaskForLayer(layerInfo.layerId);

        // Volvemos UI a estado base
        HideMaskedPreview();
    }

    // ============================= Visual Updates =============================

    private void RefreshStateButton()
    {
        if (layerInfo.isLocked)
        {
            stateButtonIcon.sprite = lockIcon;
            return;
        }

        bool visible = layersController.IsLayerVisible(layerInfo.layerId);
        stateButtonIcon.sprite = visible ? showIcon : hideIcon;
    }

    private void RefreshLinkButton(bool linked)
    {
        linkButtonIcon.sprite = linked ? unlinkIcon : linkIcon;
    }

    // ============================= Mask Preview API (para futuro) =============================

    public void OnDrop(PointerEventData eventData)
    {
        // 1. Validar que hay algo dragueado
        if (eventData.pointerDrag == null) return;
        
        // 2. Obtener el controlador de drag
        var drag = eventData.pointerDrag.GetComponent<UILayerMaskedDrag>();
        if (drag == null) return;

        // 3. Identificar el LayerItem de origen
        var sourceLayerItem = drag.OwnerLayerItem;
        if (sourceLayerItem == null) return;
        
        // 4. Evitar auto-drop
        if (sourceLayerItem == this) return;

        // 5. Obtener el masked container real desde el origen
        GameObject container = sourceLayerItem.GetSecondaryContainer();
        if (container == null) return;

        // 6. Si ESTE LayerItem ya tenía uno, lo apaga (Quitar luego)
        if (secondaryContainer != null)
        {
            secondaryContainer.SetActive(false); // Cambiar por destroy luego.
            secondaryContainer = null;
        }

        // 7. Transferimos ownership (Actualizamos la referencia al contenedor con el contenedor nuevo.)
        secondaryContainer = container;

        // 8. Reparent + orden
        secondaryContainer.transform.SetParent(leftContainer, false);
        secondaryContainer.transform.SetSiblingIndex(1); // siempre debajo del primary
        secondaryContainer.SetActive(true);

        // 9. El origen pierde referencia
        sourceLayerItem.ClearSecondaryState();

        // 10. Estado UI del destino
        SetMaskedState(true);

        // 11. Confirmamos drop exitoso
        drag.MarkDropped();
    }
    
    public GameObject GetSecondaryContainer()
    {
        return secondaryContainer;
    }
    public void ClearSecondaryState()
    {
        secondaryContainer = null;
        hasMaskedGroup = false;
        RefreshLinkButton(true);
    }

    public void SetMaskedState(bool masked)
    {
        isMasked = masked;

        if (secondaryContainer != null)
        {
            secondaryContainer.SetActive(masked);
            secondaryContainer.transform.SetSiblingIndex(1);
        }

        // Botón de link / unlink
        if (linkButton != null)
        {
            var icon = linkButton.GetComponent<Image>();
            if (icon != null)
                icon.sprite = masked ? unlinkIcon : linkIcon;
        }

        // Estado del botón principal (show / hide / lock)
        UpdateStateIcon();
    }
    private void UpdateStateIcon()
    {
        if (stateButton == null) return;

        var icon = stateButton.GetComponent<Image>();
        if (icon == null) return;

        if (isLocked)
            icon.sprite = lockIcon;
        else
            icon.sprite = isVisible ? showIcon : hideIcon;
    }
    public void ShowMaskedPreview(RenderTexture texture)
    {
        secondaryPreview.texture = texture;
        secondaryContainer.SetActive(true);
        hasMaskedGroup = true;
        RefreshLinkButton(true);
    }
    public void HideMaskedPreview()
    {
        secondaryPreview.texture = null;
        secondaryContainer.SetActive(false);
        hasMaskedGroup = false;
        RefreshLinkButton(false);
    }
}