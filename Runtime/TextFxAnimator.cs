using System;
using TMPro;
using UnityEngine;

namespace TextFX
{
    /// <summary>
    /// Animates the letters of a TextMeshPro text: all of them with <see cref="WholeText"/>, and the words inside
    /// &lt;link="name"&gt; tags with the matching style of the style sheet (see <see cref="TextFxMarkup"/>).
    /// Both stack. Nothing is allocated per frame; the laid-out letters are re-read whenever TextMeshPro rebuilds the text.
    /// </summary>
    [RequireComponent(typeof(TMP_Text)), DisallowMultipleComponent]
    [AddComponentMenu("Text FX/Text FX Animator")]
    public class TextFxAnimator : MonoBehaviour
    {
        const int NoStyle = -1;

        [SerializeField, Tooltip("Applied to every letter. Set the effect to None to animate only the <link> words")]
        TextEffectSettings wholeText = new TextEffectSettings(TextEffect.Wave);
        [SerializeField, Tooltip("Styles for words inside <link=\"name\">. Empty: the sheet at Resources/TextFxStyleSheet, if there is one")]
        TextFxStyleSheet styleSheet;
        [SerializeField, Tooltip("Keep animating while Time.timeScale is 0 (paused menus)")]
        bool unscaledTime = true;

        TMP_Text _text;
        Action<TMP_TextInfo> _onPreRender;
        Action _onSheetChanged;
        TextFxStyleSheet _subscribedSheet;

        Vector3[] _restVertices = Array.Empty<Vector3>();
        Color32[] _restColors = Array.Empty<Color32>();
        int _restLetters;
        bool _hasRest;
        bool _isPosed;
        TMP_VertexDataUpdateFlags _posedFlags;

        int[] _letterStyles = Array.Empty<int>();
        string _styledText;
        bool _hasStyledWords;
        TMP_VertexDataUpdateFlags _styledFlags;

        /// <summary>The effect on every letter. Edit its fields freely at runtime.</summary>
        public TextEffectSettings WholeText => wholeText;

        public TextFxStyleSheet StyleSheet
        {
            get => styleSheet != null ? styleSheet : TextFxStyleSheet.Default;
            set
            {
                styleSheet = value;
                Refresh();
            }
        }

        public bool UnscaledTime
        {
            get => unscaledTime;
            set => unscaledTime = value;
        }

        TMP_Text Text => _text != null ? _text : _text = GetComponent<TMP_Text>();

        void Awake()
        {
            _onPreRender = HandlePreRender;
            _onSheetChanged = Refresh;
        }

        void OnEnable()
        {
            Text.OnPreRenderText += _onPreRender;
            SubscribeToSheet();
            Refresh();
        }

        void OnDisable()
        {
            if (!ReferenceEquals(_text, null))
                _text.OnPreRenderText -= _onPreRender;
            SubscribeToSheet(unsubscribe: true);
            _hasRest = false;
            if (_isPosed)
                Rebuild();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!isActiveAndEnabled || _onSheetChanged == null)
                return;

            SubscribeToSheet();
            Refresh();
        }
#endif

        /// <summary>
        /// Re-reads which words carry a style and rebuilds the text. Called for you when the text or the sheet changes;
        /// call it after changing a sheet's styles from code.
        /// </summary>
        public void Refresh()
        {
            _styledText = null;
            _hasRest = false;
            Rebuild();
        }

        void Rebuild()
        {
            _isPosed = false;
            var text = Text;
            if (text != null && text.isActiveAndEnabled)
                text.havePropertiesChanged = true;
        }

        void SubscribeToSheet(bool unsubscribe = false)
        {
            if (_subscribedSheet != null)
                _subscribedSheet.Changed -= _onSheetChanged;

            _subscribedSheet = unsubscribe ? null : StyleSheet;
            if (_subscribedSheet != null)
                _subscribedSheet.Changed += _onSheetChanged;
        }

        void LateUpdate()
        {
            if (!_hasRest)
                return;

            // An effect was switched off (or from movement to colour): rebuild so no letter stays where the old one left it.
            var wanted = WantedFlags();
            if (_isPosed && (_posedFlags & ~wanted) != 0)
            {
                Rebuild();
                return;
            }

            if (wanted == TMP_VertexDataUpdateFlags.None)
                return;

            var flags = Pose(Text.textInfo);
            if (flags != TMP_VertexDataUpdateFlags.None)
                Text.UpdateVertexData(flags);
        }

        // TextMeshPro has just laid the text out and is about to upload it: remember where every letter rests, then pose it.
        void HandlePreRender(TMP_TextInfo info)
        {
            ReadStyledWords(info);
            CaptureRest(info);
            if (WantedFlags() != TMP_VertexDataUpdateFlags.None)
                Pose(info);
        }

