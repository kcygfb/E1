using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KiKs.Combat
{
    /// <summary>
    /// 抽牌结果：包含抽到的卡牌、因手牌溢出而直接弃掉的卡牌、以及洗牌次数。
    /// </summary>
    public sealed class DeckDrawResult
    {
        public IReadOnlyList<CardInstance> DrawnCards { get; }
        public IReadOnlyList<CardInstance> OverflowDiscardedCards { get; }
        public int ReshuffleCount { get; }

        internal DeckDrawResult(
            IList<CardInstance> drawnCards,
            IList<CardInstance> overflowDiscardedCards,
            int reshuffleCount)
        {
            DrawnCards = new ReadOnlyCollection<CardInstance>(new List<CardInstance>(drawnCards));
            OverflowDiscardedCards =
                new ReadOnlyCollection<CardInstance>(new List<CardInstance>(overflowDiscardedCards));
            ReshuffleCount = reshuffleCount;
        }
    }

    /// <summary>
    /// Owns the draw pile, hand and discard pile. Drawing automatically reshuffles the discard
    /// pile when the draw pile runs out, including in the middle of a multi-card draw.
    /// </summary>
    /// <remarks>
    /// 牌组状态：管理抽牌堆、手牌和弃牌堆。抽牌时若抽牌堆耗尽，会自动将弃牌堆洗入抽牌堆，
    /// 即使是在一次多张抽牌的中途也会触发重洗。
    /// </remarks>
    public sealed class DeckState
    {
        private readonly List<CardInstance> _drawPile;
        private readonly List<CardInstance> _hand = new List<CardInstance>();
        private readonly List<CardInstance> _discardPile = new List<CardInstance>();
        private readonly Random _random;

        public IReadOnlyList<CardInstance> DrawPile => _drawPile.AsReadOnly();
        public IReadOnlyList<CardInstance> Hand => _hand.AsReadOnly();
        public IReadOnlyList<CardInstance> DiscardPile => _discardPile.AsReadOnly();

        /// <summary>
        /// 构造牌组，使用指定随机种子。若 shuffleAtStart 为 true，开局立即洗牌。
        /// </summary>
        public DeckState(IEnumerable<CardInstance> cards, int randomSeed, bool shuffleAtStart)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            _drawPile = new List<CardInstance>(cards);
            if (_drawPile.Count == 0) throw new ArgumentException("A battle deck cannot be empty.", nameof(cards));

            _random = new Random(randomSeed);
            if (shuffleAtStart) Shuffle(_drawPile);
        }

        /// <summary>
        /// 抽取指定数量的卡牌。若抽牌堆耗尽，自动将弃牌堆洗入。
        /// 超过手牌上限的卡牌会直接进入弃牌堆（记录在 OverflowDiscardedCards 中）。
        /// </summary>
        public DeckDrawResult Draw(int requestedCount, int handLimit)
        {
            if (requestedCount < 0) throw new ArgumentOutOfRangeException(nameof(requestedCount));
            if (handLimit <= 0) throw new ArgumentOutOfRangeException(nameof(handLimit));

            var drawn = new List<CardInstance>();
            var overflow = new List<CardInstance>();
            var reshuffleCount = 0;

            for (var i = 0; i < requestedCount; i++)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0) break;

                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                    reshuffleCount++;
                }

                var topIndex = _drawPile.Count - 1;
                var card = _drawPile[topIndex];
                _drawPile.RemoveAt(topIndex);

                if (_hand.Count < handLimit)
                {
                    _hand.Add(card);
                    drawn.Add(card);
                }
                else
                {
                    _discardPile.Add(card);
                    overflow.Add(card);
                }
            }

            return new DeckDrawResult(drawn, overflow, reshuffleCount);
        }

        /// <summary>
        /// 在手牌中按实例 ID 查找卡牌，未找到返回 null。
        /// </summary>
        public CardInstance FindInHand(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) return null;
            return _hand.Find(card => card.InstanceId == instanceId);
        }

        /// <summary>
        /// 从手牌中弃掉指定卡牌（按实例 ID 匹配），被弃的卡牌移入弃牌堆。
        /// 成功返回 true，卡牌不在手牌中返回 false。
        /// </summary>
        public bool DiscardFromHand(string instanceId, out CardInstance discardedCard)
        {
            discardedCard = FindInHand(instanceId);
            if (discardedCard == null) return false;

            _hand.Remove(discardedCard);
            _discardPile.Add(discardedCard);
            return true;
        }

        /// <summary>
        /// 弃掉全部手牌，将其移入弃牌堆。返回被弃掉的卡牌列表。
        /// </summary>
        public IReadOnlyList<CardInstance> DiscardHand()
        {
            var discarded = new List<CardInstance>(_hand);
            _discardPile.AddRange(_hand);
            _hand.Clear();
            return discarded.AsReadOnly();
        }

        /// <summary>
        /// Fisher-Yates 洗牌算法：从末尾向前遍历，每次与前面的随机位置交换。
        /// </summary>
        private void Shuffle(List<CardInstance> cards)
        {
            for (var i = cards.Count - 1; i > 0; i--)
            {
                var swapIndex = _random.Next(i + 1);
                var temp = cards[i];
                cards[i] = cards[swapIndex];
                cards[swapIndex] = temp;
            }
        }
    }
}
