namespace KiKs.Combat
{
    internal static class CombatWarningText
    {
        public static string FromResult(CombatResult result)
        {
            return FromMessage(result != null ? result.Message : string.Empty);
        }

        private static string FromMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "This action is unavailable.";

            if (Contains(message, "must be activated"))
                return "Activate this magic card first.";
            if (Contains(message, "already activated"))
                return "This magic card is already activated.";
            if (Contains(message, "action points"))
                return "Not enough action points.";
            if (Contains(message, "mana"))
                return "Not enough mana, or the mana limit has been reached.";
            if (Contains(message, "already upgraded"))
                return "This card is already upgraded.";
            if (Contains(message, "Magic cards cannot be upgraded"))
                return "Magic cards cannot be upgraded.";
            if (Contains(message, "has no upgraded values"))
                return "This card cannot be upgraded.";
            if (Contains(message, "not in hand") || (Contains(message, "not in") && Contains(message, "hand")))
                return "This card is no longer in your hand.";
            if ((Contains(message, "target") && (Contains(message, "invalid") || Contains(message, "dead"))) ||
                Contains(message, "already dead"))
                return "No valid target.";
            if (Contains(message, "turn cannot end"))
                return "You cannot end the turn now.";
            if (Contains(message, "not being shot"))
                return "This card cannot keep shooting.";
            if (Contains(message, "phase") || Contains(message, "during player input"))
                return "It is not your turn.";

            return "This action is unavailable.";
        }

        private static bool Contains(string message, string fragment)
        {
            return message.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}