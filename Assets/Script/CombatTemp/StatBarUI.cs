using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KiKs.Combat
{
    /// <summary>
    /// 血条/韧性条 UI：监听 EnemyStats 事件，自动更新 Fill 和文字。
    /// 挂到 EnemyHpBar 或 EnemyToughnessBar 上即可。
    /// </summary>
    public class StatBarUI : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private EnemyStats enemyStats;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text displayText;

        [Header("类型")]
        [SerializeField] private bool trackHP = true;

        private void Awake()
        {
            // 未指定时自动查找
            if (enemyStats == null)
                enemyStats = GetComponentInParent<EnemyStats>();
            if (enemyStats == null)
                enemyStats = FindFirstObjectByType<EnemyStats>();

            // 未指定时自动查找名为 Fill 的子物体
            if (fillImage == null)
            {
                var fill = transform.Find("Fill");
                if (fill != null)
                    fillImage = fill.GetComponent<Image>();
            }

            // 未指定时自动查找名为 xxxText 的子物体
            if (displayText == null)
            {
                displayText = GetComponentInChildren<TMP_Text>();
            }

            // 确保 Image 是 Filled 类型
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
            }
        }

        private void OnEnable()
        {
            if (enemyStats == null) return;

            if (trackHP)
                enemyStats.OnHPChanged.AddListener(UpdateBar);
            else
                enemyStats.OnToughnessChanged.AddListener(UpdateBar);
        }

        private void OnDisable()
        {
            if (enemyStats == null) return;

            if (trackHP)
                enemyStats.OnHPChanged.RemoveListener(UpdateBar);
            else
                enemyStats.OnToughnessChanged.RemoveListener(UpdateBar);
        }

        private void UpdateBar(int current, int max)
        {
            if (fillImage != null)
                fillImage.fillAmount = max > 0 ? (float)current / max : 0f;

            if (displayText != null)
                displayText.text = $"{current} / {max}";
        }
    }
}
