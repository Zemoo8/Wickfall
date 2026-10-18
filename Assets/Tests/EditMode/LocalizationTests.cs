using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Wickfall.Tests
{
    /// <summary>
    /// The game ships in French (default) and English: every text key the code and the data use exists
    /// in both languages, and no key is defined twice.
    /// </summary>
    public class LocalizationTests
    {
        const string StringsPath = "Assets/Resources/Localization/Strings.txt";

        static Dictionary<string, string[]> Table(out List<string> duplicates)
        {
            var table = new Dictionary<string, string[]>();
            duplicates = new List<string>();
            foreach (string line in File.ReadAllLines(StringsPath))
            {
                if (line.StartsWith("#") || !line.Contains("\t"))
                    continue;
                var parts = line.Split('\t');
                if (table.ContainsKey(parts[0]))
                    duplicates.Add(parts[0]);
                table[parts[0]] = parts;
            }
            return table;
        }

        static IEnumerable<string> KeysInCode()
        {
            // literal keys: Loc.Get("x.y"), promptKey = "x.y", TitleKey/BodyKey = "x.y", OptionKeys = { "x.y", ... }
            var patterns = new[]
            {
                new Regex(@"Loc\.Get\(""([a-z0-9_.]+)""\)"),
                new Regex(@"(?:promptKey|TitleKey|BodyKey|textKey)\s*=\s*""([a-z0-9_.]+)"""),
                new Regex(@"ItemCard\.Show\?\.Invoke\([^,]*,\s*""([a-z0-9_.]+)"""),
            };
            var options = new Regex(@"OptionKeys\s*=\s*new\[\]\s*\{([^}]*)\}");
            var literal = new Regex(@"""([a-z0-9_.]+)""");
            foreach (string path in Directory.GetFiles("Assets/Scripts/Runtime", "*.cs", SearchOption.AllDirectories))
            {
                string code = File.ReadAllText(path);
                foreach (var p in patterns)
                    foreach (Match m in p.Matches(code))
                        yield return m.Groups[1].Value;
                foreach (Match m in options.Matches(code))
                    foreach (Match k in literal.Matches(m.Groups[1].Value))
                        yield return k.Groups[1].Value;
            }
        }

        static IEnumerable<string> KeysInData()
        {
            var db = Db.Data;
            foreach (var f in db.flames) { yield return f.nameKey; yield return f.roleKey; }
            foreach (var r in db.relics) { yield return r.nameKey; yield return r.descriptionKey; }
            foreach (var p in db.pacts) { yield return p.nameKey; yield return p.descriptionKey; }
            foreach (var r in db.regions) yield return r.nameKey;
        }

        [Test]
        public void EveryKey_ExistsInFrenchAndEnglish_AndNoneIsDuplicated()
        {
            var table = Table(out var duplicates);
            Assert.IsEmpty(duplicates, "keys defined twice: " + string.Join(", ", duplicates));
            var missing = KeysInCode().Concat(KeysInData()).Where(k => !string.IsNullOrEmpty(k)).Distinct()
                .Where(k => !table.TryGetValue(k, out var row) || row.Length < 3 || string.IsNullOrWhiteSpace(row[1]) || string.IsNullOrWhiteSpace(row[2]))
                .OrderBy(k => k).ToList();
            Assert.IsEmpty(missing, "keys without French and English text: " + string.Join(", ", missing));
        }

        [Test]
        public void FrenchIsTheDefault_AndBothLanguagesSwitch()
        {
            Assert.AreEqual(UI.Loc.French, UI.Loc.SystemDefault(), "French is the default on every system");
            string fr = UI.Loc.Get("menu.newgame", UI.Loc.French);
            string en = UI.Loc.Get("menu.newgame", UI.Loc.English);
            Assert.AreEqual("Nouvelle partie", fr);
            Assert.AreEqual("New game", en);
        }
    }
}
