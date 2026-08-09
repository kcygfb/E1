using System.Collections.Generic;
using System.Text.RegularExpressions;
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

    [Header("Price Tier Buttons (100/200/300/400)")]
    [SerializeField] private Button[] tierButtons;

    [Header("Tier Colors")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.7f, 0.5f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Button Animation")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private float pullDownY = -8f;
    [SerializeField] private float pullDownDuration = 0.12f;

    // price ranges: 100档=1-99, 200档=100-199, 300档=200-299, 400档=300-399
    private static readonly (int min, int max)[] TierRanges =
    {
        (1, 99),
        (100, 199),
        (200, 299),
        (300, 399),
    };

    private int _currentTier = 0;
    private int _currentPage;
    private readonly List<Sprite> _filtered = new();
    private Vector2 _buttonOriginPos;
    private bool _menuOpenedThisOrder;
    private Sequence _hintSeq;

    private static readonly Regex PriceRegex = new Regex(@"_(\d+)\D*$", RegexOptions.Compiled);

    private void OnEnable()
    {
        GameEvent.On("OrderCreated", OnOrderCreated);
        GameEvent.On("OrderCompleted", OnOrderCompleted);
    }

    private void OnDisable()
    {
        GameEvent.Off("OrderCreated", OnOrderCreated);
        GameEvent.Off("OrderCompleted", OnOrderCompleted);
        StopHintBounce();
    }

    private void OnOrderCreated(object _)
    {
        _menuOpenedThisOrder = false;
        // 进入制作阶段后，菜单按钮可见且未打开过 → 开始提示抖动
        if (openButton != null && openButton.gameObject.activeInHierarchy)
            StartHintBounce();
    }

    private void OnOrderCompleted(object _)
    {
        StopHintBounce();
    }

    private void StartHintBounce()
    {
        StopHintBounce();
        if (openButton == null) return;
        var rt = openButton.GetComponent<RectTransform>();
        if (rt == null) return;

        // 上下大幅抖动两遍，再停顿更久，循环提示
        _hintSeq = DOTween.Sequence();
        _hintSeq.Append(rt.DOAnchorPosY(_buttonOriginPos.y + 22f, 0.35f).SetEase(Ease.OutQuad));
        _hintSeq.Append(rt.DOAnchorPosY(_buttonOriginPos.y - 8f, 0.35f).SetEase(Ease.InQuad));
        _hintSeq.Append(rt.DOAnchorPosY(_buttonOriginPos.y, 0.3f).SetEase(Ease.OutQuad));
        _hintSeq.Append(rt.DOAnchorPosY(_buttonOriginPos.y + 14f, 0.3f).SetEase(Ease.OutQuad));
        _hintSeq.Append(rt.DOAnchorPosY(_buttonOriginPos.y, 0.3f).SetEase(Ease.InQuad));
        _hintSeq.AppendInterval(2.5f);
        _hintSeq.SetLoops(-1, LoopType.Restart);
    }

    private void StopHintBounce()
    {
        if (_hintSeq != null)
        {
            _hintSeq.Kill();
            _hintSeq = null;
        }
        if (openButton != null)
        {
            var rt = openButton.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = _buttonOriginPos;
        }
    }

    private void Awake()
    {
        // MenuImage is display-only and overlaps the price tier buttons.
        // It must not intercept pointer events intended for those buttons.
        if (menuImage != null)
            menuImage.raycastTarget = false;

        if (openButton != null)
        {
            openButton.onClick.AddListener(Toggle);
            _buttonOriginPos = openButton.GetComponent<RectTransform>().anchoredPosition;
            var trigger = openButton.gameObject.GetComponent<MenuButtonAnim>();
            if (trigger == null) trigger = openButton.gameObject.AddComponent<MenuButtonAnim>();
            trigger.Init(openButton.transform, _buttonOriginPos, hoverScale, hoverDuration, pullDownY, pullDownDuration);
        }
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // wire tier buttons
        if (tierButtons != null)
        {
            for (int i = 0; i < tierButtons.Length; i++)
            {
                if (tierButtons[i] == null) continue;
                var idx = i; // capture
                tierButtons[i].onClick.AddListener(() => SelectTier(idx));
            }
        }

        if (viewerPanel != null)
            viewerPanel.SetActive(false);
    }

    private void Toggle()
    {
        if (viewerPanel == null) return;
        if (viewerPanel.activeSelf)
            Close();
        else
            Open();
    }

    private void Open()
    {
        if (viewerPanel == null || menuPages == null || menuPages.Length == 0) return;
        _menuOpenedThisOrder = true;
        StopHintBounce();
        SelectTier(0);
        viewerPanel.SetActive(true);
        if (openButton != null)
            openButton.gameObject.SetActive(false);
    }

    public void SelectTier(int tierIndex)
    {
        if (tierIndex < 0 || tierIndex >= TierRanges.Length) return;

        _currentTier = tierIndex;

        // update button colors
        if (tierButtons != null)
        {
            for (int i = 0; i < tierButtons.Length; i++)
            {
                if (tierButtons[i] == null) continue;
                var img = tierButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == tierIndex) ? selectedColor : normalColor;
            }
        }

        // filter pages by price range; MachineMenu (no price) always included at end
        _filtered.Clear();
        var range = TierRanges[tierIndex];
        var noPricePages = new List<Sprite>();

        if (menuPages != null)
        {
            foreach (var page in menuPages)
            {
                if (page == null) continue;
                int price = ParsePriceFromName(page);
                if (price < 0)
                {
                    noPricePages.Add(page);
                }
                else if (price >= range.min && price <= range.max)
                {
                    _filtered.Add(page);
                }
            }
        }

        // MachineMenu (no price) only in 100档 (tier 0) at the end
        if (tierIndex == 0)
            _filtered.AddRange(noPricePages);

        _currentPage = 0;
        ShowPage();
    }

    private void NextPage()
    {
        if (_filtered.Count == 0) return;
        _currentPage = (_currentPage + 1) % _filtered.Count;
        ShowPage();
    }

    private void ShowPage()
    {
        if (_filtered.Count == 0)
        {
            if (menuImage != null)
                menuImage.sprite = null;
            return;
        }
        if (menuImage != null && _currentPage >= 0 && _currentPage < _filtered.Count)
            menuImage.sprite = _filtered[_currentPage];
    }

    public void Close()
    {
        if (viewerPanel != null)
            viewerPanel.SetActive(false);
        if (openButton != null)
            openButton.gameObject.SetActive(true);
    }

    private static int ParsePriceFromName(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return -1;
        // texture.name is the file name without extension, e.g. "PourOverMenu_20"
        // sprite.name might be "PourOverMenu_0" if texture is still Multiple mode
        var name = sprite.texture.name;
        if (string.IsNullOrEmpty(name)) return -1;
        var match = PriceRegex.Match(name);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var price))
            return price;
        return -1;
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
