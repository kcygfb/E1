using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MenuViewer : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] menuPages;
    [SerializeField] private Sprite nextPageSprite;

    [Header("UI References")]
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject viewerPanel;
    [SerializeField] private Image menuImage;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button closeButton;

    [Header("Button Animation")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private float pullDownY = -8f;
    [SerializeField] private float pullDownDuration = 0.12f;

    private int _currentPage;
    private Vector2 _buttonOriginPos;

    private void Awake()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(Open);
            _buttonOriginPos = openButton.GetComponent<RectTransform>().anchoredPosition;
            var trigger = openButton.gameObject.GetComponent<MenuButtonAnim>();
            if (trigger == null) trigger = openButton.gameObject.AddComponent<MenuButtonAnim>();
            trigger.Init(openButton.transform, _buttonOriginPos, hoverScale, hoverDuration, pullDownY, pullDownDuration);
        }
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (viewerPanel != null)
            viewerPanel.SetActive(false);
    }

    private void Open()
    {
        if (viewerPanel == null || menuPages == null || menuPages.Length == 0) return;
        _currentPage = 0;
        ShowPage();
        viewerPanel.SetActive(true);
    }

    private void NextPage()
    {
        if (menuPages == null || menuPages.Length == 0) return;
        _currentPage = (_currentPage + 1) % menuPages.Length;
        ShowPage();
    }

    private void ShowPage()
    {
        if (menuImage != null)
            menuImage.sprite = menuPages[_currentPage];
    }

    public void Close()
    {
        if (viewerPanel != null)
            viewerPanel.SetActive(false);
    }
}

public class MenuButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Transform _target;
    private Vector2 _originPos;
    private float _hoverScale;
    private float _hoverDuration;
    private float _pullDownY;
    private float _pullDownDuration;

    public void Init(Transform target, Vector2 originPos, float hoverScale, float hoverDuration, float pullDownY, float pullDownDuration)
    {
        _target = target;
        _originPos = originPos;
        _hoverScale = hoverScale;
        _hoverDuration = hoverDuration;
        _pullDownY = pullDownY;
        _pullDownDuration = pullDownDuration;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_target == null) return;
        _target.DOScale(_hoverScale, _hoverDuration).SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_target == null) return;
        _target.DOScale(1f, _hoverDuration).SetEase(Ease.OutQuad);
        ((RectTransform)_target).DOAnchorPos(_originPos, _pullDownDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_target == null) return;
        ((RectTransform)_target).DOAnchorPos(_originPos + new Vector2(0, _pullDownY), _pullDownDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_target == null) return;
        ((RectTransform)_target).DOAnchorPos(_originPos, _pullDownDuration).SetEase(Ease.OutBack);
    }
}
