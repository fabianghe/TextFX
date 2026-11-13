using System;
using System.Collections.Generic;
using UnityEngine;

namespace TextFX
{
    /// <summary>A named look for marked-up words: optional colour and bold, plus an effect.</summary>
    [Serializable]
    public class TextFxStyle
    {
        [SerializeField, Tooltip("Written as <link=\"name\">word</link>, or <fx=name>word</fx> through TextFxMarkup")]
        string name;
        [SerializeField, Tooltip("TextFxMarkup colours the words with Color")]
        bool setColor = true;
        [SerializeField] Color color = new Color(1f, 0.87f, 0.2f);
        [SerializeField, Tooltip("TextFxMarkup makes the words bold")]
        bool bold;
        [SerializeField] TextEffectSettings effect = new TextEffectSettings();

        public TextFxStyle() { }

        public TextFxStyle(string name, TextEffectSettings effect, Color? color = null, bool bold = false)
        {
            this.name = name;
            this.effect = effect ?? new TextEffectSettings(TextEffect.None);
            setColor = color.HasValue;
            if (color.HasValue)
                this.color = color.Value;
            this.bold = bold;
        }

        public string Name => name;
        public bool SetColor => setColor;
        public Color Color => color;
        public bool Bold => bold;
        public TextEffectSettings Effect => effect;
    }

    /// <summary>
    /// Named styles for marked-up words. A TextFxAnimator with no sheet of its own, and TextFxMarkup.Format without a sheet,
    /// use the one at Resources/TextFxStyleSheet.
    /// </summary>
    [CreateAssetMenu(menuName = "Text FX/Style Sheet", fileName = ResourcesPath)]
    public class TextFxStyleSheet : ScriptableObject
    {
        public const string ResourcesPath = "TextFxStyleSheet";

        [SerializeField, Tooltip("TextFxMarkup turns <tag>word</tag> into the first style and <tag=name>word</tag> into the named one")]
        string markupTag = "fx";
        [SerializeField, Tooltip("The first style is the one a bare <tag>word</tag> gets")]
        List<TextFxStyle> styles = new List<TextFxStyle>();

        static TextFxStyleSheet _default;
        static bool _defaultLoaded;

        string _cachedTag;
        internal string OpenPrefix;
        internal string OpenTag;
        internal string OpenNamedTag;
        internal string CloseTag;
        internal string CloseShortTag;

        /// <summary>The sheet at Resources/TextFxStyleSheet, or null if the project has none.</summary>
        public static TextFxStyleSheet Default
        {
            get
            {
                if (!_defaultLoaded || (_default == null && !ReferenceEquals(_default, null)))
                {
                    _default = Resources.Load<TextFxStyleSheet>(ResourcesPath);
                    _defaultLoaded = true;
                }

                return _default;
            }
        }

        /// <summary>Builds a sheet in code, e.g. for tests or tools.</summary>
        public static TextFxStyleSheet Create(string markupTag, params TextFxStyle[] styles)
        {
            var sheet = CreateInstance<TextFxStyleSheet>();
            sheet.markupTag = markupTag;
            sheet.styles.AddRange(styles);
            return sheet;
        }

        public string MarkupTag => markupTag;
        public int Count => styles.Count;
        public TextFxStyle this[int index] => styles[index];

        /// <summary>Index of the style with this name (case ignored), or -1.</summary>
        public int IndexOf(string name) => name == null ? -1 : IndexOf(name, 0, name.Length);

        /// <summary>Index of the style named by text[start..start+length) (case ignored), or -1. Allocates nothing.</summary>
        public int IndexOf(string text, int start, int length)
        {
            for (int i = 0; i < styles.Count; i++)
            {
                string styleName = styles[i].Name;
                if (!string.IsNullOrEmpty(styleName) && styleName.Length == length &&
                    string.Compare(text, start, styleName, 0, length, StringComparison.OrdinalIgnoreCase) == 0)
                    return i;
            }

            return -1;
        }

        internal void CacheTags()
        {
            if (ReferenceEquals(_cachedTag, markupTag) && OpenTag != null)
                return;

            _cachedTag = markupTag;
            string tag = string.IsNullOrEmpty(markupTag) ? "fx" : markupTag;
            OpenPrefix = "<" + tag;
            OpenTag = OpenPrefix + ">";
            OpenNamedTag = "<" + tag + "=";
            CloseTag = "</" + tag + ">";
            CloseShortTag = "<" + tag + "/>";
        }

        public event Action Changed;

        void OnValidate()
        {
            OpenTag = null;
            Changed?.Invoke();
        }
    }
}
