using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// Every tuning asset in one place (Resources/GameDatabase). Gameplay reads data through
    /// <see cref="Db"/>; none of it is hard-coded (PDF §4: "tout le réglage est dans des ScriptableObjects").
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Game Database", fileName = "GameDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        public const string ResourcePath = "GameDatabase";

        public WaxConfig wax;
        public LightConfig light;
        public DimConfig dim;
        public BalanceSO balance;
        public ControlsSO controls;
        public ScoreConfigSO score;
        public BossConfigSO boss;
        public AudioReactiveSO audioReactive;

        [Tooltip("Indexed by FlameId.")]
        public FlameTypeSO[] flames = new FlameTypeSO[WickfallCounts.Flames];
        [Tooltip("Indexed by RegionId.")]
        public RegionSO[] regions = new RegionSO[WickfallCounts.Regions];
        [Tooltip("Indexed by relic index (one per region).")]
        public RelicSO[] relics = new RelicSO[WickfallCounts.Relics];
        [Tooltip("Indexed by EnemyKind.")]
        public EnemyTypeSO[] enemies = new EnemyTypeSO[10];
        public HazardSO[] hazards = new HazardSO[10];
        public PactSO[] pacts = new PactSO[0];
        public ShopItemSO[] shop = new ShopItemSO[0];

        public FlameTypeSO Flame(FlameId id) => flames[(int)id];
        public RegionSO Region(RegionId id) => regions[(int)id];
        public EnemyTypeSO Enemy(EnemyKind kind) => enemies[(int)kind];

        public HazardSO Hazard(HazardKind kind)
        {
            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] != null && hazards[i].kind == kind)
                    return hazards[i];
            }
            return null;
        }

        /// <summary>A database filled with default-valued configs, for tests and a missing asset.</summary>
        public static GameDatabase CreateDefault()
        {
            var db = CreateInstance<GameDatabase>();
            db.name = "GameDatabase (defaults)";
            db.wax = CreateInstance<WaxConfig>();
            db.light = CreateInstance<LightConfig>();
            db.dim = CreateInstance<DimConfig>();
            db.balance = CreateInstance<BalanceSO>();
            db.controls = CreateInstance<ControlsSO>();
            db.score = CreateInstance<ScoreConfigSO>();
            db.boss = CreateInstance<BossConfigSO>();
            db.audioReactive = CreateInstance<AudioReactiveSO>();
            for (int i = 0; i < db.flames.Length; i++)
                db.flames[i] = PdfTables.CreateFlame((FlameId)i);
            for (int i = 0; i < db.regions.Length; i++)
                db.regions[i] = PdfTables.CreateRegion((RegionId)i);
            for (int i = 0; i < db.relics.Length; i++)
                db.relics[i] = PdfTables.CreateRelic(i);
            for (int i = 0; i < db.enemies.Length; i++)
                db.enemies[i] = PdfTables.CreateEnemy((EnemyKind)i);
            var kinds = (HazardKind[])System.Enum.GetValues(typeof(HazardKind));
            db.hazards = new HazardSO[kinds.Length];
            for (int i = 0; i < kinds.Length; i++)
                db.hazards[i] = PdfTables.CreateHazard(kinds[i]);
            db.pacts = PdfTables.CreatePacts();
            db.shop = PdfTables.CreateShop();
            return db;
        }
    }

    /// <summary>Static access: <c>Db.Wax.maxWax</c>, <c>Db.Flame(FlameId.Blue)</c>.</summary>
    public static class Db
    {
        static GameDatabase data;

        public static GameDatabase Data
        {
            get
            {
                if (data == null)
                {
                    data = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);
                    if (data == null)
                        data = GameDatabase.CreateDefault();
                }
                return data;
            }
        }

        public static void Use(GameDatabase value) => data = value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => data = null;

        public static WaxConfig Wax => Data.wax;
        public static LightConfig Light => Data.light;
        public static DimConfig Dim => Data.dim;
        public static BalanceSO Balance => Data.balance;
        public static ControlsSO Controls => Data.controls;
        public static ScoreConfigSO Score => Data.score;
        public static BossConfigSO Boss => Data.boss;
        public static AudioReactiveSO AudioReactive => Data.audioReactive;
        public static FlameTypeSO Flame(FlameId id) => Data.Flame(id);
        public static RegionSO Region(RegionId id) => Data.Region(id);
        public static EnemyTypeSO Enemy(EnemyKind kind) => Data.Enemy(kind);
        public static HazardSO Hazard(HazardKind kind) => Data.Hazard(kind);
    }
}
