using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wickfall.World;

namespace Wickfall.Tests
{
    /// <summary>Checks on the generated level files (Assets/Resources/Levels/R*.txt).</summary>
    public class LevelFileTests
    {
        static IEnumerable<string> RegionLevels() =>
            Directory.GetFiles("Assets/Resources/Levels", "R*_*.txt").OrderBy(p => p);

        static Dictionary<string, string[]> Strings()
        {
            var table = new Dictionary<string, string[]>();
            foreach (string line in File.ReadAllLines("Assets/Resources/Localization/Strings.txt"))
            {
                if (line.StartsWith("#") || !line.Contains("\t"))
                    continue;
                var parts = line.Split('\t');
                table[parts[0]] = parts;
            }
            return table;
        }

        [Test]
        public void Inscriptions_AreNumbered0ToN_AndTranslated()
        {
            var strings = Strings();
            var problems = new List<string>();
            foreach (string path in RegionLevels())
            {
                var level = LevelData.Parse(File.ReadAllText(path));
                string name = Path.GetFileNameWithoutExtension(path);
                int region = int.Parse(name.Substring(1, name.IndexOf('_') - 1));
                var numbers = level.Entities().Where(e => e.kind.StartsWith("Inscription"))
                    .Select(e => e.kind == "Inscription" ? -1 : int.Parse(e.kind.Substring("Inscription".Length))).ToList();
                if (numbers.Contains(-1))
                    problems.Add($"{name}: an inscription without a number (its text would follow map order)");
                var sorted = numbers.Where(n => n >= 0).OrderBy(n => n).ToList();
                if (!sorted.SequenceEqual(Enumerable.Range(0, sorted.Count)))
                    problems.Add($"{name}: inscriptions {string.Join(",", sorted)} are not 0..{sorted.Count - 1}");
                foreach (int n in sorted)
                {
                    string key = $"inscription.r{region}.{n}";
                    if (!strings.TryGetValue(key, out var row) || row.Length < 3 || string.IsNullOrWhiteSpace(row[1]) || string.IsNullOrWhiteSpace(row[2]))
                        problems.Add($"{name}: {key} has no French and English text");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void EveryRegionLevel_HasAStartBrazierRelicBellExitAndBot()
        {
            var problems = new List<string>();
            foreach (string path in RegionLevels())
            {
                var level = LevelData.Parse(File.ReadAllText(path));
                var kinds = level.Entities().Select(e => e.kind).ToList();
                foreach (string required in new[] { "PlayerStart", "Brazier", "Relic", "GreatBell", "Exit" })
                    if (!kinds.Contains(required))
                        problems.Add($"{Path.GetFileName(path)}: no {required}");
                if (level.BotScript.Count == 0)
                    problems.Add($"{Path.GetFileName(path)}: no @bot route (prompt §15: one bot per region)");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
