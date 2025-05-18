# Card Power System

## Overview
A flexible, data-driven card system for implementing player power-ups and level progression in dungeon scenarios. The system supports dynamic card definitions, multiple rarity tiers, customizable layouts, and interactive selection mechanics. Additionally, it integrates with a shop system where players can purchase cards using gold currency.

## Features
- **Data-Driven Architecture**: Define cards and rarities via XML without code changes
- **Rarity System**: Multiple tiers with distinctive visuals and drop rates
- **Customizable Templates**: Support for custom card layouts and visuals
- **Runtime Management**: Complete API for card generation, selection, and effect application
- **Event-Based Integration**: Clean hooks for game systems interaction
- **Shop Integration**: Purchase cards with in-game gold currency
- **Inventory Management**: Track owned cards and available purchases
- **Flexible Effect System**: Apply temporary or permanent stat modifications to the player

## Core Components

### Card
```
┌─────────────────────┐
│ CardData            │
├─────────────────────┤
│ - Id: string        │
│ - Name: string      │
│ - Icon: Texture     │
│ - Rarity: Rarity    │
│ - Description: XML  │
│ - Layout: Template  │
│ - GoldCost: int     │
│ - Effects: CardEffect[] │
└─────────────────────┘
```

### CardEffect
```
┌─────────────────────┐
│ CardEffect          │
├─────────────────────┤
│ - EffectType: enum  │
│ - Value: float      │
│ - Duration: float   │
│ - IsTemporary: bool │
└─────────────────────┘
```

### Rarity
```
┌─────────────────────┐
│ Rarity              │
├─────────────────────┤
│ - Id: string        │
│ - DropRate: float   │
│ - ColorCode: string │
│ - Background: Texture│
│ - BaseCost: int     │
└─────────────────────┘
```

### Manager
```
┌─────────────────────────┐
│ CardManager             │
├─────────────────────────┤
│ - Rarities              │
│ - AllCards              │
│ - ActiveDeck            │
│ - PlayerInventory       │
├─────────────────────────┤
│ + LoadFromXML()         │
│ + GenerateDeck()        │
│ + SelectCards()         │
│ + ApplyCard()           │
│ + GetPlayerCards()      │
│ + Events                │
└─────────────────────────┘
```

### Shop
```
┌─────────────────────────┐
│ ShopManager             │
├─────────────────────────┤
│ - ShopInventory         │
│ - RefreshInterval       │
│ - FeaturedCards         │
├─────────────────────────┤
│ + RefreshShop()         │
│ + PurchaseCard()        │
│ + GetAvailableCards()   │
│ + Events                │
└─────────────────────────┘
```

## Data Structure

### Rarity Definition (XML)
```xml
<rarities>
  <rarity id="common">
    <name>Common</name>
    <dropRate>0.6</dropRate>
    <color>#CCCCCC</color>
    <background>Textures/UI/Card_Common_BG</background>
    <baseCost>50</baseCost>
  </rarity>
  <!-- Additional tiers -->
</rarities>
```

### Card Definition (XML)
```xml
<cards>
  <card id="power_strike">
    <name>Power Strike</name>
    <icon>Textures/UI/Icons/PowerStrike</icon>
    <rarity>rare</rarity>
    <description><![CDATA[
      Increases attack damage by {damageMultiplier}% for {duration} turns.
    ]]></description>
    <effects>
      <effect type="DamageMultiplier" value="0.15" duration="3" temporary="true" />
    </effects>
    <layout override="Templates/UI/Card_Rare_Template" />
    <goldCost>120</goldCost>
  </card>
  
  <card id="heart_container">
    <name>Heart Container</name>
    <icon>Textures/UI/Icons/HeartContainer</icon>
    <rarity>epic</rarity>
    <description><![CDATA[
      Permanently increases maximum health by {healthAmount} hearts.
    ]]></description>
    <effects>
      <effect type="AddRedHeart" value="1.0" temporary="false" />
    </effects>
    <layout override="Templates/UI/Card_Epic_Template" />
    <goldCost>200</goldCost>
  </card>
  <!-- More cards -->
</cards>
```

### Shop Definition (XML)
```xml
<shop>
  <refreshInterval>86400</refreshInterval> <!-- Daily refresh in seconds -->
  <slotCount>6</slotCount>
  <featuredSlotCount>2</featuredSlotCount>
  <rarityWeights>
    <common>60</common>
    <rare>30</rare>
    <epic>8</epic>
    <legendary>2</legendary>
  </rarityWeights>
  <discounts>
    <discount id="daily_special" value="0.25" slots="1" />
    <discount id="bundle" value="0.15" minPurchase="3" />
  </discounts>
</shop>
```

