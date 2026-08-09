using System.Collections.Generic;
using UnityEngine;

namespace KiKs.UI
{
    /// <summary>
    /// 立绘表情差分缓存。按角色名 + 表情名查找对应 Sprite。
    /// 挂在 Canvas 上，DontDestroyOnLoad 跨场景复用。
    /// 表情字段为空或未匹配时返回 defaultSprite。
    /// </summary>
    public class PortraitExpressionCache : MonoBehaviour
    {
        public static PortraitExpressionCache Instance { get; private set; }

        [System.Serializable]
        public class ExpressionEntry
        {
            public string expression;
            public Sprite sprite;
        }

        [System.Serializable]
        public class CharacterExpressions
        {
            public string characterName;
            public Sprite defaultSprite;
            public List<ExpressionEntry> expressions = new();
        }

        [SerializeField] private List<CharacterExpressions> characters = new();

        private readonly Dictionary<string, CharacterExpressions> _cache = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _cache.Clear();
            foreach (var ch in characters)
            {
                if (ch != null && !string.IsNullOrEmpty(ch.characterName))
                    _cache[ch.characterName] = ch;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 查找角色立绘。expression 为空或未匹配时返回 defaultSprite。
        /// 角色不存在时返回 null。
        /// </summary>
        public Sprite GetSprite(string characterName, string expression)
        {
            if (string.IsNullOrEmpty(characterName)) return null;
            if (!_cache.TryGetValue(characterName, out var ch)) return null;

            if (string.IsNullOrEmpty(expression))
                return ch.defaultSprite;

            foreach (var entry in ch.expressions)
            {
                if (entry != null && entry.expression == expression)
                    return entry.sprite;
            }

            return ch.defaultSprite;
        }
    }
}
