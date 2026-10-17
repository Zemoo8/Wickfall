using System;
using UnityEditor;
using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Creates every tuning asset (PDF §4: "tout le réglage est dans des ScriptableObjects") and every
    /// event channel asset. An asset is only filled with the PDF's values when it is first created;
    /// after that the asset is the source and can be tuned in the Inspector.
    /// WICKFALL > Setup > Reset Data To PDF Values rewrites them from <see cref="PdfTables"/>.
    /// </summary>
    public static class DataFactory
    {
        public const string DatabasePath = "Assets/Resources/GameDatabase.asset";
        public const string EventsHubPath = "Assets/Resources/GameEvents.asset";

        public static void EnsureAll() => Build(false);

        [MenuItem("WICKFALL/Setup/Reset Data To PDF Values", priority = 20)]
        public static void ResetToPdf()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Reset data",
                    "Rewrite every flame, region, relic, enemy, hazard, pact and shop asset with the values from PdfTables? Tuning done in the Inspector is lost.",
                    "Reset", "Cancel"))
                return;
            Build(true);
        }

        static void Build(bool overwrite)
        {
            foreach (var folder in new[] { EditorPaths.Resources, EditorPaths.Events, EditorPaths.Config, EditorPaths.Flames,
                         EditorPaths.Regions, EditorPaths.Relics, EditorPaths.Enemies, EditorPaths.Hazards, EditorPaths.Pacts, EditorPaths.Shop })
                EditorPaths.EnsureFolder(folder);

            EnsureEvents();

            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            db.wax = Ensure<WaxConfig>(EditorPaths.Config + "/WaxConfig.asset", overwrite, _ => { });
            db.light = Ensure<LightConfig>(EditorPaths.Config + "/LightConfig.asset", overwrite, _ => { });
            db.dim = Ensure<DimConfig>(EditorPaths.Config + "/DimConfig.asset", overwrite, _ => { });
            db.balance = Ensure<BalanceSO>(EditorPaths.Config + "/Balance.asset", overwrite, _ => { });
            db.controls = Ensure<ControlsSO>(EditorPaths.Config + "/Controls.asset", overwrite, _ => { });
            db.score = Ensure<ScoreConfigSO>(EditorPaths.Config + "/ScoreConfig.asset", overwrite, _ => { });
            db.boss = Ensure<BossConfigSO>(EditorPaths.Config + "/BossConfig.asset", overwrite, _ => { });
            db.audioReactive = Ensure<AudioReactiveSO>(EditorPaths.Config + "/AudioReactive.asset", overwrite, _ => { });

            db.flames = new FlameTypeSO[WickfallCounts.Flames];
            for (int i = 0; i < WickfallCounts.Flames; i++)
            {
                var id = (FlameId)i;
                db.flames[i] = Ensure<FlameTypeSO>($"{EditorPaths.Flames}/Flame_{i + 1}_{id}.asset", overwrite, f => PdfTables.ApplyFlame(f, id));
            }

            db.regions = new RegionSO[WickfallCounts.Regions];
            for (int i = 0; i < WickfallCounts.Regions; i++)
            {
                var id = (RegionId)i;
                db.regions[i] = Ensure<RegionSO>($"{EditorPaths.Regions}/Region_{i + 1}_{id}.asset", overwrite, r => PdfTables.ApplyRegion(r, id));
            }

            db.relics = new RelicSO[WickfallCounts.Relics];
            for (int i = 0; i < WickfallCounts.Relics; i++)
            {
                int index = i;
                db.relics[i] = Ensure<RelicSO>($"{EditorPaths.Relics}/Relic_{i + 1}.asset", overwrite, r => PdfTables.ApplyRelic(r, index));
            }

            var kinds = (EnemyKind[])Enum.GetValues(typeof(EnemyKind));
            db.enemies = new EnemyTypeSO[kinds.Length];
            foreach (var kind in kinds)
                db.enemies[(int)kind] = Ensure<EnemyTypeSO>($"{EditorPaths.Enemies}/Enemy_{kind}.asset", overwrite, e => PdfTables.ApplyEnemy(e, kind));

            var hazardKinds = (HazardKind[])Enum.GetValues(typeof(HazardKind));
            db.hazards = new HazardSO[hazardKinds.Length];
            for (int i = 0; i < hazardKinds.Length; i++)
            {
                var kind = hazardKinds[i];
                db.hazards[i] = Ensure<HazardSO>($"{EditorPaths.Hazards}/Hazard_{kind}.asset", overwrite, h => PdfTables.ApplyHazard(h, kind));
            }

            var pactTemplates = PdfTables.CreatePacts();
            db.pacts = new PactSO[pactTemplates.Length];
            for (int i = 0; i < pactTemplates.Length; i++)
            {
                int index = i;
                db.pacts[i] = Ensure<PactSO>($"{EditorPaths.Pacts}/Pact_{i + 1}.asset", overwrite, p => PdfTables.ApplyPact(p, index));
            }

            var shopTemplates = PdfTables.CreateShop();
            db.shop = new ShopItemSO[shopTemplates.Length];
            for (int i = 0; i < shopTemplates.Length; i++)
            {
                var template = shopTemplates[i];
                db.shop[i] = Ensure<ShopItemSO>($"{EditorPaths.Shop}/{template.name}.asset", overwrite, s =>
                {
                    EditorUtility.CopySerialized(template, s);
                    s.name = template.name;
                });
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        static T Ensure<T>(string path, bool overwrite, Action<T> fill) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                fill(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            else if (overwrite)
            {
                // Reset to the type's defaults first so no stale tuning survives, then apply the PDF table.
                var fresh = ScriptableObject.CreateInstance<T>();
                EditorUtility.CopySerialized(fresh, asset);
                UnityEngine.Object.DestroyImmediate(fresh);
                fill(asset);
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        static void EnsureEvents()
        {
            var hub = AssetDatabase.LoadAssetAtPath<GameEvents>(EventsHubPath);
            if (hub == null)
            {
                hub = ScriptableObject.CreateInstance<GameEvents>();
                AssetDatabase.CreateAsset(hub, EventsHubPath);
            }

            foreach (var field in GameEvents.ChannelFields())
            {
                string name = char.ToUpperInvariant(field.Name[0]) + field.Name.Substring(1);
                string path = $"{EditorPaths.Events}/{name}.asset";
                var channel = AssetDatabase.LoadAssetAtPath(path, field.FieldType) as EventChannelBase;
                if (channel == null)
                {
                    channel = (EventChannelBase)ScriptableObject.CreateInstance(field.FieldType);
                    AssetDatabase.CreateAsset(channel, path);
                }
                field.SetValue(hub, channel);
            }
            EditorUtility.SetDirty(hub);
        }
    }
}
