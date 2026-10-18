using TMPro;
using UnityEngine;

namespace Wickfall.UI
{
    /// <summary>Shows a localization key in the current language and updates live when the language changes.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        public string key;
        public bool upperCase;
        TMP_Text text;

        void Awake() => text = GetComponent<TMP_Text>();

        void OnEnable()
        {
            Events.LanguageChanged.Register(Refresh);
            Refresh();
        }

        void OnDisable() => Events.LanguageChanged.Unregister(Refresh);

        public void SetKey(string value)
        {
            key = value;
            Refresh();
        }

        public void Refresh()
        {
            if (text == null)
                text = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(key))
                return;
            string value = Loc.Get(key);
            text.text = upperCase ? value.ToUpperInvariant() : value;
        }
    }
}
