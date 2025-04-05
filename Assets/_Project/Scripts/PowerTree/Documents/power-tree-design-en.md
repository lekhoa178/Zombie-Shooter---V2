# Power Tree Design

> **Note:** Each node requires Aether Shards to unlock and upgrade:
> - Unlock: 1 Aether Shard
> - Level 2: 2 Aether Shards
> - Level 3: 3 Aether Shards

## Health System Mechanics
- **Red Hearts**: Base health with a maximum limit. Lost when taking damage.
- **Blue Hearts**: Shield hearts that don't count toward maximum health. Lost before red hearts.
- **Black Hearts**: Special shield hearts that also push enemies back and eliminate nearby projectiles when lost.
- **Damage**: Player loses 0.5 heart per hit.

## 🌳 Branch 1: Survival - Endurance Against All Dangers

### 1. Gentle Rebirth (Total: 6 Aether Shards)
- **Level 1:** 10% chance to not lose a red heart when it would be lost
- **Level 2:** Increases to 20%
- **Level 3:** Increases to 30%

### 2. Strong Life Drop (Total: 6 Aether Shards)
- **Level 1:** Heal 1 red heart after every 2 levels
- **Level 2:** Reduces to 1 level
- **Level 3:** Heal 1 red heart per level and 1 blue heart after 3 levels

### 3. Temporary Shield (Total: 6 Aether Shards)
- **Level 1:** Receive 2 blue hearts at the start of each level
- **Level 2:** Receive 3 blue hearts
- **Level 3:** Receive 3 blue hearts and 10% chance to not lose anything when losing a blue heart

### 4. 💙 Blood Transforms into Armor (Total: 6 Aether Shards)
- **Level 1:** 20% chance to recover 0.5 blue heart when losing a blue heart. When losing a black heart, increase damage by 10% for 5 seconds
- **Level 2:** Increases chance to 35%, black heart damage +15%
- **Level 3:** Increases chance to 50%, black heart damage +20%, stronger knockback and small area damage

