# The color theme

Every window built on this toolkit uses the same color theme. It is defined as constants on `UIBuilder`. The theme is a neutral, slightly blue-tinted grey with a single orange accent color, chosen to match Parkitect's own style.

| Constant | Value (`new Color`) | Used for |
|---|---|---|
| `PanelColor` | `0.855f, 0.855f, 0.878f, 1f` | the light inner window panel (the Image on the content root) |
| `TextColor` | `0.17f, 0.17f, 0.18f, 1f` | dark label text on the panel; also the tint for white icon silhouettes |
| `TextEntryTextColor` | `0.94f, 0.94f, 0.96f, 1f` | light text inside dark input fields (placeholder text: same RGB but at alpha `0.45f`) |
| `FieldColor` | `0.25f, 0.25f, 0.25f, 1f` | the dark rounded search-style input background; also stepper buttons and the scrollbar track |
| `SliderTrackColor` | `0.30f, 0.30f, 0.32f, 1f` | the dark-grey unfilled slider track |
| `SeparatorColor` | `0.17f, 0.17f, 0.18f, 0.30f` | the faint dark rule on the panel |
| `ListInsetColor` | `0.78f, 0.78f, 0.80f, 1f` | a slightly darker inset behind a scroll list (`ScrollList` with `inset: true`) |
| `ButtonColor` | `0.935f, 0.935f, 0.958f, 1f` | push-button normal tint (lighter than the panel so it reads raised) |
| `ButtonHighlightColor` | `0.98f, 0.98f, 1f, 1f` | push-button hover tint |
| `ButtonPressedColor` | `0.845f, 0.845f, 0.868f, 1f` | push-button pressed tint |
| `ButtonDisabledColor` | `0.845f, 0.845f, 0.868f, 0.5f` | push-button disabled tint |
| `ButtonSelectedColor` | `1f, 0.72f, 0.35f, 1f` | **accent color**: the active tab, the selected radio option, a selected list row, anything else that shows "selected" |

## Rules that go with the values

- **All push buttons come from `UIBuilder.CreateThemedButton`** (or `CreateThemedIconButton`)
- **Tabs and radio groups are ordinary themed buttons**, not a custom control. Mark the active one by setting its
  background `Image.color` to `ButtonSelectedColor` and the rest back to `Color.white` (`UIBuilder.SetButtonSelected`
  does the same - `TabBar` and `RadioRow` use it).
- **Dark controls keep their light interior text.** A dropdown's selected value, an input field's text, and the
  stepper glyphs stay `TextEntryTextColor`/white. Only labels that sit directly on the panel get `TextColor`.
- **The slider's fill** is the game's own; the toolkit does not recolor it.

## Sizes that go with the theme

| Constant | Value | Meaning |
|---|---|---|
| `UIBuilder.DEFAULT_LABEL_WIDTH` | `110f` | Keeps the labels of a column of controls nicely lined up |
| `UIBuilder.ROW_HEIGHT` | `26f` | a hand-built row's height |
| `UIBuilder.BUTTON_HEIGHT` | `26f` | a themed push button |
| `UIBuilder.FIELD_HEIGHT` | `22f` | an input field inside a row |
| `UIBuilder.FIELD_MIN_WIDTH` | `50f` | the minimum width a field can be shrunk |
| `UIBuilder.STEPPER_WIDTH` | `24f` | the small glyph buttons beside a value |
| `UIBuilder.SLIDER_TRACK_HEIGHT` | `11f` | the restyled native slider's bar |
| `UIBuilder.SLIDER_HANDLE_HEIGHT` | `16f` | its handle, fixed so a short row can't shrink it |
| `UIBuilder.SLIDER_BAR_RAISE` | `4f` | how far the bar is nudged up toward its label |
| `DropdownRow.DEFAULT_ROW_HEIGHT` | `46f` | caption over control, the roomy default |
| `DropdownRow.COMPACT_ROW_HEIGHT` | `30f` | the tightest the dropdown prefab fits in |

Window panels default to 10 units of padding and 6 units row spacing (`ThemedWindowOptions.padding` / `spacing`); sub-groups use 4 units spacing.