## Effect System

### CardEffectType Enum
```csharp
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
    // Additional effect types
}
```

### CardEffect Class
```csharp
[Serializable]
public class CardEffect
{
    public CardEffectType effectType;
    public float value;
    public float duration;
    public bool isTemporary;
    
    public void Apply(PlayerStats playerStats)
    {
        switch (effectType)
        {
            case CardEffectType.DamageMultiplier:
                playerStats.ModifyDamageMultiplier(value, isTemporary ? duration : 0);
                break;
                
            case CardEffectType.AttackSpeedMultiplier:
                playerStats.ModifyAttackSpeedMultiplier(value, isTemporary ? duration : 0);
                break;
                
            case CardEffectType.MoveSpeedMultiplier:
                playerStats.ModifyMoveSpeedMultiplier(value, isTemporary ? duration : 0);
                break;
                
            case CardEffectType.DodgeChance:
                playerStats.ModifyDodgeChance(value, isTemporary ? duration : 0);
                break;
                
            case CardEffectType.AddRedHeart:
                playerStats.AddHearts(HeartType.Red, value);
                break;
                
            case CardEffectType.AddBlueHeart:
                playerStats.AddHearts(HeartType.Blue, value);
                break;
                
            case CardEffectType.AddBlackHeart:
                playerStats.AddHearts(HeartType.Black, value);
                break;
                
            case CardEffectType.GoldMultiplier:
                playerStats.ModifyGoldDropMultiplier(value);
                break;
                
            case CardEffectType.ShopDiscount:
                playerStats.ModifyShopDiscount(value);
                break;
                
            // Additional effect types
        }
    }
    
    public string GetDisplayName()
    {
        // Return display-friendly effect type name
        return effectType.ToString().SplitCamelCase();
    }
}
```

## Implementation

### CardManager API
```csharp
public class CardManager : MonoBehaviour
{
    #region Properties
    public List<CardData> AllCards { get; private set; }
    public List<Rarity> Rarities { get; private set; }
    public List<CardData> ActiveDeck { get; private set; }
    public Dictionary<string, int> PlayerInventory { get; private set; } // Card ID -> Count
    #endregion

    #region Public Methods
    public void LoadRarities(string xmlPath) { /* Implementation */ }
    public void LoadCards(string xmlPath) { /* Implementation */ }
    public void GenerateDeck(int count, string filterTag = null) { /* Implementation */ }
    public void ApplySelection(List<CardData> selected) { /* Implementation */ }
    public List<CardData> GetPlayerCards() { /* Implementation */ }
    public bool AddCardToInventory(string cardId, int count = 1) { /* Implementation */ }
    #endregion

    #region Events
    public event Action<List<CardData>> OnDeckGenerated;
    public event Action<CardData, bool> OnCardSelected; // (card, isSelected)
    public event Action<List<CardData>> OnSelectionApproved;
    public event Action<string, int> OnCardAddedToInventory; // (cardId, newCount)
    #endregion
}
```

### CardData Implementation
```csharp
[CreateAssetMenu(fileName = "Card", menuName = "CardSystem/Card")]
public class CardData : ScriptableObject
{
    #region Fields
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Rarity rarity;
    [SerializeField] private string description;
    [SerializeField] private string layoutTemplate;
    [SerializeField] private int goldCost;
    [SerializeField] private List<CardEffect> effects = new List<CardEffect>();
    #endregion
    
    #region Properties
    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Rarity Rarity => rarity;
    public string Description => description;
    public string LayoutTemplate => layoutTemplate;
    public int GoldCost => goldCost;
    public IReadOnlyList<CardEffect> Effects => effects;
    #endregion

    #region Public Methods
    public void ApplyEffects(PlayerStats target)
    {
        foreach (var effect in effects)
        {
            effect.Apply(target);
        }
    }
    
    public int CalculatePrice(float discountMultiplier = 1.0f) 
    { 
        return Mathf.RoundToInt(goldCost * discountMultiplier);
    }
    
    public string GetProcessedDescription()
    {
        string processedDesc = Description;
        
        // Replace placeholders with effect values
        foreach (var effect in effects)
        {
            // For damage multipliers and chances, convert to percentage
            if (effect.effectType == CardEffectType.DamageMultiplier || 
                effect.effectType == CardEffectType.AttackSpeedMultiplier ||
                effect.effectType == CardEffectType.MoveSpeedMultiplier ||
                effect.effectType == CardEffectType.DodgeChance)
            {
                processedDesc = processedDesc.Replace($"{{{effect.effectType.ToString().ToCamelCase()}}}", $"{effect.value * 100:0}");
            }
            // Handle durations
            else if (effect.isTemporary)
            {
                processedDesc = processedDesc.Replace("{duration}", $"{effect.duration:0.0}");
            }
            // Handle health amounts
            else if (effect.effectType == CardEffectType.AddRedHeart ||
                    effect.effectType == CardEffectType.AddBlueHeart ||
                    effect.effectType == CardEffectType.AddBlackHeart)
            {
                processedDesc = processedDesc.Replace("{healthAmount}", $"{effect.value:0.#}");
            }
            // Generic value replacement
            else
            {
                processedDesc = processedDesc.Replace($"{{{effect.effectType.ToString().ToCamelCase()}}}", $"{effect.value:0.##}");
            }
        }
        
        return processedDesc;
    }
    #endregion
}
```

