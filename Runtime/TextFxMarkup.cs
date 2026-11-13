using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TextFX
{
    /// <summary>
    /// Turns writer-friendly markup into TextMeshPro rich text that a <see cref="TextFxAnimator"/> animates:
    /// &lt;fx&gt;word&lt;/fx&gt; uses the sheet's first style, &lt;fx=name&gt;word&lt;/fx&gt; (or &lt;fx="name"&gt;) the named one,
    /// and an unknown name falls back to the first style. The tag ("fx") is set per style sheet.
    /// Each word becomes &lt;link="name"&gt; plus the style's colour and bold. Unclosed tags close at the end; stray closing tags are dropped.
    /// Main thread only. Allocates the result string, so call it when the text changes, not per frame.
    /// </summary>
    public static class TextFxMarkup
    {
        static readonly StringBuilder Builder = new StringBuilder(256);
        static readonly List<int> OpenStyles = new List<int>(4);

        /// <summary>Formats with the sheet at Resources/TextFxStyleSheet.</summary>
        public static string Format(string text) => Format(text, TextFxStyleSheet.Default, out _);

        public static string Format(string text, TextFxStyleSheet sheet) => Format(text, sheet, out _);

        /// <param name="includeLinks">False for plain previews (colour and bold only), e.g. outside TextMeshPro.</param>
        public static string Format(string text, TextFxStyleSheet sheet, out bool hasStyledWords, bool includeLinks = true)
        {
            hasStyledWords = false;
            if (string.IsNullOrEmpty(text) || sheet == null)
                return text;

            sheet.CacheTags();
            string open = sheet.OpenTag;
            string openNamed = sheet.OpenNamedTag;
            string close = sheet.CloseTag;
            string closeShort = sheet.CloseShortTag;

            // "<fx" covers "<fx>", "<fx=" and "<fx/>".
            if (text.IndexOf(sheet.OpenPrefix, StringComparison.Ordinal) < 0 && text.IndexOf(close, StringComparison.Ordinal) < 0)
                return text;

            Builder.Clear();
            OpenStyles.Clear();

            int i = 0;
            while (i < text.Length)
            {
                if (text[i] != '<')
                {
                    Builder.Append(text[i++]);
                    continue;
                }

                if (StartsWith(text, i, close))
                {
                    AppendClose(sheet, includeLinks);
                    i += close.Length;
                }
                else if (StartsWith(text, i, closeShort))
                {
                    AppendClose(sheet, includeLinks);
                    i += closeShort.Length;
                }
                else if (StartsWith(text, i, open))
                {
                    AppendOpen(sheet, 0, includeLinks);
                    hasStyledWords = true;
                    i += open.Length;
                }
                else if (StartsWith(text, i, openNamed) && text.IndexOf('>', i) is var end && end > 0)
                {
                    int nameStart = i + openNamed.Length;
                    int nameLength = end - nameStart;
                    if (nameLength >= 2 && text[nameStart] == '"' && text[end - 1] == '"')
                    {
                        nameStart++;
                        nameLength -= 2;
                    }

                    AppendOpen(sheet, Mathf.Max(0, sheet.IndexOf(text, nameStart, nameLength)), includeLinks);
                    hasStyledWords = true;
                    i = end + 1;
                }
                else
                {
                    Builder.Append(text[i++]);
                }
            }

            while (OpenStyles.Count > 0)
                AppendClose(sheet, includeLinks);

            return Builder.ToString();
        }

        /// <summary>Wraps the whole text in one style, e.g. a name shown on its own. Null or unknown name: the first style.</summary>
        public static string Style(string text, string styleName, TextFxStyleSheet sheet = null)
        {
            sheet = sheet != null ? sheet : TextFxStyleSheet.Default;
            if (string.IsNullOrEmpty(text) || sheet == null)
                return text;

            Builder.Clear();
            OpenStyles.Clear();
            AppendOpen(sheet, Mathf.Max(0, sheet.IndexOf(styleName)), true);
            Builder.Append(text);
            AppendClose(sheet, true);
            return Builder.ToString();
        }

        static bool StartsWith(string text, int index, string tag) =>
            index + tag.Length <= text.Length && string.CompareOrdinal(text, index, tag, 0, tag.Length) == 0;

        static void AppendOpen(TextFxStyleSheet sheet, int style, bool includeLinks)
        {
            OpenStyles.Add(style);
            if (style >= sheet.Count)
                return;

            var look = sheet[style];
            if (includeLinks && !string.IsNullOrEmpty(look.Name))
                Builder.Append("<link=\"").Append(look.Name).Append("\">");
            if (look.SetColor)
                Builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(look.Color)).Append('>');
            if (look.Bold)
                Builder.Append("<b>");
        }

        static void AppendClose(TextFxStyleSheet sheet, bool includeLinks)
        {
            if (OpenStyles.Count == 0)
                return;

            int style = OpenStyles[OpenStyles.Count - 1];
            OpenStyles.RemoveAt(OpenStyles.Count - 1);
            if (style >= sheet.Count)
                return;

            var look = sheet[style];
            if (look.Bold)
                Builder.Append("</b>");
            if (look.SetColor)
                Builder.Append("</color>");
            if (includeLinks && !string.IsNullOrEmpty(look.Name))
                Builder.Append("</link>");
        }
    }
}
