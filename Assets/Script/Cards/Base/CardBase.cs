using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    public abstract class CardBase : ScriptableObject
    {
        [SerializeField] private Sprite artwork;
        [SerializeField, TextArea] private string description;

        public abstract string CardId { get; }
        public abstract string CardName { get; }
        public abstract CardCategory Category { get; }
        public abstract CardFamily Family { get; }
        public abstract CardTargetType TargetType { get; }

        public virtual int ActionPointCost => 1;
        public virtual int ManaCost => 0;
        public virtual bool IsSpecial => false;
        public virtual bool IsUnique => false;

        public Sprite Artwork => artwork;
        public string Description => description;

        public IReadOnlyList<CardEffectCommand> CreateEffectCommands()
        {
            var commands = new List<CardEffectCommand>();
            BuildEffectCommands(commands);
            return commands;
        }

        protected abstract void BuildEffectCommands(List<CardEffectCommand> commands);
    }
}