        void ReadStyledWords(TMP_TextInfo info)
        {
            var sheet = StyleSheet;
            if (sheet == null || sheet.Count == 0 || info.linkCount == 0)
            {
                _hasStyledWords = false;
                _styledText = null;
                return;
            }

            // Links only change with the text, and reading their ids allocates, so do it once per new text.
            string current = Text.text;
            if (ReferenceEquals(current, _styledText) && _letterStyles.Length >= info.characterCount)
                return;

            _styledText = current;
            _hasStyledWords = false;
            _styledFlags = TMP_VertexDataUpdateFlags.None;
            EnsureCapacity(ref _letterStyles, info.characterCount);
            for (int i = 0; i < info.characterCount; i++)
                _letterStyles[i] = NoStyle;

            for (int i = 0; i < info.linkCount; i++)
            {
                var link = info.linkInfo[i];
                int style = sheet.IndexOf(link.GetLinkID());
                if (style == NoStyle || !sheet[style].Effect.IsActive)
                    continue;

                _hasStyledWords = true;
                _styledFlags |= FlagsFor(sheet[style].Effect);
                int last = Mathf.Min(link.linkTextfirstCharacterIndex + link.linkTextLength, info.characterCount);
                for (int c = link.linkTextfirstCharacterIndex; c < last; c++)
                    _letterStyles[c] = style;
            }
        }

        void CaptureRest(TMP_TextInfo info)
        {
            int letters = info.characterCount;
            EnsureCapacity(ref _restVertices, letters * 4);
            EnsureCapacity(ref _restColors, letters * 4);

            var characters = info.characterInfo;
            for (int c = 0; c < letters; c++)
            {
                ref var character = ref characters[c];
                if (!character.isVisible)
                    continue;

                ref var mesh = ref info.meshInfo[character.materialReferenceIndex];
                int rest = c * 4;
                Array.Copy(mesh.vertices, character.vertexIndex, _restVertices, rest, 4);
                Array.Copy(mesh.colors32, character.vertexIndex, _restColors, rest, 4);
            }

            _restLetters = letters;
            _hasRest = true;
            _isPosed = false;
        }

        TMP_VertexDataUpdateFlags Pose(TMP_TextInfo info)
        {
            var flags = WantedFlags();
            if (flags == TMP_VertexDataUpdateFlags.None)
                return flags;

            bool moves = (flags & TMP_VertexDataUpdateFlags.Vertices) != 0;
            bool tints = (flags & TMP_VertexDataUpdateFlags.Colors32) != 0;
            var sheet = _hasStyledWords ? StyleSheet : null;
            float time = unscaledTime ? Time.unscaledTime : Time.time;
            float fontSize = Text.fontSize;
            int letters = Mathf.Min(info.characterCount, _restLetters);
            var characters = info.characterInfo;

            for (int c = 0; c < letters; c++)
            {
                ref var character = ref characters[c];
                if (!character.isVisible)
                    continue;

                var pose = LetterPose.Rest;
                if (wholeText.IsActive)
                    wholeText.Apply(time, c, fontSize, ref pose);

                if (sheet != null && c < _letterStyles.Length && _letterStyles[c] != NoStyle && _letterStyles[c] < sheet.Count)
                    sheet[_letterStyles[c]].Effect.Apply(time, c, fontSize, ref pose);

                ref var mesh = ref info.meshInfo[character.materialReferenceIndex];
                int vertex = character.vertexIndex;
                int rest = c * 4;

                if (moves)
                {
                    var vertices = mesh.vertices;
                    Vector3 centre = (_restVertices[rest] + _restVertices[rest + 2]) * 0.5f;
                    for (int v = 0; v < 4; v++)
                        vertices[vertex + v] = centre + (_restVertices[rest + v] - centre) * pose.Scale + pose.Offset;
                }

                if (tints)
                {
                    var colors = mesh.colors32;
                    for (int v = 0; v < 4; v++)
                    {
                        var restColor = _restColors[rest + v];
                        var tinted = Color32.LerpUnclamped(restColor, pose.Tint, pose.TintAmount);
                        tinted.a = restColor.a;
                        colors[vertex + v] = tinted;
                    }
                }
            }

            _isPosed = true;
            _posedFlags = flags;
            return flags;
        }

        TMP_VertexDataUpdateFlags WantedFlags() =>
            FlagsFor(wholeText) | (_hasStyledWords ? _styledFlags : TMP_VertexDataUpdateFlags.None);

        static TMP_VertexDataUpdateFlags FlagsFor(TextEffectSettings effect)
        {
            if (effect.MovesLetters)
                return TMP_VertexDataUpdateFlags.Vertices;
            if (effect.TintsLetters)
                return TMP_VertexDataUpdateFlags.Colors32;
            return TMP_VertexDataUpdateFlags.None;
        }

        static void EnsureCapacity<T>(ref T[] array, int length)
        {
            if (array.Length < length)
                array = new T[Mathf.NextPowerOfTwo(length)];
        }
    }
}
