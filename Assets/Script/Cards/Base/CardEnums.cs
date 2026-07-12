namespace KiKs.Cards
{
    public enum CardCategory
    {
        Attack,
        Magic,
        Defense
    }

    public enum CardFamily
    {
        Gun,
        Axe,
        Blade,
        ChainWeapon,
        Magic,
        Defense
    }

    public enum CardTargetType
    {
        Self,
        SingleEnemy
    }

    public enum DamageType
    {
        Normal,
        True
    }

    public enum CardEffectType
    {
        Damage,
        ToughnessDamageFlat,
        ToughnessDamagePercent,
        Poison,
        Freeze,
        Bleed,
        Burn,
        Blind,
        ArmorBreak,
        Stun,
        Dodge,
        DamageReduction,
        Stealth,
        Immunity
    }
}
