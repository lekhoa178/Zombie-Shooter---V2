using System;

namespace CardSystem
{
    /// <summary>
    /// Effect types for cards
    /// </summary>
    public enum CardEffectType
    {
        DamageMultiplier,     // Increases player damage
        AttackSpeedMultiplier, // Increases attack speed
        MoveSpeedMultiplier,   // Increases movement speed
        DodgeChance,          // Increases dodge chance
        AddRedHeart,          // Adds red heart containers
        AddBlueHeart,         // Adds blue/shield hearts
        AddBlackHeart,        // Adds black hearts
        GoldMultiplier,       // Increases gold drops
        ShopDiscount,         // Provides shop discount
    }
} 