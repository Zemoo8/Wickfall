using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// The PDF's tables turned into data. WICKFALL > Setup writes these values into the shipped
    /// assets once (after that the assets are the source, editable in the Inspector), and
    /// PdfValuesTests checks the shipped assets against its own copy of the PDF numbers.
    /// Values marked "proposal" are not in the PDF (see Docs/DESIGN_DECISIONS.md).
    /// </summary>
    public static class PdfTables
    {
        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static string FlameKey(FlameId id)
        {
            switch (id)
            {
                case FlameId.Amber: return "amber";
                case FlameId.Blue: return "blue";
                case FlameId.Green: return "green";
                case FlameId.White: return "white";
                default: return "crimson";
            }
        }

        public static string RegionKey(RegionId id)
        {
            switch (id)
            {
                case RegionId.TallowFields: return "tallow";
                case RegionId.Wickwood: return "wickwood";
                case RegionId.SootFoundries: return "foundries";
                case RegionId.GlassCathedral: return "cathedral";
                case RegionId.DeepHollow: return "hollow";
                default: return "hearth";
            }
        }

        public static FlameTypeSO CreateFlame(FlameId id)
        {
            var f = ScriptableObject.CreateInstance<FlameTypeSO>();
            ApplyFlame(f, id);
            return f;
        }

        /// <summary>PDF §2.1 flame table + G02/G03 proposals.</summary>
        public static void ApplyFlame(FlameTypeSO f, FlameId id)
        {
            string key = FlameKey(id);
            f.name = "Flame_" + id;
            f.id = id;
            f.nameKey = "flame." + key;
            f.roleKey = "flame." + key + ".role";
            f.artNameKey = "art." + key;
            f.damageScalesWithLowWax = false;
            f.damageAtEmptyWax = 2.2f;
            f.damageAtFullWax = 0.6f;
            f.hollowBonus = 1f;
            f.poisonDamagePerSecond = 0f;
            f.poisonSeconds = 0f;
            f.slowFactor = 1f;
            f.slowSeconds = 0f;

            switch (id)
            {
                case FlameId.Amber: // Équilibrée, arc de mêlée | x1,0 | x1,0 | x1,0
                    f.waxCostMultiplier = 1.0f; f.damageMultiplier = 1.0f; f.lightMultiplier = 1.0f;
                    f.coreColor = Hex("#FFE9A8"); f.bodyColor = Hex("#FFC14D"); f.edgeColor = Hex("#F28A1D"); f.lightColor = Hex("#FFC67A");
                    f.attackStyle = FlameAttackStyle.MeleeArc;
                    f.attackRange = 1.45f; f.attackArcDegrees = 130f; f.attackDuration = 0.26f; f.attackCooldown = 0.1f;
                    f.artBaseWaxCost = 6f; f.artDamageMultiplier = 2.4f; f.artRadius = 2.4f; f.artCooldown = 0.8f;
                    f.unlockedAtStart = true; f.unlockedIn = RegionId.TallowFields;
                    break;
                case FlameId.Blue: // À distance, ralentit | x0,9 | x0,7 | x0,85
                    f.waxCostMultiplier = 0.9f; f.damageMultiplier = 0.7f; f.lightMultiplier = 0.85f;
                    f.coreColor = Hex("#E0F7FF"); f.bodyColor = Hex("#8EDBFF"); f.edgeColor = Hex("#3A9BE8"); f.lightColor = Hex("#9FDDFF");
                    f.attackStyle = FlameAttackStyle.Bolt;
                    f.attackRange = 0.6f; f.attackArcDegrees = 30f; f.attackDuration = 0.22f; f.attackCooldown = 0.2f;
                    f.projectileSpeed = 12f; f.projectileRange = 7.5f;
                    f.slowFactor = 0.45f; f.slowSeconds = 2.2f;
                    f.artBaseWaxCost = 6f; f.artDamageMultiplier = 2.0f; f.artRadius = 0.6f; f.artCooldown = 0.9f;
                    f.unlockedAtStart = false; f.unlockedIn = RegionId.TallowFields;
                    break;
                case FlameId.Green: // Poison, touche les groupes | x1,4 | x0,9 | x1,1
                    f.waxCostMultiplier = 1.4f; f.damageMultiplier = 0.9f; f.lightMultiplier = 1.1f;
                    f.coreColor = Hex("#E6FFD0"); f.bodyColor = Hex("#9BEA6A"); f.edgeColor = Hex("#46A53E"); f.lightColor = Hex("#B6F08E");
                    f.attackStyle = FlameAttackStyle.PoisonCloud;
                    f.attackRange = 1.6f; f.attackArcDegrees = 200f; f.attackDuration = 0.32f; f.attackCooldown = 0.18f;
                    f.poisonDamagePerSecond = 3f; f.poisonSeconds = 3f;
                    f.artBaseWaxCost = 6f; f.artDamageMultiplier = 1.6f; f.artRadius = 2.8f; f.artCooldown = 1.0f;
                    f.unlockedAtStart = false; f.unlockedIn = RegionId.Wickwood;
                    break;
                case FlameId.White: // Anti-Creux, énorme lumière | x1,2 | x1,6 | x1,8
                    f.waxCostMultiplier = 1.2f; f.damageMultiplier = 1.6f; f.lightMultiplier = 1.8f;
                    f.coreColor = Hex("#FFFFFF"); f.bodyColor = Hex("#F4F0DE"); f.edgeColor = Hex("#CEC5B2"); f.lightColor = Hex("#FFF8E6");
                    f.attackStyle = FlameAttackStyle.RadiantSlash;
                    f.attackRange = 1.5f; f.attackArcDegrees = 150f; f.attackDuration = 0.3f; f.attackCooldown = 0.14f;
                    f.hollowBonus = 1.5f;
                    f.artBaseWaxCost = 6f; f.artDamageMultiplier = 1.8f; f.artRadius = 3.2f; f.artCooldown = 1.1f;
                    f.unlockedAtStart = false; f.unlockedIn = RegionId.SootFoundries;
                    break;
                default: // Cramoisie: Plus de dégâts à faible cire | x1,6 | x0,6 à 2,2 | x1,3
                    f.waxCostMultiplier = 1.6f; f.damageMultiplier = 1.0f; f.lightMultiplier = 1.3f;
                    f.damageScalesWithLowWax = true;
                    f.coreColor = Hex("#FFD0C8"); f.bodyColor = Hex("#FF5A4A"); f.edgeColor = Hex("#C21A33"); f.lightColor = Hex("#FF7A66");
                    f.attackStyle = FlameAttackStyle.HeavyStrike;
                    f.attackRange = 1.4f; f.attackArcDegrees = 100f; f.attackDuration = 0.34f; f.attackCooldown = 0.2f;
                    f.artBaseWaxCost = 6f; f.artDamageMultiplier = 2.6f; f.artRadius = 1.8f; f.artCooldown = 1.0f;
                    f.unlockedAtStart = false; f.unlockedIn = RegionId.GlassCathedral;
                    break;
            }
        }

        public static RegionSO CreateRegion(RegionId id)
        {
            var r = ScriptableObject.CreateInstance<RegionSO>();
            ApplyRegion(r, id);
            return r;
        }

        /// <summary>PDF §2.2 environments + G03/G04/G09 proposals.</summary>
        public static void ApplyRegion(RegionSO r, RegionId id)
        {
            string key = RegionKey(id);
            r.name = "Region_" + (int)(id + 1) + "_" + id;
            r.id = id;
            r.nameKey = "region." + key;
            r.environmentKey = "region." + key + ".env";
            r.relicIndex = (int)id;
            r.waxLossMultiplier = 1f;
            r.lightRevealGround = false;
            r.negativeLight = false;
            r.heartbeat = false;
            r.grantsFlame = true;
            r.baseScene = "R" + ((int)id + 1) + "_" + id;

            switch (id)
            {
                case RegionId.TallowFields: // Plaine de cire, lumière ambiante faible
                    r.ambientIntensity = 0.16f; r.ambientColor = Hex("#A39A8B");
                    r.flameFound = FlameId.Blue; r.parTimeSeconds = 720f; r.music = MusicId.TallowFields;
                    break;
                case RegionId.Wickwood: // Forêt noire, canopée qui bloque la lumière
                    r.ambientIntensity = 0.09f; r.ambientColor = Hex("#6F5D40");
                    r.flameFound = FlameId.Green; r.parTimeSeconds = 840f; r.music = MusicId.Wickwood;
                    break;
                case RegionId.SootFoundries: // Usine, fours qui vacillent
                    r.ambientIntensity = 0.1f; r.ambientColor = Hex("#7A7266");
                    r.flameFound = FlameId.White; r.parTimeSeconds = 900f; r.music = MusicId.SootFoundries;
                    break;
                case RegionId.GlassCathedral: // Miroirs noirs
                    r.ambientIntensity = 0.1f; r.ambientColor = Hex("#CEC5B2");
                    r.flameFound = FlameId.Crimson; r.parTimeSeconds = 960f; r.music = MusicId.GlassCathedral;
                    break;
                case RegionId.DeepHollow: // Aucune lumière ambiante | Perte de cire x1,5
                    r.ambientIntensity = 0f; r.ambientColor = Color.black;
                    r.waxLossMultiplier = 1.5f; r.lightRevealGround = true; r.heartbeat = true;
                    r.grantsFlame = false; r.flameFound = FlameId.White; r.parTimeSeconds = 900f; r.music = MusicId.DeepHollow;
                    break;
                default: // Cathédrale de cendre, Foyer en lumière négative
                    r.ambientIntensity = 0.2f; r.ambientColor = Hex("#A39A8B");
                    r.negativeLight = true;
                    r.grantsFlame = false; r.flameFound = FlameId.White; r.parTimeSeconds = 720f; r.music = MusicId.HearthChamber;
                    break;
            }
        }

        public static RelicSO CreateRelic(int index)
        {
            var r = ScriptableObject.CreateInstance<RelicSO>();
            ApplyRelic(r, index);
            return r;
        }

        /// <summary>PDF §2.4 relic examples (4) + two proposals (G04).</summary>
        public static void ApplyRelic(RelicSO r, int index)
        {
            r.index = index;
            r.region = (RegionId)index;
            r.nameKey = "relic." + index;
            r.descriptionKey = "relic." + index + ".desc";
            r.name = "Relic_" + index;
            switch (index)
            {
                case 0: r.effect = RelicEffect.ExtraDodge; r.value = 1f; r.fromPdf = true; r.glow = Hex("#FFC14D"); break;
                case 1: r.effect = RelicEffect.WallGrip; r.value = 1f; r.fromPdf = true; r.glow = Hex("#9BEA6A"); break;
                case 2: r.effect = RelicEffect.MaxWax; r.value = 20f; r.fromPdf = true; r.glow = Hex("#F4F0DE"); break;
                case 3: r.effect = RelicEffect.FlameSlot; r.value = 1f; r.fromPdf = true; r.glow = Hex("#8EDBFF"); break;
                case 4: r.effect = RelicEffect.SpareWick; r.value = 25f; r.fromPdf = false; r.glow = Hex("#FFE9A8"); break;
                default: r.effect = RelicEffect.HearthEmber; r.value = 0.25f; r.fromPdf = false; r.glow = Hex("#FF5A4A"); break;
            }
        }

        public static EnemyTypeSO CreateEnemy(EnemyKind kind)
        {
            var e = ScriptableObject.CreateInstance<EnemyTypeSO>();
            ApplyEnemy(e, kind);
            return e;
        }

        /// <summary>PDF §2.2 creatures. Behaviour flags are the PDF's; numbers are G06 proposals.</summary>
        public static void ApplyEnemy(EnemyTypeSO e, EnemyKind kind)
        {
            e.name = "Enemy_" + kind;
            e.kind = kind;
            e.nameKey = "enemy." + kind.ToString().ToLowerInvariant();
            e.tags = DamageTags.None;
            e.countsForAvoided = true;
            e.contactDamage = 0f;
            e.perceptionScale = 1f;
            e.knockbackResistance = 0f;
            switch (kind)
            {
                case EnemyKind.Dripling: // slow melee
                    e.homeRegion = RegionId.TallowFields; e.maxHealth = 22f; e.colliderRadius = 0.45f;
                    e.wanderSpeed = 0.6f; e.chaseSpeed = 1.55f; e.attackDamage = 8f; e.attackRange = 0.95f;
                    e.attackWindup = 0.55f; e.attackActive = 0.15f; e.attackRecover = 0.7f; e.attackCooldown = 1.0f;
                    e.killWaxMin = 4; e.killWaxMax = 6; e.shardDrop = 2;
                    break;
                case EnemyKind.Waxfly: // harmless ambient life (G20)
                    e.homeRegion = RegionId.TallowFields; e.maxHealth = 1f; e.colliderRadius = 0.15f;
                    e.wanderSpeed = 1.4f; e.chaseSpeed = 3.5f; e.attackDamage = 0f; e.attackRange = 0f;
                    e.killWaxMin = 0; e.killWaxMax = 0; e.shardDrop = 0; e.countsForAvoided = false;
                    break;
                case EnemyKind.Husk: // ambush from branches
                    e.homeRegion = RegionId.Wickwood; e.maxHealth = 32f; e.colliderRadius = 0.5f;
                    e.wanderSpeed = 0f; e.chaseSpeed = 2.5f; e.attackDamage = 12f; e.attackRange = 1.3f;
                    e.attackWindup = 0.4f; e.attackActive = 0.2f; e.attackRecover = 0.8f; e.attackCooldown = 1.2f;
                    e.perceptionScale = 0.7f; e.killWaxMin = 7; e.killWaxMax = 9; e.shardDrop = 3;
                    break;
                case EnemyKind.Lurker: // moves only while you shine
                    e.homeRegion = RegionId.Wickwood; e.maxHealth = 26f; e.colliderRadius = 0.42f; e.tags = DamageTags.Hollow;
                    e.wanderSpeed = 0f; e.chaseSpeed = 2.3f; e.attackDamage = 10f; e.attackRange = 0.9f;
                    e.attackWindup = 0.3f; e.attackActive = 0.15f; e.attackRecover = 0.6f; e.attackCooldown = 0.9f;
                    e.killWaxMin = 6; e.killWaxMax = 8; e.shardDrop = 3;
                    break;
                case EnemyKind.LightEaterBloom: // obstacle: cuts your light for 3 s
                    e.homeRegion = RegionId.Wickwood; e.maxHealth = 18f; e.colliderRadius = 0.5f;
                    e.wanderSpeed = 0f; e.chaseSpeed = 0f; e.attackDamage = 0f; e.attackRange = 1.6f;
                    e.attackWindup = 0.35f; e.attackCooldown = 4f; e.knockbackResistance = 1f;
                    e.killWaxMin = 4; e.killWaxMax = 5; e.shardDrop = 1; e.countsForAvoided = false;
                    break;
                case EnemyKind.Slagborn: // armored: only white or green
                    e.homeRegion = RegionId.SootFoundries; e.maxHealth = 60f; e.colliderRadius = 0.7f; e.tags = DamageTags.Armored;
                    e.wanderSpeed = 0.6f; e.chaseSpeed = 1.25f; e.attackDamage = 18f; e.attackRange = 1.5f;
                    e.attackWindup = 0.85f; e.attackActive = 0.2f; e.attackRecover = 1.0f; e.attackCooldown = 1.4f;
                    e.knockbackResistance = 0.85f; e.killWaxMin = 13; e.killWaxMax = 15; e.shardDrop = 6;
                    break;
                case EnemyKind.Bellows: // shoots soot (-10 wax)
                    e.homeRegion = RegionId.SootFoundries; e.maxHealth = 24f; e.colliderRadius = 0.5f;
                    e.wanderSpeed = 0.4f; e.chaseSpeed = 1.0f; e.attackDamage = 10f; e.attackRange = 6f;
                    e.attackWindup = 0.7f; e.attackActive = 0.1f; e.attackRecover = 0.8f; e.attackCooldown = 2.2f;
                    e.projectileSpeed = 5.5f; e.knockbackResistance = 0.5f;
                    e.killWaxMin = 7; e.killWaxMax = 9; e.shardDrop = 4;
                    break;
                case EnemyKind.Reflection: // copies your last 3 actions
                    e.homeRegion = RegionId.GlassCathedral; e.maxHealth = 30f; e.colliderRadius = 0.45f;
                    e.tags = DamageTags.Mirror | DamageTags.Hollow;
                    e.wanderSpeed = 0.8f; e.chaseSpeed = 2.4f; e.attackDamage = 10f; e.attackRange = 1.4f;
                    e.attackWindup = 0.35f; e.attackActive = 0.2f; e.attackRecover = 0.5f; e.attackCooldown = 1.1f;
                    e.killWaxMin = 9; e.killWaxMax = 11; e.shardDrop = 5;
                    break;
                case EnemyKind.Unlit: // drift toward the strongest light
                    e.homeRegion = RegionId.DeepHollow; e.maxHealth = 34f; e.colliderRadius = 0.5f; e.tags = DamageTags.Hollow;
                    e.wanderSpeed = 0.5f; e.chaseSpeed = 1.15f; e.attackDamage = 0f; e.contactDamage = 12f; e.attackRange = 0.8f;
                    e.attackCooldown = 1.0f; e.knockbackResistance = 0.6f;
                    e.killWaxMin = 10; e.killWaxMax = 12; e.shardDrop = 5;
                    break;
                default: // Roi Creux: boss in 3 phases (BossConfigSO)
                    e.homeRegion = RegionId.HearthChamber; e.maxHealth = 780f; e.colliderRadius = 1.4f; e.tags = DamageTags.Hollow;
                    e.wanderSpeed = 0f; e.chaseSpeed = 1.4f; e.contactDamage = 10f; e.knockbackResistance = 1f;
                    e.killWaxMin = 15; e.killWaxMax = 15; e.shardDrop = 0; e.countsForAvoided = false;
                    break;
            }
        }

        public static HazardSO CreateHazard(HazardKind kind)
        {
            var h = ScriptableObject.CreateInstance<HazardSO>();
            ApplyHazard(h, kind);
            return h;
        }

        /// <summary>PDF §2.2 / §2.3 obstacle numbers + G06 proposals.</summary>
        public static void ApplyHazard(HazardSO h, HazardKind kind)
        {
            h.name = "Hazard_" + kind;
            h.kind = kind;
            h.nameKey = "hazard." + kind.ToString().ToLowerInvariant();
            h.damage = 0f; h.telegraphSeconds = 0f; h.lightEffectSeconds = 0f; h.damageTakenMultiplier = 1f;
            h.moveMultiplier = 1f; h.pushSpeed = 0f; h.statusSeconds = 0f;
            switch (kind)
            {
                case HazardKind.WaxGeyser: // 10 dégâts, annoncés 1,2 s
                    h.damage = 10f; h.telegraphSeconds = 1.2f; h.period = 3.6f; h.activeSeconds = 0.45f; h.radius = 0.85f;
                    break;
                case HazardKind.LightEaterBloom: // lumière coupée 3 s
                    h.lightEffectSeconds = 3f; h.telegraphSeconds = 0.35f; h.period = 4f; h.radius = 1.6f;
                    break;
                case HazardKind.Press: // 35 dégâts
                    h.damage = 35f; h.telegraphSeconds = 0.9f; h.period = 2.8f; h.activeSeconds = 0.25f; h.radius = 1.1f; // a press blocks a 2-3 wide lane
                    break;
                case HazardKind.SteamJet:
                    h.damage = 6f; h.telegraphSeconds = 0.6f; h.period = 3.2f; h.activeSeconds = 1.1f; h.radius = 0.7f; h.pushSpeed = 5f;
                    break;
                case HazardKind.Conveyor:
                    h.pushSpeed = 2.4f; h.period = 0f; h.activeSeconds = 0f;
                    break;
                case HazardKind.Soot: // -10 cire ; dégâts x1,25 et perte de lumière pendant 1,5 s
                    h.damage = 10f; h.damageTakenMultiplier = 1.25f; h.lightEffectSeconds = 1.5f; h.statusSeconds = 1.5f; h.radius = 0.3f;
                    break;
                case HazardKind.Laser: // 25 dégâts
                    h.damage = 25f; h.telegraphSeconds = 0.8f; h.period = 3.2f; h.activeSeconds = 1.3f; h.radius = 0.25f;
                    break;
                case HazardKind.GlassFloor: // casse sous la flamme cramoisie
                    h.damage = 12f; h.statusSeconds = 0.7f; h.radius = 0.5f;
                    break;
                default: // Cendre qui ralentit
                    h.moveMultiplier = 0.55f; h.radius = 1f;
                    break;
            }
        }

        public static PactSO[] CreatePacts()
        {
            var pacts = new PactSO[4];
            for (int i = 0; i < pacts.Length; i++)
            {
                pacts[i] = ScriptableObject.CreateInstance<PactSO>();
                ApplyPact(pacts[i], i);
            }
            return pacts;
        }

        /// <summary>G11: the PDF's own example first, then three proposals.</summary>
        public static void ApplyPact(PactSO p, int index)
        {
            p.index = index;
            p.name = "Pact_" + index;
            p.nameKey = "pact." + index;
            p.descriptionKey = "pact." + index + ".desc";
            p.fromPdf = index == 0;
            switch (index)
            {
                case 0: // PDF: +30 cire maximale, perte x1,5
                    p.gainStat = PactStat.MaxWax; p.gainValue = 30f; p.costStat = PactStat.WaxLoss; p.costValue = 1.5f;
                    p.offeredIn = RegionId.DeepHollow;
                    break;
                case 1:
                    p.gainStat = PactStat.DamageDealt; p.gainValue = 1.3f; p.costStat = PactStat.MaxWax; p.costValue = -20f;
                    p.offeredIn = RegionId.DeepHollow;
                    break;
                case 2:
                    p.gainStat = PactStat.DetectionRadius; p.gainValue = 0.75f; p.costStat = PactStat.LightRadius; p.costValue = 0.8f;
                    p.offeredIn = RegionId.HearthChamber;
                    break;
                default:
                    p.gainStat = PactStat.ShardGain; p.gainValue = 1.5f; p.costStat = PactStat.HitWaxPenalty; p.costValue = 4f;
                    p.offeredIn = RegionId.HearthChamber;
                    break;
            }
        }

        public static ShopItemSO[] CreateShop()
        {
            var items = new ShopItemSO[2 + WickfallCounts.Flames * 2];
            items[0] = Item("candle", ShopItemKind.WaxCandle, FlameId.Amber, 1, 25, 25f, 3);
            items[1] = Item("salve", ShopItemKind.SootSalve, FlameId.Amber, 1, 15, 1f, 3);
            for (int i = 0; i < WickfallCounts.Flames; i++)
            {
                var flame = (FlameId)i;
                items[2 + i * 2] = Item("dmg_" + FlameKey(flame), ShopItemKind.FlameDamage, flame, 1, 60, 0.15f, 2);
                items[3 + i * 2] = Item("art_" + FlameKey(flame), ShopItemKind.FlameArtCost, flame, 1, 50, 0.15f, 2);
            }
            return items;
        }

        static ShopItemSO Item(string id, ShopItemKind kind, FlameId flame, int tier, int price, float value, int maxOwned)
        {
            var s = ScriptableObject.CreateInstance<ShopItemSO>();
            s.name = "Shop_" + id;
            s.id = id;
            s.kind = kind;
            s.flame = flame;
            s.tier = tier;
            s.price = price;
            s.value = value;
            s.maxOwned = maxOwned;
            s.nameKey = "shop." + (kind == ShopItemKind.FlameDamage ? "dmg" : kind == ShopItemKind.FlameArtCost ? "art" : id);
            s.descriptionKey = s.nameKey + ".desc";
            return s;
        }
    }
}
