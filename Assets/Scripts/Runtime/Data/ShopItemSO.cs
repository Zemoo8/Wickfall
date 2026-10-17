using UnityEngine;

namespace Wickfall
{
    public enum ShopItemKind
    {
        WaxCandle,     // consumable: wax back
        SootSalve,     // consumable: clears soot
        FlameDamage,   // flame upgrade: damage tier
        FlameArtCost,  // flame upgrade: cheaper art
    }

    /// <summary>PDF §2.4 "Éclats de cire (monnaie) : dépensés aux braseros pour des consommables et améliorer les flammes" (G19).</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Shop Item", fileName = "ShopItem")]
    public sealed class ShopItemSO : ScriptableObject
    {
        public string id = "candle";
        public string nameKey = "shop.candle";
        public string descriptionKey = "shop.candle.desc";
        public ShopItemKind kind;
        public FlameId flame;
        [Tooltip("Upgrade tier (1 or 2) for flame upgrades.")]
        public int tier = 1;
        public int price = 30;
        public float value = 25f;
        [Tooltip("How many the player can carry (consumables) or buy (upgrades).")]
        public int maxOwned = 3;
    }
}
