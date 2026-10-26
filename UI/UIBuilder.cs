using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>Layout, label, and control builders that match Parkitect's UI style.</summary>
    /// <remarks>
    /// Defines the shared colors and sizes used throughout the toolkit (see docs/THEME.md).
    /// Every Create* method takes the parent Transform as its first argument.
    /// </remarks>
    public static class UIBuilder {
        /// <summary>The version of the toolkit's UI/ source folder.</summary>
        /// <remarks>
        /// Use this to check which version a mod has copied. See README.md, "Using it in your mod".
        /// </remarks>
        public const string VERSION = "1.0.0";

        // ---== Theme ==---
        // Matches Parkitect's overview windows: light panels with dark labels, dark fields with light text and an orange selection color.

        /// <summary>Light inner window panel.</summary>
        public static readonly Color PanelColor = new Color(0.855f, 0.855f, 0.878f, 1f);
        /// <summary>Dark label text on the panel; also the tint for white icon silhouettes.</summary>
        public static readonly Color TextColor = new Color(0.17f, 0.17f, 0.18f, 1f);
        /// <summary>Light text inside dark input fields.</summary>
        public static readonly Color TextEntryTextColor = new Color(0.94f, 0.94f, 0.96f, 1f);
        /// <summary>Dark, rounded search-style input background.</summary>
        public static readonly Color FieldColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        /// <summary>Dark-grey unfilled slider track.</summary>
        public static readonly Color SliderTrackColor = new Color(0.30f, 0.30f, 0.32f, 1f);
        /// <summary>Faint dividing line on the light panel.</summary>
        public static readonly Color SeparatorColor = new Color(0.17f, 0.17f, 0.18f, 0.30f);
        /// <summary>Slightly darker inset behind a scroll list.</summary>
        public static readonly Color ListInsetColor = new Color(0.78f, 0.78f, 0.80f, 1f);

        // Button.colors applies these tints to a white background sprite. Hover lightens the button, pressing darkens it.
        /// <summary>Light button background that appears raised above the panel.</summary>
        public static readonly Color ButtonColor = new Color(0.935f, 0.935f, 0.958f, 1f);
        public static readonly Color ButtonHighlightColor = new Color(0.98f, 0.98f, 1f, 1f);
        public static readonly Color ButtonPressedColor = new Color(0.845f, 0.845f, 0.868f, 1f);
        /// <summary>Greyed out and half transparent.</summary>
        public static readonly Color ButtonDisabledColor = new Color(0.845f, 0.845f, 0.868f, 0.5f);
        /// <summary>Highlight background for selected buttons and tabs.</summary>
        /// <remarks>
        /// Apply with SetButtonSelected so hover and press feedback still works.
        /// </remarks>
        public static readonly Color ButtonSelectedColor = new Color(1f, 0.72f, 0.35f, 1f);

        // ---== Sizes ==---
        // Shared sizes keep rows aligned. Controls cloned from game prefabs keep the heights those prefabs need.
        /// <summary>Default width of a row's label column.</summary>
        public const float DEFAULT_LABEL_WIDTH = 110f;
        /// <summary>Height of a row built without a game prefab.</summary>
        public const float ROW_HEIGHT = 26f;
        /// <summary>Button height; CreateButtonRow adds 2 units to the row height by default.</summary>
        public const float BUTTON_HEIGHT = 26f;
        /// <summary>Height of an input field inside a row.</summary>
        public const float FIELD_HEIGHT = 22f;
        /// <summary>The minimum width a flexible input field can shrink to.</summary>
        public const float FIELD_MIN_WIDTH = 50f;
        /// <summary>Width of the small arrow, plus and minus buttons beside a value.</summary>
        public const float STEPPER_WIDTH = 24f;
        /// <summary>Height of the themed slider's track.</summary>
        public const float SLIDER_TRACK_HEIGHT = 11f;
        /// <summary>Fixed slider handle height, so a short row cannot shrink it.</summary>
        public const float SLIDER_HANDLE_HEIGHT = 16f;
        /// <summary>Distance to move the slider track up toward its label.</summary>
        public const float SLIDER_BAR_RAISE = 4f;

        private const float ICON_INSET = 4f; // padding between an icon button's edge and its sprite
        private const float CAPTION_INSET = 8f; // left and right padding for a button label that is not centered
        private const string ICON_NAME = "Icon"; // child name used by GetButtonIcon to find the icon Image

        private static Font _defaultFont;

        /// <summary>Font for input fields that use Unity's Text component.</summary>
        /// <remarks>The toolkit's other labels use Parkitect's TextMesh Pro font.</remarks>
        public static Font DefaultFont {
            get {
                if (_defaultFont == null) {
                    _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _defaultFont;
            }
        }

        // ---== Containers ==---

        public static GameObject CreateUIObject(Transform parent, string name, params Type[] components) {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Creates a fixed-height horizontal row with vertically centered controls.</summary>
        /// <remarks>
        /// Keep childControlWidth true when mixing fixed and flexible widths, such as a slider beside an input field.
        /// Otherwise the row ignores LayoutElement widths, which can shrink prefab controls and wrap their labels.
        /// Set it false only when children must size themselves, as ColorSwatchRow's picker button does.
        /// With childForceExpandWidth false, only children with flexibleWidth greater than zero use spare width.
        /// </remarks>
        /// <param name="childControlWidth">
        /// Whether the row uses its children's LayoutElement widths. Defaults to true.
        /// </param>
        public static GameObject CreateHorizontalRow(Transform parent, string name, float height, bool childControlWidth = true, bool childForceExpandWidth = false, float spacing = 8f) {
            var go = CreateUIObject(parent, name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = spacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = childControlWidth;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = childForceExpandWidth;
            hlg.childForceExpandHeight = false;
            FixHeight(go, height);
            return go;
        }

        /// <summary>Creates a full-width vertical group for rows that belong together.</summary>
        /// <remarks>
        /// Useful for a tab's content or a group shown by a toggle. Show or hide it with SetActive, then call
        /// resizeWindowToContent; hidden groups take no layout space. Use leftPadding to indent the group and
        /// negative topPadding to reduce the gap below the preceding row.
        /// </remarks>
        public static GameObject CreateVerticalGroup(Transform parent, string name, float spacing = 4f, int leftPadding = 0, int topPadding = 0, int bottomPadding = 0) {
            var go = CreateUIObject(parent, name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(leftPadding, 0, topPadding, bottomPadding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return go;
        }

        /// <summary>Creates a fixed-height row for buttons, usually at the bottom of a window.</summary>
        /// <remarks>Buttons created with width &lt;= 0 share the available width evenly.</remarks>
        public static GameObject CreateButtonRow(Transform parent, float height = BUTTON_HEIGHT + 2f, float spacing = 8f) {
            return CreateHorizontalRow(parent, "ButtonRow", height, spacing: spacing);
        }

        /// <summary>Adds a fixed gap between rows in addition to the layout group's spacing.</summary>
        public static GameObject CreateSpacer(Transform parent, float height) {
            var go = CreateUIObject(parent, "Spacer", typeof(RectTransform));
            FixHeight(go, height);
            return go;
        }

        /// <summary>Creates a spacer that uses the remaining width or height in a layout group.</summary>
        /// <remarks>Pushes later items to the bottom of a column or the right edge of a row.</remarks>
        public static GameObject CreateFlexibleSpacer(Transform parent) {
            var go = CreateUIObject(parent, "FlexibleSpacer", typeof(RectTransform));
            var le = EnsureLayoutElement(go);
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;
            return go;
        }

        /// <summary>Adds a light rounded background and a padded row or column layout to a window's content root.</summary>
        /// <remarks>
        /// The background is on root; add controls as children so they appear in front of it.
        /// ThemedWindow calls this automatically. Call it yourself when building directly on UIWindow.
        /// </remarks>
        public static HorizontalOrVerticalLayoutGroup SetupWindowPanel(RectTransform root, int padding = 10, float spacing = 6f, bool horizontal = false) {
            var panel = root.gameObject.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            ApplyPanelSprite(panel, PanelColor);

            HorizontalOrVerticalLayoutGroup layout;
            if (horizontal) {
                var hlg = root.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true; // Columns fill the content height
                layout = hlg;
            } else {
                var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                layout = vlg;
            }
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return layout;
        }

        /// <summary>Styles an existing Image as a rounded background using the game's panel sprite and the given color.</summary>
        /// <remarks>Use for custom panel, button or field backgrounds. Sliced scaling preserves the rounded corners.</remarks>
        public static void ApplyPanelSprite(Image image, Color color) {
            if (image == null) return;
            image.sprite = ThemedPrefabs.PanelSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        /// <summary>Creates a flat rectangular background and returns its Image.</summary>
        public static Image CreatePanel(Transform parent, Color color) {
            var go = CreateUIObject(parent, "Panel", typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        // ---== Layout elements ==---

        /// <summary>Returns the existing LayoutElement, or adds one if needed.</summary>
        public static LayoutElement EnsureLayoutElement(GameObject go) {
            return go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        }

        /// <summary>Sets the minimum and preferred layout height and disables flexible height.</summary>
        public static LayoutElement FixHeight(GameObject go, float height) {
            var le = EnsureLayoutElement(go);
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
            return le;
        }

        /// <summary>Sets the minimum and preferred layout width and disables flexible width.</summary>
        public static LayoutElement FixWidth(GameObject go, float width) {
            var le = EnsureLayoutElement(go);
            le.minWidth = width;
            le.preferredWidth = width;
            le.flexibleWidth = 0f;
            return le;
        }

        // ---== Labels ==---
        // Label builders return a UILabel so callers can update text without working with TextMesh Pro directly.

        /// <summary>Creates a single-line label using the game's font and the toolkit's text color.</summary>
        /// <param name="minHeight">A value greater than 0 fixes the layout height; 0 leaves it to the text.</param>
        /// <param name="alignment">
        /// Use TextAnchor.MiddleLeft, TextAnchor.MiddleCenter, or TextAnchor.MiddleRight. Text is vertically centered.
        /// </param>
        public static UILabel CreateThemedLabel(Transform parent, string text, float minHeight = 0f, TextAnchor alignment = TextAnchor.MiddleLeft) {
            var go = CreateUIObject(parent, "Label", typeof(RectTransform));
            var tmp = AddThemedText(go, text, ToTmpAlignment(alignment), false);
            if (minHeight > 0f) FixHeight(go, minHeight);
            return new UILabel(tmp);
        }

        /// <summary>Creates a themed label that wraps to fit the available width.</summary>
        /// <remarks>Uses flexible width so the parent layout can assign the space needed for wrapping.</remarks>
        public static UILabel CreateWrappedLabel(Transform parent, string text) {
            var go = CreateUIObject(parent, "Label", typeof(RectTransform));
            var tmp = AddThemedText(go, text, TextAlignmentOptions.TopLeft, true);
            EnsureLayoutElement(go).flexibleWidth = 1f;
            return new UILabel(tmp);
        }

        /// <summary>Creates a fixed-width row label so controls in different rows line up.</summary>
        /// <remarks>
        /// Sets both the LayoutElement width and the RectTransform width, so the label works whether or not
        /// the parent layout controls child widths.
        /// </remarks>
        public static UILabel CreateRowLabel(Transform parent, string text, float width = DEFAULT_LABEL_WIDTH) {
            var label = CreateThemedLabel(parent, text);
            FixWidth(label.Root, width);
            var rt = (RectTransform)label.Root.transform;
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            return label;
        }

        // Use the game's label font and style with dark text for the light panel.
        private static TextMeshProUGUI AddThemedText(GameObject go, string text, TextAlignmentOptions alignment, bool wrap) {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = ThemedPrefabs.LabelFont;
            tmp.fontSize = ThemedPrefabs.LabelFontSize;
            tmp.fontStyle = ThemedPrefabs.LabelFontStyle;
            tmp.color = TextColor;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = wrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.text = text;
            return tmp;
        }

        /// <summary>Applies the dark theme text color to text components matching label.</summary>
        /// <remarks>
        /// Intended for a prefab's row label on the light panel. Text inside a dark field should stay light.
        /// Does nothing if no matching text is found; every exact match in `go` and its children is recolored.
        /// </remarks>
        /// <param name="label">Exact text to match.</param>
        public static void DarkenLabelText(GameObject go, string label) {
            if (go == null) return;
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true)) {
                if (tmp != null && tmp.text == label) {
                    tmp.color = TextColor;
                }
            }
        }

        /// <summary>Returns the text label of a button made by CreateThemedButton.</summary>
        /// <returns>Null if button is null or has no text component.</returns>
        public static UILabel GetButtonCaption(Button button) {
            if (button == null) return null;
            var caption = button.GetComponentInChildren<TMP_Text>(true);
            return caption != null ? new UILabel(caption) : null;
        }

        /// <summary>Adds a horizontal dividing line with space above and below.</summary>
        /// <remarks>
        /// The returned container occupies one row in a vertical layout. Its height includes the line and both gaps.
        /// </remarks>
        public static GameObject CreateSeparator(Transform parent, float thickness = 2f, float padAbove = 4f, float padBelow = 4f) {
            var container = CreateUIObject(parent, "Separator", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, Mathf.RoundToInt(padAbove), Mathf.RoundToInt(padBelow));
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            FixHeight(container, thickness + padAbove + padBelow);

            var line = CreateUIObject(container.transform, "Line", typeof(RectTransform), typeof(Image));
            line.GetComponent<Image>().color = SeparatorColor;
            FixHeight(line, thickness);
            return container;
        }

        // ---== Buttons ==---

        /// <summary>Creates a text button styled like Parkitect's "Close all" and "Open all" buttons.</summary>
        /// <remarks>Change the label later with GetButtonCaption(button).setText(text).</remarks>
        /// <param name="width">
        /// A positive value fixes the width. A value &lt;= 0 makes the button share spare width with other flexible controls.
        /// </param>
        /// <param name="normalColor">
        /// Optional background tint. Hover and press colors are calculated from it.
        /// </param>
        /// <param name="alignment">
        /// Use TextAnchor.MiddleLeft, TextAnchor.MiddleCenter, or TextAnchor.MiddleRight. Text is vertically centered.
        /// </param>
        public static Button CreateThemedButton(Transform parent, string label, Action onClick, float width = 0f, float height = BUTTON_HEIGHT, string tooltip = null, Color? normalColor = null, TextAnchor alignment = TextAnchor.MiddleCenter) {
            var go = CreateUIObject(parent, "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(width > 0f ? width : 100f, height); // only read outside a layout group
            var le = FixHeight(go, height);
            if (width > 0f) {
                FixWidth(go, width);
            } else {
                le.flexibleWidth = 1f;
            }

            var bg = go.GetComponent<Image>();
            ApplyPanelSprite(bg, Color.white); // white lets Button.colors supply the normal, hover and press colors

            var labelGo = CreateUIObject(go.transform, "Text", typeof(RectTransform));
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            if (alignment != TextAnchor.MiddleCenter) {
                labelRt.offsetMin = new Vector2(CAPTION_INSET, 0f);
                labelRt.offsetMax = new Vector2(-CAPTION_INSET, 0f);
            }
            var caption = AddThemedText(labelGo, label, ToTmpAlignment(alignment), false);
            caption.raycastTarget = false; // Send hover, click, and tooltip events to the button background.

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            if (normalColor != null) {
                var baseColor = normalColor.Value;
                colors.normalColor = baseColor;
                colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.15f);
                colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.12f);
                colors.selectedColor = baseColor;
            } else {
                colors.normalColor = ButtonColor;
                colors.highlightedColor = ButtonHighlightColor;
                colors.pressedColor = ButtonPressedColor;
                colors.selectedColor = ButtonColor;
            }
            colors.disabledColor = ButtonDisabledColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            Tooltips.Attach(go, tooltip);
            return btn;
        }

        /// <summary>Creates a themed button with an icon instead of a text label.</summary>
        /// <remarks>
        /// Use a white icon on a transparent background. The button tints it to the dark text color.
        /// For a button with two states, change GetButtonIcon(button).sprite when the state changes.
        /// </remarks>
        public static Button CreateThemedIconButton(Transform parent, Sprite icon, Action onClick, float width = 30f, float height = BUTTON_HEIGHT, string tooltip = null) {
            var btn = CreateThemedButton(parent, "", onClick, width, height, tooltip);
            var iconGo = CreateUIObject(btn.transform, ICON_NAME, typeof(RectTransform), typeof(Image));
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(ICON_INSET, ICON_INSET);
            iconRt.offsetMax = new Vector2(-ICON_INSET, -ICON_INSET);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = TextColor;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false; // Send hover, click, and tooltip events to the button background.
            return btn;
        }

        /// <summary>Returns the icon Image in a button made by CreateThemedIconButton.</summary>
        /// <returns>Null if button is null or has no icon child, including a regular text button.</returns>
        public static Image GetButtonIcon(Button button) {
            if (button == null) return null;
            var iconTransform = button.transform.Find(ICON_NAME);
            return iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        }

        /// <summary>Applies ButtonSelectedColor, or restores the button's unselected background.</summary>
        /// <remarks>
        /// Changes the background Image color while preserving normal hover and press feedback.
        /// </remarks>
        public static void SetButtonSelected(Button button, bool selected) {
            if (button == null) return;
            var bg = button.GetComponent<Image>();
            if (bg != null) bg.color = selected ? ButtonSelectedColor : Color.white;
        }

        /// <summary>Creates a small dark button for arrows, plus or minus beside a value field.</summary>
        /// <remarks>Used by OptionRow and NumberRow. Its colors match input fields.</remarks>
        /// <param name="glyph">
        /// Symbol to display in the game's font. Prefer basic ASCII where possible. Unsupported characters may appear blank
        /// or as squares. See docs/GUIDE.md section 6 regarding supported symbols.
        /// </param>
        public static Button CreateStepperButton(Transform parent, string glyph, Action onClick, float width = STEPPER_WIDTH, float height = FIELD_HEIGHT) {
            var go = CreateUIObject(parent, "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            FixWidth(go, width);
            FixHeight(go, height);
            var bg = go.GetComponent<Image>();
            bg.color = FieldColor;

            var labelGo = CreateUIObject(go.transform, "Text", typeof(RectTransform));
            var rt = (RectTransform)labelGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = AddThemedText(labelGo, glyph, TextAlignmentOptions.Center, false);
            text.color = TextEntryTextColor;
            text.raycastTarget = false;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor) {
            switch (anchor) {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft: {
                        return TextAlignmentOptions.MidlineLeft;
                    }
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight: {
                        return TextAlignmentOptions.MidlineRight;
                    }
                default: {
                        return TextAlignmentOptions.Center;
                    }
            }
        }

        // ---== Input fields ==---

        /// <summary>Creates a single-line input field styled like Parkitect's rounded search box.</summary>
        /// <remarks>
        /// In a layout group, add a LayoutElement to set its size. The default RectTransform size applies outside a layout group.
        /// </remarks>
        public static InputField CreateInputField(Transform parent, string initial, InputField.ContentType contentType = InputField.ContentType.Standard, string placeholder = "", int fontSize = 13) {
            var inputGo = CreateUIObject(parent, "Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            var rt = (RectTransform)inputGo.transform;
            rt.sizeDelta = new Vector2(60f, FIELD_HEIGHT);
            var inputBg = inputGo.GetComponent<Image>();
            inputBg.color = FieldColor;

            var textGo = CreateUIObject(inputGo.transform, "Text", typeof(RectTransform), typeof(Text));
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(6f, 2f);
            textRt.offsetMax = new Vector2(-6f, -2f);
            var text = textGo.GetComponent<Text>();
            text.font = DefaultFont;
            text.fontSize = fontSize;
            text.color = TextEntryTextColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.text = initial ?? "";

            var placeholderGo = CreateUIObject(inputGo.transform, "Placeholder", typeof(RectTransform), typeof(Text));
            var phRt = (RectTransform)placeholderGo.transform;
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(6f, 2f);
            phRt.offsetMax = new Vector2(-6f, -2f);
            var ph = placeholderGo.GetComponent<Text>();
            ph.font = DefaultFont;
            ph.fontSize = fontSize;
            ph.alignment = TextAnchor.MiddleLeft;
            ph.raycastTarget = false;
            ph.text = placeholder ?? "";

            var input = inputGo.GetComponent<InputField>();
            input.targetGraphic = inputBg;
            input.textComponent = text;
            input.placeholder = ph;
            input.text = initial ?? "";
            input.contentType = contentType;
            input.lineType = InputField.LineType.SingleLine;
            ApplySearchFieldStyle(input);
            return input;
        }

        /// <summary>Styles an existing InputField with a dark rounded background, light text and a dimmed placeholder.</summary>
        public static void ApplySearchFieldStyle(InputField input) {
            if (input == null) return;
            if (input.targetGraphic is Image bg) ApplyPanelSprite(bg, FieldColor);
            if (input.textComponent != null) input.textComponent.color = TextEntryTextColor;
            if (input.placeholder is Text ph) ph.color = new Color(TextEntryTextColor.r, TextEntryTextColor.g, TextEntryTextColor.b, 0.45f);
            input.caretColor = TextEntryTextColor;
            input.customCaretColor = true;
        }
    }
}
