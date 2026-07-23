using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace KiKs.Combat
{
    /// <summary>
    /// 玩家攻击动效：监听引擎 DamageApplied 事件（sourceId=player）。
    /// 近战走状态机（冲刺→斩击→返回）；远程走状态机（射击→恢复）；
    /// 魔法走 DOTween 前冲兜底；有 Animator 时走 Animator Trigger。
    /// 挂在玩家立绘上。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class PlayerAttackFeedback : MonoBehaviour
    {
        private enum AttackState
        {
            Idle,
            Dashing,
            Slashing,
            Returning
        }

        [Header("Animator（有动画资源时用）")]
        [Tooltip("拖入 Animator Controller，留空则用 DOTween 兜底")]
        [SerializeField] private Animator animator;
        [Tooltip("攻击动画的 Trigger 参数名")]
        [SerializeField] private string attackTrigger = "Attack";
        [Tooltip("攻击动画的 int 参数：0=近战, 1=远程, 2=魔法（对应卡牌category）")]
        [SerializeField] private string attackTypeParam = "AttackType";

        [Header("近战冲刺")]
        [SerializeField] private Transform meleeDashTarget;
        [SerializeField] private float dashDuration = 0.07f;
        [SerializeField] private float slashDelay = 0.12f;
        [SerializeField] private float returnDuration = 0.25f;
        [SerializeField] private float dashOffsetX = -180f;

        [Header("打击感")]
        [Tooltip("命中瞬间卡帧时间（秒），0=不卡")]
        [SerializeField] private float hitstopDuration = 0.12f;
        [Tooltip("卡帧时玩家缩放，制造冲击感")]
        [SerializeField] private float hitstopScale = 1.15f;
        [Tooltip("卡帧结束后弹回原scale的时间")]
        [SerializeField] private float hitstopReturnTime = 0.18f;
        [Tooltip("屏震强度（Canvas 抖动像素数），0=不震")]
        [SerializeField] private float shakeStrength = 15f;
        [Tooltip("屏震持续秒数")]
        [SerializeField] private float shakeDuration = 0.2f;
        [Tooltip("全屏闪白持续时间（秒），0=不闪")]
        [SerializeField] private float screenFlashDuration = 0.1f;

        [Header("音效")]
        [Tooltip("冲刺音效（冲刺开始时播放）")]
        [SerializeField] private AudioClip dashSfx;
        [Tooltip("命中音效（刀光落下时播放）")]
        [SerializeField] private AudioClip hitSfx;
        [Tooltip("开枪音效")]
        [SerializeField] private AudioClip rangedSfx;
        [SerializeField] private AudioSource audioSource;

        [Header("近战攻击图")]
        [Tooltip("冲刺到达后切换为这张图")]
        [SerializeField] private Sprite meleeAttackSprite;

        [Header("立绘缩放")]
        [Tooltip("近战立绘额外缩放倍率，1=面积比自动计算，>1放大，<1缩小")]
        [SerializeField] private float meleeSpriteScaleMultiplier = 1f;
        [Tooltip("射击立绘额外缩放倍率，1=面积比自动计算，>1放大，<1缩小")]
        [SerializeField] private float rangedSpriteScaleMultiplier = 1.15f;

        [Header("魔法姿态")]
        [Tooltip("悬浮魔法牌/魔手时切换为这张图")]
        [SerializeField] private Sprite magicPoseSprite;
        [Tooltip("魔法立绘额外缩放倍率")]
        [SerializeField] private float magicSpriteScaleMultiplier = 1f;
        [Tooltip("魔法立绘位置偏移")]
        [SerializeField] private Vector2 magicSpriteOffset = Vector2.zero;

        [Header("刀光特效")]
        [SerializeField] private Sprite slashSprite;
        [SerializeField] private Transform vfxParent;

        [Header("远程射击")]
        [Tooltip("开枪时切换为这张图")]
        [SerializeField] private Sprite rangedAttackSprite;
        [Tooltip("开枪后切回原图的延迟")]
        [SerializeField] private float rangedShootDuration = 0.2f;
        [Tooltip("枪口闪光颜色")]
        [SerializeField] private Color muzzleFlashColor = new(1f, 0.9f, 0.3f, 0.8f);
        [Tooltip("枪口闪光大小")]
        [SerializeField] private float muzzleFlashSize = 120f;
        [Tooltip("枪口闪光持续秒数")]
        [SerializeField] private float muzzleFlashDuration = 0.12f;
        [Tooltip("射击立绘位置偏移（修正脚部对齐）")]
        [SerializeField] private Vector2 rangedSpriteOffset = Vector2.zero;

        [Tooltip("近战立绘位置偏移（修正脚部对齐）")]
        [SerializeField] private Vector2 meleeSpriteOffset = Vector2.zero;

        [Header("魔法兜底前冲")]
        [SerializeField] private float lungeDistance = 80f;
        [SerializeField] private float lungeDuration = 0.12f;
        [SerializeField] private float attackScale = 1.1f;
        [SerializeField] private float scaleDuration = 0.12f;

        [Header("引擎引用")]
        [SerializeField] private BattleController battleController;

        private RectTransform _rect;
        private UnityEngine.UI.Image _image;
        private Sprite _originSprite;
        private Vector2 _originPos;
        private Vector3 _originScale;
        private float _originVisualWidth;
        private float _originVisualHeight;
        private RectTransform _canvasRect;
        private Vector2 _canvasOriginPos;
        private AttackState _state = AttackState.Idle;
        private Coroutine _attackRoutine;
        private Sequence _tweenSeq;
        private bool _useAnimator;
        private bool _isRangedPose;
        private bool _isMagicPose;

        /// <summary>攻击流程进行中时为 true，防止重复触发</summary>
        public bool IsBusy => _state != AttackState.Idle;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<UnityEngine.UI.Image>();
            _originSprite = _image.sprite;
            _originPos = _rect.anchoredPosition;
            _originScale = _rect.localScale;
            _originVisualWidth = _rect.sizeDelta.x * _originScale.x;
            _originVisualHeight = _rect.sizeDelta.y * _originScale.y;
            _canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
            _canvasOriginPos = _canvasRect.anchoredPosition;
            if (audioSource == null)
                audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            _useAnimator = animator != null && animator.runtimeAnimatorController != null;
        }

        private void Start()
        {
            if (animator == null) animator = GetComponent<Animator>();
            _useAnimator = animator != null && animator.runtimeAnimatorController != null;

            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();
            if (battleController != null)
                battleController.CombatEventRaised += OnCombatEvent;
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
            StopAttack();
        }

        /// <summary>切换到远程姿态（悬浮远程牌时调用，保持住）</summary>
        public void SwitchToRangedPose()
        {
            if (IsBusy || _isRangedPose) return;
            // 先从魔法姿态恢复
            if (_isMagicPose) RestoreSprite();
            _isMagicPose = false;
            _isRangedPose = true;
            if (rangedAttackSprite != null)
            {
                SwapSprite(rangedAttackSprite, rangedSpriteScaleMultiplier);
                _rect.anchoredPosition += rangedSpriteOffset;
            }
        }

        /// <summary>切换到魔法姿态（悬浮魔法牌/魔手时调用，保持住）</summary>
        public void SwitchToMagicPose()
        {
            if (IsBusy || _isMagicPose) return;
            // 先从远程姿态恢复
            if (_isRangedPose) RestoreSprite();
            _isRangedPose = false;
            _isMagicPose = true;
            if (magicPoseSprite != null)
            {
                SwapSprite(magicPoseSprite, magicSpriteScaleMultiplier);
                _rect.anchoredPosition += magicSpriteOffset;
            }
        }

        /// <summary>切换回近战姿态（悬浮近战牌时调用）</summary>
        public void SwitchToMeleePose()
        {
            if (IsBusy || (!_isRangedPose && !_isMagicPose)) return;
            _isRangedPose = false;
            _isMagicPose = false;
            RestoreSprite();
        }

        /// <summary>单发射击特效（多段射击时每发调用，不切立绘不走状态机）</summary>
        public void PlayRangedSingleShot()
        {
            if (IsBusy) return;
            SpawnMuzzleFlash();
            TriggerEnemyHit();
            PlaySfx(rangedSfx);
            ShakeCanvas();
        }

        private void OnCombatEvent(CombatEvent evt)
        {
            if (evt.Type != CombatEventType.DamageApplied) return;
            if (string.IsNullOrEmpty(evt.SourceId)) return;
            if (battleController == null || !battleController.IsInitialized) return;
            if (evt.SourceId != battleController.State.Player.Id) return;

            int attackType = ResolveAttackType(evt.CardInstanceId);
            PlayAttack(attackType);
        }

        private int ResolveAttackType(string cardInstanceId)
        {
            if (string.IsNullOrEmpty(cardInstanceId)) return 0;

            // 先尝试从手牌查 category
            var card = battleController.State.Deck.FindInHand(cardInstanceId);
            if (card != null)
                return card.Spec.Category switch
                {
                    "guns" or "ranged" => 1,
                    "magic" => 2,
                    _ => 0
                };

            // 卡牌打出后已不在手牌，用卡牌 ID 前缀兜底
            var id = cardInstanceId.Split('#')[0];
            if (id.StartsWith("ranged_") || id.StartsWith("gun_")) return 1;
            if (id.StartsWith("magic_")) return 2;
            return 0;
        }

        /// <param name="attackType">0=近战, 1=远程, 2=魔法</param>
        public void PlayAttack(int attackType = 0)
        {
            // Animator 路径：交给动画控制器
            if (_useAnimator)
            {
                if (!string.IsNullOrEmpty(attackTypeParam))
                    animator.SetInteger(attackTypeParam, attackType);
                animator.SetTrigger(attackTrigger);
                return;
            }

            // 近战路径：状态机
            if (attackType == 0 && meleeDashTarget != null)
            {
                StopAttack();
                _attackRoutine = StartCoroutine(MeleeAttackRoutine());
                return;
            }

            // 远程路径：状态机
            if (attackType == 1)
            {
                StopAttack();
                _attackRoutine = StartCoroutine(RangedAttackRoutine());
                return;
            }

            // 魔法路径：简单前冲
            PlayLungeFallback();
        }

        /// <summary>近战攻击状态机：Dashing → Slashing → Returning → Idle</summary>
        private IEnumerator MeleeAttackRoutine()
        {
            // --- Dashing：冲刺到敌人面前 ---
            _state = AttackState.Dashing;
            _rect.anchoredPosition = _originPos;
            _rect.localScale = _originScale;
            PlaySfx(dashSfx);

            var dashTarget = CalcDashTargetLocal();

            _tweenSeq = DOTween.Sequence();
            _tweenSeq.Append(_rect.DOLocalMove(dashTarget, dashDuration).SetEase(Ease.OutCubic));
            yield return _tweenSeq.WaitForCompletion();

            // --- Slashing：切换攻击图 + 卡帧 + 屏震 + 全屏闪白 + 刀光 + 敌人受击 ---
            _state = AttackState.Slashing;
            if (meleeAttackSprite != null)
            {
                SwapSprite(meleeAttackSprite, meleeSpriteScaleMultiplier);
                _rect.anchoredPosition += meleeSpriteOffset;
            }
            SpawnSlash();
            TriggerEnemyHit();
            FlashScreen();
            PlaySfx(hitSfx);

            // 卡肉：冻结时间 + 玩家放大 + 屏震
            Time.timeScale = 0f;
            _rect.localScale = _rect.localScale * hitstopScale;
            ShakeCanvas();
            yield return new WaitForSecondsRealtime(hitstopDuration);

            // 解冻：弹回 scale
            Time.timeScale = 1f;
            var baseScale = GetScaleForSprite(meleeAttackSprite, meleeSpriteScaleMultiplier);
            _rect.DOScale(baseScale, hitstopReturnTime).SetEase(Ease.OutQuart);
            yield return new WaitForSeconds(slashDelay + 0.1f);

            // --- Returning：换回原图 + 返回原位 ---
            _state = AttackState.Returning;
            RestoreSprite();

            _tweenSeq = DOTween.Sequence();
            _tweenSeq.Append(_rect.DOLocalMove(_originPos, returnDuration).SetEase(Ease.OutQuart));
            yield return _tweenSeq.WaitForCompletion();

            // --- Idle：恢复 ---
            _rect.anchoredPosition = _originPos;
            _state = AttackState.Idle;
            _attackRoutine = null;
        }

        /// <summary>远程攻击状态机：Shooting → Idle（立绘由姿态状态管理）</summary>
        private IEnumerator RangedAttackRoutine()
        {
            // --- Shooting：确保射击立绘 + 枪口闪光 + 敌人受击 ---
            _state = AttackState.Slashing;
            var wasRangedPose = _isRangedPose;

            if (rangedAttackSprite != null && _image.sprite != rangedAttackSprite)
            {
                SwapSprite(rangedAttackSprite, rangedSpriteScaleMultiplier);
                _rect.anchoredPosition += rangedSpriteOffset;
            }

            SpawnMuzzleFlash();
            TriggerEnemyHit();
            PlaySfx(rangedSfx);

            // 短暂卡帧（比近战轻）
            Time.timeScale = 0f;
            ShakeCanvas();
            yield return new WaitForSecondsRealtime(hitstopDuration * 0.5f);

            Time.timeScale = 1f;
            yield return new WaitForSeconds(rangedShootDuration);

            // --- Idle：姿态保持，不自动恢复 ---
            _state = AttackState.Idle;
            _attackRoutine = null;
        }

        private void SpawnMuzzleFlash()
        {
            if (vfxParent == null)
                vfxParent = transform.root;

            var muzzlePos = _rect.position;
            muzzlePos.x += _rect.rect.width * _rect.localScale.x * 0.4f;
            muzzlePos.y += _rect.rect.height * _rect.localScale.y * 0.1f;

            var obj = new GameObject("MuzzleFlash");
            obj.transform.SetParent(vfxParent, true);
            obj.transform.position = muzzlePos;

            var img = obj.AddComponent<UnityEngine.UI.Image>();
            img.color = muzzleFlashColor;
            img.raycastTarget = false;

            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(muzzleFlashSize, muzzleFlashSize);

            var seq = DOTween.Sequence();
            seq.Append(obj.transform.DOScale(0.5f, 0f));
            seq.Join(obj.transform.DOScale(1.5f, muzzleFlashDuration * 0.4f).SetEase(Ease.OutQuart));
            seq.Join(obj.transform.DOLocalRotate(new Vector3(0, 0, 45f), muzzleFlashDuration)
                .SetEase(Ease.OutQuart));
            seq.Join(DOTween.To(() => img.color, c => img.color = c,
                new Color(muzzleFlashColor.r, muzzleFlashColor.g, muzzleFlashColor.b, 0f),
                muzzleFlashDuration).SetEase(Ease.InQuart));
            seq.OnComplete(() => Destroy(obj));
        }

        private Vector2 CalcDashTargetLocal()
        {
            var targetWorld = meleeDashTarget.position;
            targetWorld.x += dashOffsetX;
            var targetLocal = _rect.parent.InverseTransformPoint(targetWorld);
            targetLocal.y = _originPos.y;
            return targetLocal;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }

        private static Vector2 GetSpriteSize(Sprite sprite)
        {
            if (sprite == null) return Vector2.one;
            var rect = sprite.rect;
            return new Vector2(rect.width, rect.height);
        }

        private Vector3 GetScaleForSprite(Sprite sprite, float scaleMultiplier = 1f)
        {
            if (sprite == null || _originVisualWidth <= 0f) return _originScale;
            var size = GetSpriteSize(sprite);
            var targetArea = _originVisualWidth * _originVisualHeight;
            var spriteArea = size.x * size.y;
            var uniformScale = Mathf.Sqrt(targetArea / spriteArea) * scaleMultiplier;
            return new Vector3(uniformScale, uniformScale, 1f);
        }

        /// <summary>切换 sprite：重设 sizeDelta 为自然尺寸 + 均匀缩放，避免变形</summary>
        private void SwapSprite(Sprite newSprite, float scaleMultiplier = 1f)
        {
            if (newSprite == null) return;
            _image.sprite = newSprite;

            var newSize = GetSpriteSize(newSprite);
            if (newSize.x <= 0f || newSize.y <= 0f) return;

            _rect.sizeDelta = newSize;

            var targetArea = _originVisualWidth * _originVisualHeight;
            var spriteArea = newSize.x * newSize.y;
            var uniformScale = Mathf.Sqrt(targetArea / spriteArea) * scaleMultiplier;
            _rect.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }

        /// <summary>恢复原图、原始 sizeDelta、原始 scale 和原始位置</summary>
        private void RestoreSprite()
        {
            _image.sprite = _originSprite;
            _rect.sizeDelta = new Vector2(
                _originVisualWidth / _originScale.x,
                _originVisualHeight / _originScale.y);
            _rect.localScale = _originScale;
            _rect.anchoredPosition = _originPos;
        }

        private void TriggerEnemyHit()
        {
            if (meleeDashTarget == null) return;
            var hitFeedback = meleeDashTarget.GetComponent<EnemyHitFeedbackNew>();
            if (hitFeedback != null)
                hitFeedback.PlayHit();
        }

        private void FlashScreen()
        {
            if (screenFlashDuration <= 0f) return;

            var flashObj = new GameObject("ImpactFlash");
            flashObj.transform.SetParent(_canvasRect, false);

            var impactPos = meleeDashTarget != null ? meleeDashTarget.position : Vector3.zero;
            var localPos = _canvasRect.InverseTransformPoint(impactPos);

            var rt = flashObj.AddComponent<RectTransform>();
            rt.anchoredPosition = localPos;

            var image = flashObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 1f, 1f, 0.5f);
            image.raycastTarget = false;

            var startSize = 100f;
            var endSize = 800f;
            rt.sizeDelta = new Vector2(startSize, startSize);

            var seq = DOTween.Sequence();
            seq.Join(DOTween.To(() => rt.sizeDelta,
                v => rt.sizeDelta = v, new Vector2(endSize, endSize), screenFlashDuration)
                .SetEase(Ease.OutQuart));
            seq.Join(DOTween.To(() => image.color, c => image.color = c,
                new Color(1f, 1f, 1f, 0f), screenFlashDuration)
                .SetEase(Ease.InQuart));
            seq.OnComplete(() => Destroy(flashObj));
        }

        private void ShakeCanvas()
        {
            if (_canvasRect == null || shakeStrength <= 0f) return;
            _canvasRect.DOKill();
            _canvasRect.anchoredPosition = _canvasOriginPos;
            _canvasRect.DOShakePosition(shakeDuration, shakeStrength, 20, 90f, false, true)
                .OnComplete(() => _canvasRect.anchoredPosition = _canvasOriginPos);
        }

        private void SpawnSlash()
        {
            if (vfxParent == null)
                vfxParent = transform.root;

            var slashObj = new GameObject("SlashVFX");
            slashObj.transform.SetParent(vfxParent, true);
            var slashPos = meleeDashTarget.position;
            slashPos.x += dashOffsetX;
            slashObj.transform.position = slashPos;

            var image = slashObj.AddComponent<UnityEngine.UI.Image>();
            if (slashSprite != null) image.sprite = slashSprite;
            image.raycastTarget = false;

            var rt = slashObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 200);

            var slash = slashObj.AddComponent<SlashVFX>();
            slash.SetDirection(135f, -45f);
            slash.SetColorGradient(Color.white, new Color(1f, 0.2f, 0.2f, 1f));
            slash.Play();

            SpawnDamagePopup(slashPos);
        }

        private void SpawnDamagePopup(Vector3 pos)
        {
            pos.x -= 80f;
            pos.y += 80f;

            var obj = new GameObject("DamagePopup");
            obj.transform.SetParent(vfxParent, true);
            obj.transform.position = pos;

            var img = obj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(1f, 1f, 1f, 0.8f);
            img.raycastTarget = false;

            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 250);

            var seq = DOTween.Sequence();
            seq.Append(obj.transform.DOScale(0.3f, 0f));
            seq.Join(obj.transform.DOScale(1.6f, 0.18f).SetEase(Ease.OutQuart));
            seq.Join(obj.transform.DOLocalRotate(new Vector3(0, 0, 135f), 0f));
            seq.Join(obj.transform.DOLocalRotate(new Vector3(0, 0, -45f), 0.18f).SetEase(Ease.OutQuart));
            seq.Join(DOTween.To(() => img.color, c => img.color = c,
                new Color(1f, 1f, 1f, 0f), 0.18f).SetEase(Ease.InQuart));
            seq.OnComplete(() => Destroy(obj));
        }

        /// <summary>魔法兜底：原地前冲 + 回弹</summary>
        private void PlayLungeFallback()
        {
            StopAttack();
            _rect.anchoredPosition = _originPos;
            _rect.localScale = _originScale;

            _tweenSeq = DOTween.Sequence();
            _tweenSeq.Append(_rect.DOLocalMoveX(_originPos.x + lungeDistance, lungeDuration)
                .SetEase(Ease.OutQuad));
            _tweenSeq.Join(_rect.DOScale(_originScale * attackScale, scaleDuration)
                .SetEase(Ease.OutQuad));
            _tweenSeq.Append(_rect.DOLocalMoveX(_originPos.x, returnDuration)
                .SetEase(Ease.OutQuart));
            _tweenSeq.Join(_rect.DOScale(_originScale, returnDuration)
                .SetEase(Ease.OutQuart));
        }

        private void StopAttack()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }
            Time.timeScale = 1f;
            _tweenSeq?.Kill();
            _tweenSeq = null;
            if (_canvasRect != null)
                _canvasRect.DOKill();
            if (_image != null && _originSprite != null)
                RestoreSprite();
            _state = AttackState.Idle;
        }

        /// <summary>动画播放完毕后由 Animation Event 调用，恢复位置</summary>
        public void OnAttackAnimationEnd()
        {
            _rect.anchoredPosition = _originPos;
            _rect.localScale = _originScale;
        }
    }
}