### ShopManager API
```csharp
public class ShopManager : MonoBehaviour
{
    #region Properties
    public List<ShopItem> ShopInventory { get; private set; }
    public List<ShopItem> FeaturedItems { get; private set; }
    public int RefreshInterval { get; private set; } // In seconds
    public DateTime NextRefreshTime { get; private set; }
    #endregion
    
    #region Public Methods
    public void LoadShopConfig(string xmlPath) { /* Implementation */ }
    public void RefreshShop(bool forceFull = false) { /* Implementation */ }
    public bool PurchaseCard(string cardId, int quantity = 1) { /* Implementation */ }
    public bool CanPurchase(string cardId, int quantity = 1) { /* Implementation */ }
    public int GetCardCost(string cardId, int quantity = 1) { /* Implementation */ }
    public void ApplyDiscount(string discountId) { /* Implementation */ }
    #endregion
    
    #region Events
    public event Action OnShopRefreshed;
    public event Action<string, int, int> OnCardPurchased; // (cardId, quantity, totalCost)
    public event Action<string> OnPurchaseFailed; // (reason)
    #endregion
}
```

### ShopItem Implementation
```csharp
public class ShopItem
{
    #region Properties
    public CardData Card { get; private set; }
    public int Quantity { get; private set; }
    public float DiscountMultiplier { get; private set; }
    public bool IsFeatured { get; private set; }
    public int FinalCost => Card.CalculatePrice(DiscountMultiplier);
    #endregion
    
    #region Public Methods
    public void ApplyDiscount(float multiplier) { /* Implementation */ }
    public void SetFeatured(bool featured) { /* Implementation */ }
    #endregion
}
```

## UI Workflow

### Card Selection Workflow
1. **Generation**: System creates a deck of N random cards based on rarity weights
2. **Display**: Cards are presented in a grid with visual styling based on rarity
3. **Selection**: Player selects up to Y cards (minimum 1)
4. **Confirmation**: Player approves selection
5. **Application**: Selected card effects are applied to player stats
6. **Cleanup**: UI closes, deck is cleared or cards are stored in inventory

### Shop Workflow
1. **Open Shop**: Player accesses the shop UI from the game menu
2. **Browse Cards**: Available cards are displayed with costs and details
3. **Card Preview**: Player can examine card effects before purchase
4. **Purchase**: Player spends gold to add card to inventory
5. **Confirmation**: Success/failure message is displayed
6. **Refresh**: Shop inventory updates periodically or with manual refresh

## Usage Example

### Card System Example
```csharp
// Initialization
private void Start()
{
 CardManager.Instance.LoadRarities("Assets/Data/rarities.xml");  
CardManager.Instance.LoadCards("Assets/Data/cards.xml");  
    CardManager.Instance.OnSelectionApproved += ApplyCards;
}

// Level-Up Event
private void OnLevelUp() 
{
  CardManager.Instance.GenerateDeck(5);  
  CardUI.OpenDeck(CardManager.Instance.ActiveDeck, maxSelections: 2);  
}  

// Apply Selected Cards
private void ApplyCards(List<CardData> selection)
{
    PlayerStats playerStats = GetComponent<PlayerStats>();
    foreach (var card in selection)
    {
        card.ApplyEffects(playerStats);
    }
}
```

