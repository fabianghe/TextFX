using System;
using TMPro;
using UnityEngine;

namespace TextFX
{
    /// <summary>
    /// Reveals a TextMeshPro text letter by letter, pausing after commas and sentence ends.
    /// Works alongside a <see cref="TextFxAnimator"/>: format the text with <see cref="TextFxMarkup"/> first if it uses markup.
    /// </summary>
    [RequireComponent(typeof(TMP_Text)), DisallowMultipleComponent]
    [AddComponentMenu("Text FX/Text FX Typewriter")]
    public class TextFxTypewriter : MonoBehaviour
    {
        const int AllCharacters = 99999;

        [SerializeField, Min(1f)] float charactersPerSecond = 45f;
        [SerializeField, Min(0f), Tooltip("Wait before the first letter, e.g. for a pop-in")] float startDelay = 0.15f;
        [SerializeField, Min(0f)] float commaPause = 0.08f;
        [SerializeField, Min(0f)] float sentencePause = 0.25f;
        [SerializeField, Tooltip("Keep typing while Time.timeScale is 0 (paused menus)")] bool unscaledTime = true;

        public event Action Completed;
        public event Action<char> CharacterRevealed;

        public bool IsRevealing { get; private set; }

        public float CharactersPerSecond
        {
            get => charactersPerSecond;
            set => charactersPerSecond = Mathf.Max(1f, value);
        }

        public float StartDelay => startDelay;

        TMP_Text _text;
        int _visibleCharacters;
        int _totalCharacters;
        float _timer;

        TMP_Text Text => _text != null ? _text : _text = GetComponent<TMP_Text>();

        void Awake()
        {
            if (!IsRevealing)
                enabled = false;
        }

        public void Play(string text) => Play(text, startDelay);

        public void Play(string text, float delay)
        {
            Text.text = text;
            Text.maxVisibleCharacters = 0;
            Text.ForceMeshUpdate(true);
            _totalCharacters = Text.textInfo.characterCount;
            _visibleCharacters = 0;
            _timer = -delay;
            IsRevealing = true;
            enabled = true;

            if (_totalCharacters == 0)
                Complete();
        }

        /// <summary>Shows the rest of the text at once.</summary>
        public void Complete()
        {
            if (!IsRevealing)
                return;

            IsRevealing = false;
            enabled = false;
            Text.maxVisibleCharacters = AllCharacters;
            Completed?.Invoke();
        }

        void Update()
        {
            _timer += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            while (_timer >= 0f)
            {
                _visibleCharacters++;
                Text.maxVisibleCharacters = _visibleCharacters;
                CharacterRevealed?.Invoke(Text.textInfo.characterInfo[_visibleCharacters - 1].character);

                if (_visibleCharacters >= _totalCharacters)
                {
                    Complete();
                    return;
                }

                _timer -= 1f / charactersPerSecond + PauseAfter(_visibleCharacters - 1);
            }
        }

        float PauseAfter(int characterIndex)
        {
            var characters = Text.textInfo.characterInfo;
            bool nextIsSpace = characterIndex + 1 >= _totalCharacters || char.IsWhiteSpace(characters[characterIndex + 1].character);
            if (!nextIsSpace)
                return 0f;

            switch (characters[characterIndex].character)
            {
                case '.':
                case '!':
                case '?':
                case '…':
                    return sentencePause;
                case ',':
                case ';':
                case ':':
                case '—':
                case '–':
                    return commaPause;
                default:
                    return 0f;
            }
        }
    }
}
