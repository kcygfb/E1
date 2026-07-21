using System;

namespace KiKs.Combat
{
    /// <summary>One physical card in the battle. Upgrade state never writes back to JSON.</summary>
    public sealed class CardInstance
    {
        public string InstanceId { get; }
        public CardSpec Spec { get; }
        public bool IsUpgraded { get; private set; }

        public CardInstance(string instanceId, CardSpec spec)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                throw new ArgumentException("Card instance id is required.", nameof(instanceId));

            InstanceId = instanceId;
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }

        internal bool TryUpgrade()
        {
            if (IsUpgraded || !Spec.CanUpgrade) return false;
            IsUpgraded = true;
            return true;
        }

        internal void ConsumeUpgrade()
        {
            IsUpgraded = false;
        }
    }
}
