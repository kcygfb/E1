using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>战斗中角色旁的 buff icon 显示。每帧从 CombatantState 读取，变化时重建 UI。</summary>
    public class BuffDisplayUI : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private BattleController battleController;
        [SerializeField] private bool isPlayer;

        [Header("Buff Icons")]
        [SerializeField] private Sprite bleedIcon;
        [SerializeField] private Sprite poisonIcon;
        [SerializeField] private Sprite stunIcon;
        [SerializeField] private Sprite nullifyIcon;

        [Header("Layout")]
        [SerializeField] private Vector2 iconSize = new(40, 40);
        [SerializeField] private float spacing = 6f;

        private readonly struct BuffSnapshot
        {
            public readonly int Bleed;
            public readonly int Poison;
            public readonly int Stun;
            public readonly int Nullify;

            public BuffSnapshot(int bleed, int poison, int stun, int nullify)
            {
                Bleed = bleed; Poison = poison; Stun = stun;
                Nullify = nullify;
            }

            public bool Equals(BuffSnapshot other) =>
                Bleed == other.Bleed && Poison == other.Poison && Stun == other.Stun &&
                Nullify == other.Nullify;
        }

        private BuffSnapshot _lastSnapshot;
        private bool _initialized;
        private HorizontalLayoutGroup _layout;
        private readonly Dictionary<string, GameObject> _iconObjects = new();

        private void Awake()
        {
            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();
            CreateLayout();
        }

        private void CreateLayout()
        {
            _layout = GetComponent<HorizontalLayoutGroup>();
            if (_layout == null)
                _layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            _layout.spacing = spacing;
            _layout.childAlignment = TextAnchor.MiddleCenter;
            _layout.childControlWidth = false;
            _layout.childControlHeight = false;
            _layout.childForceExpandWidth = false;
            _layout.childForceExpandHeight = false;
        }

        private void Update()
        {
            if (battleController == null) return;
            var state = battleController.GetEngineInternal()?.State;
            if (state == null) return;

            var combatant = isPlayer ? state.Player : state.FindFirstLivingEnemy();
            if (combatant == null) return;

            var snapshot = new BuffSnapshot(
                combatant.BleedStacks,
                combatant.PoisonStacks,
                combatant.StunTurns,
                combatant.NullifyAttackCharges
            );

            if (_initialized && snapshot.Equals(_lastSnapshot)) return;
            _lastSnapshot = snapshot;
            _initialized = true;
            RefreshUI(snapshot);
        }

        private void RefreshUI(in BuffSnapshot snap)
        {
            UpdateIcon("Bleed", snap.Bleed, bleedIcon);
            UpdateIcon("Poison", snap.Poison, poisonIcon);
            UpdateIcon("Stun", snap.Stun, stunIcon);
            UpdateIcon("Nullify", snap.Nullify, nullifyIcon);
        }

        private void UpdateIcon(string key, int value, Sprite icon)
        {
            bool shouldShow = value > 0;

            if (!_iconObjects.TryGetValue(key, out var go))
            {
                if (!shouldShow) return;
                go = CreateIconObject(key, icon);
                _iconObjects[key] = go;
            }

            go.SetActive(shouldShow);
            if (!shouldShow) return;

            // Update number text
            var text = go.transform.Find("Count")?.GetComponent<Text>();
            if (text != null)
                text.text = value.ToString();
        }

        private GameObject CreateIconObject(string name, Sprite icon)
        {
            var go = new GameObject("Buff_" + name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = iconSize;

            var img = go.GetComponent<Image>();
            if (icon != null)
            {
                img.sprite = icon;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            img.raycastTarget = false;

            // Count text
            var textGO = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.2f, 0.2f);
            textRT.anchorMax = new Vector2(0.9f, 0.9f);
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var text = textGO.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = (int)(iconSize.x * 0.25f);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = "0";
            // 黑色描边，保证在亮色 icon 上也可读
            var outline = textGO.GetComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return go;
        }
    }
}