### Shop System Example
```csharp
// Initialization
private void Start()
{
    ShopManager.Instance.LoadShopConfig("Assets/Data/shop.xml");
    ShopManager.Instance.RefreshShop();
    ShopManager.Instance.OnCardPurchased += HandleCardPurchased;
}

// Open Shop UI
private void OpenShop()
{
    List<ShopItem> shopItems = ShopManager.Instance.ShopInventory;
    ShopUI.Show(shopItems);
}

// Purchase Card
private void BuyCard(string cardId)
{
    PlayerStats playerStats = GetComponent<PlayerStats>();
    if (ShopManager.Instance.CanPurchase(cardId))
    {
        int cost = ShopManager.Instance.GetCardCost(cardId);
        if (playerStats.SpendGold(cost))
        {
            ShopManager.Instance.PurchaseCard(cardId);
        }
        else
        {
            ShopUI.ShowMessage("Not enough gold!");
        }
    }
}

// Handle Purchase Result
private void HandleCardPurchased(string cardId, int quantity, int totalCost)
{
    ShopUI.ShowConfirmation($"Purchased {quantity}x {cardId} for {totalCost} gold!");
    UpdateInventoryUI();
}
```

## XML Parser Implementation

```csharp
public class CardXmlParser
{
    public static List<CardData> ParseCardsFromXml(XmlDocument doc)
    {
        List<CardData> cards = new List<CardData>();
        
        XmlNodeList cardNodes = doc.SelectNodes("//card");
        foreach (XmlNode cardNode in cardNodes)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            
            // Parse basic properties
            card.id = cardNode.Attributes["id"]?.Value;
            card.displayName = cardNode.SelectSingleNode("name")?.InnerText;
            card.icon = Resources.Load<Sprite>(cardNode.SelectSingleNode("icon")?.InnerText);
            card.rarity = FindRarityById(cardNode.SelectSingleNode("rarity")?.InnerText);
            card.description = cardNode.SelectSingleNode("description")?.InnerText;
            card.layoutTemplate = cardNode.SelectSingleNode("layout")?.Attributes["override"]?.Value;
            
            // Parse gold cost
            int.TryParse(cardNode.SelectSingleNode("goldCost")?.InnerText, out int cost);
            card.goldCost = cost;
            
            // Parse effects
            XmlNodeList effectNodes = cardNode.SelectNodes("effects/effect");
            foreach (XmlNode effectNode in effectNodes)
            {
                CardEffect effect = new CardEffect();
                
                // Parse effect type
                string typeStr = effectNode.Attributes["type"]?.Value;
                if (Enum.TryParse(typeStr, out CardEffectType effectType))
                {
                    effect.effectType = effectType;
                }
                
                // Parse value
                float.TryParse(effectNode.Attributes["value"]?.Value, out float value);
                effect.value = value;
                
                // Parse duration
                float.TryParse(effectNode.Attributes["duration"]?.Value, out float duration);
                effect.duration = duration;
                
                // Parse temporary flag
                bool.TryParse(effectNode.Attributes["temporary"]?.Value, out bool isTemp);
                effect.isTemporary = isTemp;
                
                card.effects.Add(effect);
            }
            
            cards.Add(card);
        }
        
        return cards;
    }
    
    private static Rarity FindRarityById(string rarityId)
    {
        // Implementation to find rarity by ID
        return null;
    }
}
```

## Extension Points

- **Add Rarities**: Modify rarities.xml to create new tiers with appropriate costs
- **Create Cards**: Add card entries to cards.xml with gold costs
- **Custom Visuals**: Create new templates and reference via layout override
- **Effect Types**: Add new effect types to CardEffectType enum for new mechanics
- **Shop Customization**: Modify shop.xml to change refresh rates, slot counts, and discounts
- **Special Offers**: Implement time-limited discounts and bundles

## Integration Guide

1. Add the CardSystem and ShopSystem prefabs to your scene
2. Configure XML paths in the inspector for cards, rarities, and shop settings
3. Subscribe to card and shop events for game logic integration
4. Call GenerateDeck() when cards should be offered as rewards
5. Call RefreshShop() to update the available shop inventory
6. Style UI components to match your game's aesthetic

## Monetization Options

The Shop System can be extended to support:

1. **Premium Currency**: Add gems/diamonds for purchasing rare cards
2. **Card Packs**: Bundle multiple random cards at a discount
3. **Subscription**: Daily card offers or shop refreshes
4. **Battle Pass**: Unlock special cards through gameplay tiers