### 5. 🖤 Dark Power (Total: 6 Aether Shards)
- **Level 1:** When losing a black heart, negate more than 2 bullets instead of just 1
- **Level 2:** When losing a black heart, create a shockwave that pushes nearby enemies
- **Level 3:** When losing a black heart, deal 50% damage to all enemies on screen (doesn't affect bosses)

### 6. 🌀 Near Death Revival (Total: 6 Aether Shards)
- **Level 1:** 5% chance to revive with 1 blue heart when dying
- **Level 2:** Increases chance to 10%, revive with 1 red heart
- **Level 3:** Increases chance to 15%, revive with 1 red heart and 1 blue heart

### 7. 🛠️ Last Shield (Total: 6 Aether Shards)
- **Level 1:** If only 0.5 red heart remains, 30% chance to completely dodge the next attack
- **Level 2:** Increases dodge chance to 50%
- **Level 3:** Successful dodge creates a wave that pushes back surrounding enemies

### 8. 💖 Life Force Transformation (Total: 6 Aether Shards)
- **Level 1:** 30% chance to create 0.5 blue heart when receiving a red heart
- **Level 2:** Increases chance to 50%
- **Level 3:** Increases chance to 70%, created blue heart can be a black heart

## ⚔️ Branch 2: Attack - Destroy Enemies

### 1. Wild Strength (Total: 6 Aether Shards)
- **Level 1:** Increase damage by 10% when above 50% health
- **Level 2:** Increases to 20%
- **Level 3:** Increases to 30%

### 2. Rage Destruction (Total: 6 Aether Shards)
- **Level 1:** When losing a red heart, increase damage by 15% for 5 seconds
- **Level 2:** Increases to 25%
- **Level 3:** Increases to 35% and lasts for 7 seconds

### 3. Lightning Strike (Total: 6 Aether Shards)
- **Level 1:** Attack speed increases by 10%
- **Level 2:** Increases to 20%
- **Level 3:** Increases to 30% and 5% chance for attacks to deal double damage

### 4. ⚡ Berserk (Total: 6 Aether Shards)
- **Level 1:** 5% chance to increase attack speed by 20% for 3 seconds when hitting
- **Level 2:** Increases to 10% chance, 30% attack speed
- **Level 3:** Increases to 15% chance, 40% attack speed, can stack twice

### 5. 🩸 Bloodthirsty Combat (Total: 6 Aether Shards)
- **Level 1:** When defeating 3 enemies within 5 seconds, gain 20% attack speed for 5 seconds
- **Level 2:** Buff increases to 25% attack speed and 5% damage
- **Level 3:** Buff can stack twice, up to 30% attack speed and 10% damage

### 6. Piercing Damage (Total: 6 Aether Shards)
- **Level 1:** Damage increases by 25% if enemy has above 75% health
- **Level 2:** Increases to 35%
- **Level 3:** Increases to 50%

## 🌀 Branch 3: Evasion - High Speed

### 1. 👣 Ghost Steps (Total: 6 Aether Shards)
- **Level 1:** Increase movement speed by 10%
- **Level 2:** Increases to 20%
- **Level 3:** When successfully dodging, gain 50% movement speed for 2 seconds

### 2. 🌬️ Wind Evasion (Total: 6 Aether Shards)
- **Level 1:** 10% chance to completely dodge when attacked
- **Level 2:** Increases to 15%
- **Level 3:** Increases to 20%, if dodge is successful

### 3. 🦊 Agility (Total: 6 Aether Shards)
- **Level 1:** When HP below 1 red heart, movement speed +10%
- **Level 2:** Increases to +20%
- **Level 3:** When HP below 1 red heart, dodge +10%

## 💰 Branch 4: Finance - Earn More Gold

### 1. 💎 Luck (Total: 6 Aether Shards)
- **Level 1:** 10% chance for enemies to drop gold
- **Level 2:** Increases to 20%
- **Level 3:** Increases to 30%, can drop x2 gold

### 2. Hell Merchant (Total: 6 Aether Shards)
- **Level 1:** Reduce card price in shop by 5%
- **Level 2:** Reduce by 10%
- **Level 3:** Reduce by 15%

### 3. Infinite Treasure (Total: 6 Aether Shards)
- **Level 1:** If completing level without losing health, receive 50% more gold
- **Level 2:** Increases to 75%
- **Level 3:** Increases to 100%

### 4. Safe Finance (Total: 6 Aether Shards)
- **Level 1:** When losing a red heart, receive 10 gold back
- **Level 2:** Receive 15 gold back
- **Level 3:** Receive 20 gold back and 10% chance to not lose heart

## Tree Structure

### Branch 1: Survival
```
[Survival Root]
       │
┌──────┴──────┐
│             │
[Gentle Rebirth] [Strong Life Drop]
       │             │
┌──────┴──────┐ ┌────┴────┐
│             │ │         │
[Temporary Shield] [Last Shield] [Blood Armor] [Dark Power]
       │             │
       └──────┐ ┌────┘
              │ │
        [Near Death] [Life Transform]
```

### Branch 2: Attack
```
[Attack Root]
       │
┌──────┴──────┐
│             │
[Wild Strength] [Lightning Strike]
       │             │
┌──────┴──────┐ ┌────┴────┐
│             │ │         │
[Rage Destruction] [Piercing Damage] [Berserk] [Bloodthirsty]
```

### Branch 3: Evasion
```
[Evasion Root]
       │
┌──────┴──────┐
│             │
[Ghost Steps] [Wind Evasion]
       │             │
       └──────┐ ┌────┘
              │
         [Agility]
```

### Branch 4: Finance
```
[Finance Root]
       │
┌──────┴──────┐
│             │
[Luck] [Hell Merchant]
       │             │
└──────┐ ┌────┘
       │ │
[Infinite Treasure] [Safe Finance]
```

## Unlocking Rules

### Branch 1: Survival
- Start with either "Gentle Rebirth" or "Strong Life Drop" (1 Aether Shard each)
- "Gentle Rebirth" unlocks "Temporary Shield" and "Last Shield" (1 Aether Shard each)
- "Strong Life Drop" unlocks "Blood Armor" and "Dark Power" (1 Aether Shard each)
- "Last Shield" unlocks "Near Death" (1 Aether Shard)
- "Blood Armor" unlocks "Life Transform" (1 Aether Shard)

### Branch 2: Attack
- Start with either "Wild Strength" or "Lightning Strike" (1 Aether Shard each)
- "Wild Strength" unlocks "Rage Destruction" and "Piercing Damage" (1 Aether Shard each)
- "Lightning Strike" unlocks "Berserk" and "Bloodthirsty" (1 Aether Shard each)

### Branch 3: Evasion
- Start with either "Ghost Steps" or "Wind Evasion" (1 Aether Shard each)
- Both "Ghost Steps" and "Wind Evasion" unlock "Agility" (1 Aether Shard)

### Branch 4: Finance
- Start with either "Luck" or "Hell Merchant" (1 Aether Shard each)
- "Luck" unlocks "Infinite Treasure" (1 Aether Shard)
- "Hell Merchant" unlocks "Safe Finance" (1 Aether Shard)

## Total Aether Shard Costs
- Each node costs 6 Aether Shards to fully upgrade (1 + 2 + 3)
- Each branch has 8 nodes
- Total cost per branch: 48 Aether Shards
- Total cost for all branches: 192 Aether Shards 