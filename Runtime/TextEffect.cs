using System;
using UnityEngine;

namespace TextFX
{
    public enum TextEffect
    {
        None,
        Wave,
        Bounce,
        Pulse,
        Shake,
        Shimmer,
        Pop,
    }

    /// <summary>How one effect moves or tints letters. Public fields so code can retune it at runtime.</summary>
    [Serializable]
    public class TextEffectSettings
    {
        const float FullCycle = Mathf.PI * 2f;
        const float ShakeRate = 8f;
        const float ShakeSeedStep = 1.7f;

        [Tooltip("Wave: letters bob up and down one after another. Bounce: letters hop up from the baseline. Pulse: letters swell and shrink. Pop: letters grow and settle back to their size, never smaller. Shake: letters tremble. Shimmer: the colour drifts to Shimmer Color and back")]
        public TextEffect Effect = TextEffect.Wave;

        [Range(0f, 0.5f), Tooltip("Wave, Bounce, Shake: how far letters move, as a fraction of the font size. Pulse, Pop: how much they grow (0.1 = 10%). Unused by Shimmer")]
        public float Strength = 0.06f;

        [Min(0f), Tooltip("Cycles per second")]
        public float Speed = 1.2f;

        [Range(0f, 2f), Tooltip("How much later each letter moves than the one before it. 0: the whole text moves as one")]
        public float Spread = 0.5f;

        [Tooltip("Shimmer only: the colour letters drift to")]
        public Color ShimmerColor = Color.white;

        public TextEffectSettings() { }

        public TextEffectSettings(TextEffect effect) => Effect = effect;

        public TextEffectSettings(TextEffect effect, float strength, float speed, float spread)
        {
            Effect = effect;
            Strength = strength;
            Speed = speed;
            Spread = spread;
        }

        public bool IsActive => Effect != TextEffect.None;
        public bool MovesLetters => Effect != TextEffect.None && Effect != TextEffect.Shimmer;
        public bool TintsLetters => Effect == TextEffect.Shimmer;

        internal void Apply(float time, int letter, float fontSize, ref LetterPose pose)
        {
            float phase = time * Speed * FullCycle - letter * Spread;
            switch (Effect)
            {
                case TextEffect.Wave:
                    pose.Offset.y += Mathf.Sin(phase) * Strength * fontSize;
                    break;
                case TextEffect.Bounce:
                    pose.Offset.y += Mathf.Abs(Mathf.Sin(phase * 0.5f)) * Strength * fontSize;
                    break;
                case TextEffect.Pulse:
                    pose.Scale *= 1f + Mathf.Sin(phase) * Strength;
                    break;
                case TextEffect.Pop:
                    pose.Scale *= 1f + (0.5f - 0.5f * Mathf.Cos(phase)) * Strength;
                    break;
                case TextEffect.Shake:
                    float beat = time * Speed * ShakeRate;
                    float seed = letter * ShakeSeedStep;
                    float reach = 2f * Strength * fontSize;
                    pose.Offset.x += (Mathf.PerlinNoise(beat, seed) - 0.5f) * reach;
                    pose.Offset.y += (Mathf.PerlinNoise(seed, beat) - 0.5f) * reach;
                    break;
                case TextEffect.Shimmer:
                    pose.Tint = ShimmerColor;
                    pose.TintAmount = Mathf.Sin(phase) * 0.5f + 0.5f;
                    break;
            }
        }
    }

    /// <summary>One letter's displacement from its laid-out position this frame.</summary>
    internal struct LetterPose
    {
        public Vector3 Offset;
        public float Scale;
        public Color32 Tint;
        public float TintAmount;

        public static LetterPose Rest => new LetterPose { Scale = 1f };
    }
}
