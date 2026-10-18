using System.IO;
using NUnit.Framework;
using UnityEngine;
using Wickfall.Meta;
using Wickfall.Presentation;

namespace Wickfall.Tests
{
    /// <summary>PDF §4 SaveSystem (encrypted, G25), endings (PDF §1), the boss phases (PDF §4), presentation phases.</summary>
    public class SaveAndRulesTests
    {
        string folder;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "wickfall_tests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        static SaveData Sample()
        {
            var data = SaveData.NewGame();
            data.region = (int)RegionId.SootFoundries;
            data.position = new Vector2(12.5f, -3f);
            data.wax = 42.25f;
            data.relicsMask = 0b101;
            data.pactsMask = 1;
            data.endingsMask = 1 << (int)EndingId.B;
            data.shards = 77;
            data.worldFlags.Add("R3/gate_1");
            return data;
        }

        [Test]
        public void Save_RoundTripsThroughEncryption()
        {
            var bytes = SaveCodec.EncodeSave(Sample());
            Assert.IsTrue(SaveCodec.TryDecodeSave(bytes, out var loaded));
            Assert.AreEqual((int)RegionId.SootFoundries, loaded.region);
            Assert.AreEqual(42.25f, loaded.wax, 1e-4f);
            Assert.AreEqual(new Vector2(12.5f, -3f), loaded.position);
            Assert.AreEqual(0b101, loaded.relicsMask);
            Assert.AreEqual(1, loaded.pactsMask);
            Assert.IsTrue(loaded.HasEnding(EndingId.B));
            Assert.AreEqual("R3/gate_1", loaded.worldFlags[0]);
        }

        [Test]
        public void Save_IsNotPlainText()
        {
            var bytes = SaveCodec.EncodeSave(Sample());
            string asText = System.Text.Encoding.UTF8.GetString(bytes);
            StringAssert.DoesNotContain("relicsMask", asText);
            StringAssert.DoesNotContain("R3/gate_1", asText);
        }

        [Test]
        public void Save_TamperingIsDetected()
        {
            var bytes = SaveCodec.EncodeSave(Sample());
            bytes[bytes.Length - 5] ^= 0x5A;
            Assert.IsFalse(SaveCodec.TryDecodeSave(bytes, out _));
            Assert.IsFalse(SaveCodec.TryDecodeSave(new byte[] { 1, 2, 3 }, out _));
            Assert.IsFalse(SaveCodec.TryDecodeSave(null, out _));
        }

        [Test]
        public void Save_CorruptedFileFallsBackToBackup()
        {
            var store = new SaveStore(folder);
            var first = Sample();
            first.shards = 10;
            store.Write(first);
            var second = Sample();
            second.shards = 20;
            store.Write(second); // the first save becomes the backup

            File.WriteAllBytes(store.SavePath, new byte[] { 0, 1, 2, 3, 4 });
            Assert.AreEqual(SaveLoadResult.RestoredFromBackup, store.TryLoad(out var loaded));
            Assert.AreEqual(10, loaded.shards);
            Assert.AreEqual(SaveLoadResult.Loaded, store.TryLoad(out _), "the backup was copied back over the damaged save");
        }

        [Test]
        public void Save_NoFilesMeansNoSave()
        {
            Assert.AreEqual(SaveLoadResult.NoSave, new SaveStore(folder).TryLoad(out _));
        }

        [Test]
        public void Settings_AreSeparateAndPlain()
        {
            var store = new SaveStore(folder);
            store.SaveSettings(new SettingsData { language = "en", musicVolume = 0.25f });
            var loaded = store.LoadSettings();
            Assert.AreEqual("en", loaded.language);
            Assert.AreEqual(0.25f, loaded.musicVolume, 1e-4f);
            StringAssert.Contains("musicVolume", File.ReadAllText(store.SettingsPath));
        }

        [Test]
        public void EndingC_NeedsAllSixRelicsAndTheKingSpared()
        {
            Assert.IsTrue(ProgressRules.CanPassTheWick(6, true));
            Assert.IsFalse(ProgressRules.CanPassTheWick(5, true));
            Assert.IsFalse(ProgressRules.CanPassTheWick(6, false));

            var save = SaveData.NewGame();
            save.relicsMask = 0b111111;
            save.kingDecided = true;
            save.kingSpared = true;
            Assert.IsTrue(ProgressRules.CanPassTheWick(save));
            save.kingSpared = false;
            Assert.IsFalse(ProgressRules.CanPassTheWick(save));
        }

        [Test]
        public void Boss_EachPhaseRemovesOneSupport()
        {
            var config = ScriptableObject.CreateInstance<BossConfigSO>();
            Assert.IsFalse(ProgressRules.TrimDisabled(1, config));
            Assert.IsTrue(ProgressRules.TrimDisabled(2, config), "PDF: pas de mode faible");
            Assert.IsTrue(float.IsPositiveInfinity(ProgressRules.MaxWaxCap(2, config)));
            Assert.AreEqual(40f, ProgressRules.MaxWaxCap(3, config), "PDF: cire maximale à 40");
            Assert.IsTrue(ProgressRules.WaxPoolsDisabled(1, config));
        }

