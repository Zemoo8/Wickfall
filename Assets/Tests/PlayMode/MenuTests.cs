using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Wickfall.Meta;
using Wickfall.UI;

namespace Wickfall.Tests
{
    /// <summary>M7 menus: region select (G10), the endings gallery, credits, options.</summary>
    public class MenuTests
    {
        MainMenu menu;

        IEnumerator OpenMenu(SaveData save)
        {
            PlayModeFixture.UseTestSaveFolder();
            if (save != null)
                new SaveStore(PlayModeFixture.TestSaveFolder).Write(save);
            AudioListener.volume = 0f;
            SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
            yield return null; // the previous scene's menu is gone from here on
            float t = 0f;
            menu = null;
            while ((menu == null || !menu.Lines.Any()) && t < 10f)
            {
                menu = Object.FindFirstObjectByType<MainMenu>();
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(menu, "the main menu");
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            var core = SceneManager.GetSceneByName(SceneNames.Core);
            if (core.isLoaded)
                yield return SceneManager.UnloadSceneAsync(core);
            SaveStore.DirectoryOverride = null;
        }

        [UnityTest]
        public IEnumerator FreshStart_HasNoContinue_NoRegionSelect_NoEndings()
        {
            yield return OpenMenu(null);
            var keys = menu.Lines.Select(l => l.key).ToList();
            CollectionAssert.DoesNotContain(keys, "menu.continue");
            CollectionAssert.DoesNotContain(keys, "menu.regionselect");
            CollectionAssert.DoesNotContain(keys, "menu.endings");
            CollectionAssert.Contains(keys, "menu.newgame");
            CollectionAssert.Contains(keys, "menu.options");
            CollectionAssert.Contains(keys, "menu.credits");
            CollectionAssert.Contains(keys, "menu.howto");
            menu.ShowHowTo();
            yield return null;
            Assert.AreEqual(9, menu.Lines.Count(l => l.key.StartsWith("=")), "the controls and the rule");
        }

        [UnityTest]
        public IEnumerator RegionSelect_OpensOnlyClearedRegions_WithTheirBestRank()
        {
            var save = SaveData.NewGame();
            for (int i = 0; i < 3; i++)
            {
                save.regions[i].cleared = true;
                save.regions[i].bestScore = 1500 + i;
                save.regions[i].bestRank = (int)Rank.Flamme;
            }
            save.endingsMask = (1 << (int)EndingId.A) | (1 << (int)EndingId.C);
            yield return OpenMenu(save);
            var keys = menu.Lines.Select(l => l.key).ToList();
            CollectionAssert.Contains(keys, "menu.continue");
            CollectionAssert.Contains(keys, "menu.regionselect");
            CollectionAssert.Contains(keys, "menu.endings");

            menu.ShowRegions();
            yield return null;
            var regions = menu.Lines.Where(l => l.key.StartsWith("=")).ToList();
            Assert.AreEqual(6, regions.Count, "six regions listed");
            Assert.AreEqual(3, regions.Count(l => l.interactable), "only the three cleared ones open");
            StringAssert.Contains("1500", regions[0].key, "best score shown");
            StringAssert.Contains(Loc.Get("rank.flamme"), regions[0].key, "best rank shown");

            menu.ShowEndings();
            yield return null;
            var endings = menu.Lines.Where(l => l.key.StartsWith("=")).Select(l => l.key.Substring(1)).ToList();
            Assert.AreEqual(Loc.Get("ending.a.title"), endings[0]);
            Assert.AreEqual(Loc.Get("ending.locked"), endings[1], "B not found yet");
            Assert.AreEqual(Loc.Get("ending.c.title"), endings[2]);

            menu.ShowCredits();
            yield return null;
            Assert.AreEqual(8, menu.Lines.Count(l => l.key.StartsWith("=")));
            menu.ShowMain();
            yield return null;
            Assert.AreEqual(MainMenu.Page.Main, menu.Current);
        }

        [UnityTest]
        public IEnumerator Options_OpenFromTheMenu()
        {
            yield return OpenMenu(null);
            Assert.IsNotNull(OptionsView.Instance, "the options view lives in Core");
            OptionsView.Instance.Open();
            yield return null;
            Assert.IsTrue(OptionsView.Instance.IsOpen);
        }
    }
}
