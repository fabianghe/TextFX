using NUnit.Framework;
using UnityEngine;

namespace TextFX.Tests
{
    public class TextFxMarkupTests
    {
        TextFxStyleSheet _sheet;

        [SetUp]
        public void SetUp()
        {
            _sheet = TextFxStyleSheet.Create("fx",
                new TextFxStyle("hl", new TextEffectSettings(TextEffect.Wave), Color.red),
                new TextFxStyle("loud", new TextEffectSettings(TextEffect.Shake), Color.blue, bold: true),
                new TextFxStyle("plain", new TextEffectSettings(TextEffect.Pulse)));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_sheet);

        static string Hex(Color color) => ColorUtility.ToHtmlStringRGBA(color);

        [Test]
        public void TextWithoutTags_IsReturnedUntouched()
        {
            const string text = "Take the old road north. <b>Now.</b>";

            Assert.AreSame(text, TextFxMarkup.Format(text, _sheet, out bool hasStyledWords));
            Assert.IsFalse(hasStyledWords);
        }

        [Test]
        public void ABareTag_UsesTheFirstStyle()
        {
            string formatted = TextFxMarkup.Format("Take the <fx>old road</fx> north.", _sheet, out bool hasStyledWords);

            Assert.IsTrue(hasStyledWords);
            Assert.AreEqual("Take the <link=\"hl\"><color=#" + Hex(Color.red) + ">old road</color></link> north.", formatted);
        }

        [Test]
        public void ANamedTag_UsesThatStyle_WithBold()
        {
            Assert.AreEqual(
                "A <link=\"loud\"><color=#" + Hex(Color.blue) + "><b>word</b></color></link>.",
                TextFxMarkup.Format("A <fx=LOUD>word</fx>.", _sheet));
        }

        [Test]
        public void AQuotedName_Works()
        {
            Assert.AreEqual(TextFxMarkup.Format("A <fx=loud>word</fx>.", _sheet), TextFxMarkup.Format("A <fx=\"loud\">word</fx>.", _sheet));
        }

        [Test]
        public void AStyleWithoutColour_OnlyAddsTheLink()
        {
            Assert.AreEqual("A <link=\"plain\">word</link>.", TextFxMarkup.Format("A <fx=plain>word</fx>.", _sheet));
        }

        [Test]
        public void TheShortClosingTag_ClosesToo()
        {
            Assert.AreEqual(TextFxMarkup.Format("A <fx>word</fx> here.", _sheet), TextFxMarkup.Format("A <fx>word<fx/> here.", _sheet));
        }

        [Test]
        public void AnUnknownName_UsesTheFirstStyle()
        {
            Assert.AreEqual(TextFxMarkup.Format("A <fx>word</fx> here.", _sheet), TextFxMarkup.Format("A <fx=no-such-style>word</fx> here.", _sheet));
        }

        [Test]
        public void AForgottenClosingTag_IsClosedAtTheEnd()
        {
            StringAssert.EndsWith("</link>", TextFxMarkup.Format("A <fx>word", _sheet));
        }

        [Test]
        public void AStrayClosingTag_IsDropped()
        {
            Assert.AreEqual("A word here.", TextFxMarkup.Format("A word</fx> here.", _sheet));
        }

        [Test]
        public void ThePlainPreview_HasNoLinks()
        {
            string formatted = TextFxMarkup.Format("A <fx>word</fx>.", _sheet, out _, includeLinks: false);

            StringAssert.DoesNotContain("<link", formatted);
            StringAssert.Contains("<color=#", formatted);
        }

        [Test]
        public void TheTag_ComesFromTheSheet()
        {
            var sheet = TextFxStyleSheet.Create("hl", new TextFxStyle("hl", new TextEffectSettings(TextEffect.Wave)));
            try
            {
                Assert.AreEqual("A <link=\"hl\">word</link>.", TextFxMarkup.Format("A <hl>word</hl>.", sheet));
                Assert.AreEqual("A <fx>word</fx>.", TextFxMarkup.Format("A <fx>word</fx>.", sheet));
            }
            finally
            {
                Object.DestroyImmediate(sheet);
            }
        }

        [Test]
        public void Style_WrapsTheWholeText()
        {
            Assert.AreEqual("<link=\"plain\">Mara</link>", TextFxMarkup.Style("Mara", "plain", _sheet));
        }
    }
}
