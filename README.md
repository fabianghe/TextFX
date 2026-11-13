# Text FX

Per-letter TextMeshPro animations for Unity: **Wave, Bounce, Pulse, Pop, Shake, Shimmer**, on a whole text or on marked-up words, plus a typewriter reveal. No dependencies beyond TextMeshPro (uGUI 2.0 / TMP 3.x). Nothing is allocated per frame.

## Install

In `Packages/manifest.json`, either a local checkout:

```json
"com.fabi.textfx": "file:../../TextFX"
```

or the git repo once it has a remote (pin a tag):

```json
"com.fabi.textfx": "https://github.com/<owner>/TextFX.git#v1.0.0"
```

## Animate a whole text

Add **Text FX Animator** to any GameObject with a `TextMeshProUGUI` or `TextMeshPro`. It defaults to a gentle wave.

| Field | Meaning |
|---|---|
| Effect | Wave (bob up/down), Bounce (hop up from the baseline), Pulse (grow/shrink), Pop (grow and settle back, never smaller; good for button labels), Shake (tremble), Shimmer (colour drifts to Shimmer Color) |
| Strength | Movement as a fraction of the font size (Pulse, Pop: scale change, 0.1 = 10%) |
| Speed | Cycles per second |
| Spread | Delay between neighbouring letters; 0 moves the text as one |

Settings can be changed at runtime: `animator.WholeText.Speed = 2f;`

## Animate single words

1. Create a style sheet: *Create > Text FX > Style Sheet*. Put it in a `Resources` folder as `TextFxStyleSheet` to make it the project default, or assign it to an animator.
2. Each style has a name, optional colour and bold, and an effect.
3. Mark the words with a TextMeshPro link whose id is the style name: `Press <link="loud">GO</link>!`, or write markup and format it:

```csharp
text.text = TextFxMarkup.Format("Take the <fx>old road</fx> to <fx=loud>the gate</fx>.");
```

`<fx>` uses the first style, `<fx=name>` the named one (unknown names fall back to the first). The tag is set per sheet, so a project can keep e.g. `<hl>`. Set the animator's whole-text effect to **None** if only the words should move. Links whose id matches no style (real hyperlinks) are left alone.

## Typewriter

Add **Text FX Typewriter** and call `typewriter.Play(text)`. Events: `CharacterRevealed(char)` (for blips), `Completed`. `Complete()` reveals the rest at once. It pauses after commas and sentence ends.

## Notes

- Letters are re-read whenever TextMeshPro rebuilds the text, so changing the text, size or layout just works.
- Letters can move outside the RectTransform: give them room inside masks.
- Call `animator.Refresh()` after changing a sheet's styles from code.
- Tests: add `"testables": ["com.fabi.textfx"]` to the project manifest to see them in the Test Runner.