        [Test]
        public void Modifiers_PdfPactIsPlus30MaxWax_AndLossTimes1Point5()
        {
            var db = GameDatabase.CreateDefault();
            var save = SaveData.NewGame();
            save.pactsMask = 1; // pact 0 = the PDF's example
            var m = PlayerModifiers.Compute(save, db);
            Assert.AreEqual(130f, m.MaxWax, 1e-4f);
            Assert.AreEqual(1.5f, m.WaxLossMultiplier, 1e-4f);
        }

        [Test]
        public void Modifiers_RelicsGiveDodgeGripWaxAndASlot()
        {
            var db = GameDatabase.CreateDefault();
            var save = SaveData.NewGame();
            Assert.AreEqual(4, PlayerModifiers.Compute(save, db).FlameSlots);
            save.relicsMask = 0b1111;
            var m = PlayerModifiers.Compute(save, db);
            Assert.AreEqual(2, m.DodgeCharges);
            Assert.IsTrue(m.WallGrip);
            Assert.AreEqual(120f, m.MaxWax, 1e-4f);
            Assert.AreEqual(5, m.FlameSlots);
        }

        [Test]
        public void Presentation_PhasesNeverRemoveContent_AndBuildOnEachOther()
        {
            var dev = PresentationConfig.FeaturesFor(PresentationPhase.Development);
            var anim = PresentationConfig.FeaturesFor(PresentationPhase.Animation2D);
            var test = PresentationConfig.FeaturesFor(PresentationPhase.Testing);
            Assert.AreEqual(dev, anim & dev, "phase 5 keeps everything phase 4 shows");
            Assert.AreEqual(anim, test & anim, "phase 6 keeps everything phase 5 shows");
            Assert.IsTrue((dev & Feature.Region1) != 0 && (dev & Feature.Region2) != 0 && (dev & Feature.Region3) == 0);
            Assert.IsTrue((test & Feature.Boss) != 0);
            var final = PresentationConfig.FeaturesFor(PresentationPhase.FinalProduct);
            Assert.IsTrue((final & Feature.DevOverlay) == 0, "no debug tools in the final product");
            Assert.AreEqual("Showcase_Concept", PresentationConfig.StartSceneFor(PresentationPhase.Conception));
            Assert.AreEqual("Showcase_Design", PresentationConfig.StartSceneFor(PresentationPhase.Design));
            Assert.AreEqual("MainMenu", PresentationConfig.StartSceneFor(PresentationPhase.Release));
        }

        [Test]
        public void EnteringARegion_GuaranteesTheFlamesOfEarlierRegions()
        {
            int Mask(params FlameId[] flames) { int m = 0; foreach (var f in flames) m |= 1 << (int)f; return m; }
            Assert.AreEqual(Mask(FlameId.Amber), ProgressRules.FlamesGuaranteedOnEntering(RegionId.TallowFields));
            Assert.AreEqual(Mask(FlameId.Amber, FlameId.Blue), ProgressRules.FlamesGuaranteedOnEntering(RegionId.Wickwood));
            Assert.AreEqual(Mask(FlameId.Amber, FlameId.Blue, FlameId.Green), ProgressRules.FlamesGuaranteedOnEntering(RegionId.SootFoundries),
                "R3's Slagborn need green (PDF): R2's flame is guaranteed");
            Assert.AreEqual(Mask(FlameId.Amber, FlameId.Blue, FlameId.Green, FlameId.White, FlameId.Crimson),
                ProgressRules.FlamesGuaranteedOnEntering(RegionId.GlassCathedral), "G03: crimson on entering R4");
        }

        [Test]
        public void StateMachine_ChangesWithoutNesting()
        {
            var owner = new Owner();
            var fsm = new StateMachine<Owner>(owner);
            owner.Fsm = fsm;
            var a = fsm.Add(new Hop("a"));
            var b = fsm.Add(new Hop("b"));
            a.Next = b; // a hands over to b from inside its own Enter
            fsm.Change(a);
            Assert.AreEqual("b", fsm.CurrentName);
            Assert.AreEqual("a+ a- b+", owner.Log.Trim(), "the queued change runs after a's Enter finishes");
            Assert.AreSame(a, fsm.Previous);
        }

        class Owner
        {
            public string Log = "";
            public StateMachine<Owner> Fsm;
        }

        class Hop : State<Owner>
        {
            readonly string id;
            public State<Owner> Next;
            public Hop(string id) => this.id = id;
            public override string Name => id;

            public override void Enter()
            {
                Owner.Log += id + "+ ";
                if (Next != null)
                    Owner.Fsm.Change(Next);
            }

            public override void Exit() => Owner.Log += id + "- ";
        }
    }
}
