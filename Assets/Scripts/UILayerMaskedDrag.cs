using UnityEngine;
using UnityEngine.EventSystems;

public class UILayerMaskedDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]

    private Canvas rootCanvas;
    private RectTransform rectTransform;
    private Vector2 startAnchoredPosition;
    private Transform startParent;
    private int startSiblingIndex;
    private bool droppedSuccessfully;
    
    private CanvasGroup canvasGroup;

    [Header("Debug")]
    [SerializeField] private Transform ownerLayerItemObj;
    public UILayerItem OwnerLayerItem { get; private set; }
    public void SetOwner(UILayerItem owner)
    {
        OwnerLayerItem = owner;
        ownerLayerItemObj = owner.transform;
    }

    private void Awake()
    {
        SetOwner(GetComponentInParent<UILayerItem>());

        rootCanvas = GetComponentInParent<Canvas>();
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        droppedSuccessfully = false;

        startParent = transform.parent;
        startSiblingIndex = transform.GetSiblingIndex();
        startAnchoredPosition = rectTransform.anchoredPosition;

        canvasGroup.blocksRaycasts = false; // 🔥

        transform.SetParent(rootCanvas.transform, true);
    }
    public void OnDrag(PointerEventData eventData) => rectTransform.position = eventData.position;
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true; // RESTAURAR
        if (!droppedSuccessfully) RestoreToOrigin();
    }

    public void MarkDropped() => droppedSuccessfully = true;
    private void RestoreToOrigin()
    {
        transform.SetParent(startParent, true);
        transform.SetSiblingIndex(startSiblingIndex);
        rectTransform.anchoredPosition = startAnchoredPosition;
    }

}
