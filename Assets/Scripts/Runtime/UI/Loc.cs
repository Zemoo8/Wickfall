using System.Collections.Generic;
using UnityEngine;

namespace Wickfall.UI
{
    /// <summary>
    /// French and English text (prompt §12). Every player-facing string lives in
    /// Resources/Localization/Strings.txt: one key per line, then the French text, then the English
    /// text, separated by tabs. French is the default; the first launch follows the system language.
    /// </summary>
    public static class Loc
    {
        public const string French = "fr";
        public const string English = "en";
        public const string ResourcePath = "Localization/Strings";

        static Dictionary<string, string> fr;
        static Dictionary<string, string> en;
        static string language = French;

        public static string Language => language;
        public static bool IsFrench => language == French;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            fr = null;
            en = null;
            language = French;
        }

        static void EnsureLoaded()
        {
            if (fr != null)
                return;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            LoadFrom(asset != null ? asset.text : string.Empty);
        }

        /// <summary>Parses a table (tests call this with their own text).</summary>
        public static void LoadFrom(string table)
        {
            fr = new Dictionary<string, string>(512);
            en = new Dictionary<string, string>(512);
            if (string.IsNullOrEmpty(table))
                return;
            var lines = table.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#')
                    continue;
                var cells = line.Split('\t');
                if (cells.Length < 3 || cells[0].Length == 0)
                    continue;
                string key = cells[0].Trim();
                fr[key] = Unescape(cells[1]);
                en[key] = Unescape(cells[2]);
            }
        }

        static string Unescape(string cell) => cell.Replace("\\n", "\n");

        public static void SetLanguage(string code, bool notify = true)
        {
            string next = code == English ? English : French;
            if (next == language)
                return;
            language = next;
            if (notify)
                Events.LanguageChanged.Raise();
        }

        /// <summary>The language a first launch should use: French unless the system is English.</summary>
        public static string SystemDefault() =>
            Application.systemLanguage == SystemLanguage.English ? English : French;

        public static string Get(string key)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            var table = language == English ? en : fr;
            if (table.TryGetValue(key, out string value))
                return value;
            // Fall back to the other language, then to the key itself so missing text is visible.
            var other = language == English ? fr : en;
            return other.TryGetValue(key, out value) ? value : "[" + key + "]";
        }

        public static string Get(string key, string lang)
        {
            EnsureLoaded();
            var table = lang == English ? en : fr;
            return table.TryGetValue(key, out string value) ? value : null;
        }

        public static bool Has(string key)
        {
            EnsureLoaded();
            return fr.ContainsKey(key) && en.ContainsKey(key);
        }

        public static string Format(string key, object a) => string.Format(Get(key), a);
        public static string Format(string key, object a, object b) => string.Format(Get(key), a, b);

        /// <summary>Every key, for the localization tests.</summary>
        public static IEnumerable<string> Keys
        {
            get
            {
                EnsureLoaded();
                return fr.Keys;
            }
        }

        public static IEnumerable<string> EnglishKeys
        {
            get
            {
                EnsureLoaded();
                return en.Keys;
            }
        }
    }
}
