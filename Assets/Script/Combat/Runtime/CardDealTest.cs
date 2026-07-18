using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// 测试用：点击按钮随机抽一张牌
    /// </summary>
    public class CardDealTest : MonoBehaviour
    {
        [SerializeField] private Button drawButton;
        [SerializeField] private CardDealAnimator animator;
        [SerializeField] private CardDatabaseService cardDatabase;

        private void Start()
        {
            if (drawButton != null)
                drawButton.onClick.AddListener(OnDrawClicked);
        }

        private void OnDrawClicked()
        {
            if (animator == null || cardDatabase == null)
            {
                Debug.LogWarning("[CardDealTest] Missing references");
                return;
            }

            if (!cardDatabase.IsLoaded)
            {
                Debug.LogWarning("[CardDealTest] CardDatabase not loaded yet");
                return;
            }

            var allCards = cardDatabase.Repository.Cards;
            if (allCards.Count == 0) return;

            // 随机抽一张
            var spec = allCards[Random.Range(0, allCards.Count)];
            animator.DrawCard(spec);
        }
    }
}