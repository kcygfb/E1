using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace KiKs.Combat
{
    /// <summary>
    /// 敌人意图卡牌的悬停放大效果。
    /// 与玩家手牌的 <see cref="KiKs.UI.CardInteraction"/> 相互独立：
    /// 仅负责放大/还原，不含拖拽、点击、斜切、高光等玩家专属逻辑。
    /// 由 <see cref="EnemyCardPresenter"/> 在生成敌人卡牌时调用 <see cref="Initialize"/> 完成配置。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EnemyCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("悬停放大")]
        [Tooltip("悬停时的放大倍率（基于敌人卡牌的基础缩放）")]
        [SerializeField] private float hoverScaleMultiplier = 1.5f;
        [Tooltip("悬停上移的像素距离")]
        [SerializeField] private float hoverLiftY = 10f;
        [Tooltip("放大/还原动画时长（秒）")]
        [SerializeField] private float hoverDuration = 0.15f;

        private RectTransform _rect;
        private Vector3 _baseScale;
        private Vector2 _basePos;
        private bool _initialized;
        private bool _hovering;
        private int _baseSiblingIndex;
        private Tween _tween;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 由 <see cref="EnemyCardPresenter"/> 在敌人卡牌入场动画结束后调用，
        /// 记录当前缩放/位置作为悬停放大的基准值。
        /// </summary>
        public void Initialize()
        {
            _baseScale = _rect.localScale;
            _basePos = _rect.anchoredPosition;
            _initialized = true;
        }

        /// <summary>手牌重新排布后同步基准位置（由 <see cref="EnemyCardPresenter.ArrangeEnemyHand"/> 调用）。</summary>
        public void SetBasePosition(Vector2 position)
        {
            _basePos = position;
            // 若当前处于悬停状态，让卡牌平滑过渡到新基准位置（保持放大倍率）
            if (_hovering)
            {
                _tween?.Kill();
                _rect.DOAnchorPosY(position.y + hoverLiftY, hoverDuration).SetEase(Ease.OutQuint);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_initialized || _hovering) return;
            _hovering = true;

            // 悬停时置顶渲染，避免放大后被后面的牌挡住
            _baseSiblingIndex = _rect.GetSiblingIndex();
            _rect.SetAsLastSibling();

            _tween?.Kill();
            var seq = DOTween.Sequence();
            seq.Join(_rect.DOScale(_baseScale * hoverScaleMultiplier, hoverDuration).SetEase(Ease.OutQuint));
            seq.Join(_rect.DOAnchorPosY(_basePos.y + hoverLiftY, hoverDuration).SetEase(Ease.OutQuint));
            _tween = seq;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_initialized || !_hovering) return;
            _hovering = false;

            // 离开悬停后恢复原渲染层级
            RestoreSiblingIndex();

            _tween?.Kill();
            var seq = DOTween.Sequence();
            seq.Join(_rect.DOScale(_baseScale, hoverDuration).SetEase(Ease.OutQuint));
            seq.Join(_rect.DOAnchorPosY(_basePos.y, hoverDuration).SetEase(Ease.OutQuint));
            _tween = seq;
        }

        /// <summary>恢复原兄弟层级；若期间手牌数量变化（有牌被打出/弃掉），则按基准位置重算应处层级。</summary>
        private void RestoreSiblingIndex()
        {
            var parent = _rect.parent;
            if (parent == null) return;
            int clamped = Mathf.Min(_baseSiblingIndex, parent.childCount - 1);
            _rect.SetSiblingIndex(clamped);
        }

        private void OnDisable()
        {
            // 若禁用/销毁时仍处于悬停置顶状态，先恢复层级，避免打乱手牌渲染顺序
            if (_hovering) RestoreSiblingIndex();
            _hovering = false;
            _tween?.Kill();
            _tween = null;
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }
    }
}
