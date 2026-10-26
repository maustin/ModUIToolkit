using System;
using System.Collections.Generic;
using Parkitect.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A dropdown for actions such as "Apply preset", with a fixed button label.</summary>
    /// <remarks>
    /// Each choice calls onPicked. The fixed label is restored before the callback so the same option can be chosen again.
    /// Disabled when it has no options or after setInteractable(false). Uses the dropdown from Parkitect's
    /// AttractionSettingDropdownUI prefab without its row label. TMP's placeholder displays the fixed label while value is -1.
    /// </remarks>
    public class ActionDropdown {
        private const float MIN_WIDTH = 80f;
        private const float DISABLED_CAPTION_ALPHA = 0.45f;

        public GameObject Root { get; private set; }

        private readonly string caption;
        private readonly Action<string> onPicked;
        private string[] names = new string[0];
        private bool suppress;
        private bool interactable = true;

        private TMP_Dropdown dropdown;
        private TMP_Text placeholderText;
        private Color placeholderColor;

        public static ActionDropdown Create(Transform parent, string caption, Action<string> onPicked, string tooltip = null) {
            return new ActionDropdown(parent, caption, onPicked, tooltip);
        }

        private ActionDropdown(Transform parent, string caption, Action<string> onPicked, string tooltip) {
            UILog.Debug("ActionDropdown.ctor: " + caption);
            this.caption = caption;
            this.onPicked = onPicked;

            buildThemed(parent);

            Tooltips.Attach(Root, tooltip);
            setNames(names);
        }

        private void buildThemed(Transform parent) {
            UILog.Debug("ActionDropdown.buildThemed");
            var wrapper = ThemedPrefabs.CloneInactive(m => m.attractionSettingDropdownUIGO);

            var wrapperGO = wrapper.gameObject;
            dropdown = wrapper.GetComponentInChildren<TMP_Dropdown>(true);

            // Remove the wrapper before activation and keep only the dropdown; this control has no separate row label.
            ThemedPrefabs.StripWrappers(wrapperGO);
            var go = dropdown.gameObject;
            go.name = "ActionDropdown";
            go.transform.SetParent(parent, false);
            UnityEngine.Object.DestroyImmediate(wrapperGO);

            var le = UIBuilder.FixHeight(go, UIBuilder.BUTTON_HEIGHT);
            le.minWidth = MIN_WIDTH;
            le.flexibleWidth = 1f;

            // Clone the dropdown's label text so the placeholder keeps the same font, size, and placement.
            // TMP shows it while value == -1, the dropdown's resting state.
            var placeholderGO = UnityEngine.Object.Instantiate(dropdown.captionText.gameObject, dropdown.captionText.transform.parent, false);
            placeholderGO.name = "Placeholder";
            placeholderText = placeholderGO.GetComponent<TMP_Text>();
            placeholderText.text = caption;
            placeholderColor = placeholderText.color;
            dropdown.placeholder = placeholderText;

            attachItemTooltips();
            dropdown.onValueChanged.AddListener(onValueChanged);

            go.SetActive(true);
            Root = go;
        }

        // TMP clones each popup entry from this template. Add the tooltip components here so each entry gets them too;
        // ItemTooltip fills in the option text and enables the tooltip only when the text is clipped.
        private void attachItemTooltips() {
            var itemTemplate = dropdown.template != null ? dropdown.template.GetComponentInChildren<Toggle>(true) : null;
            if (itemTemplate == null) {
                UILog.Info("ActionDropdown: no item template found, long names will have no tooltip");
                return;
            }
            var tip = itemTemplate.gameObject.AddComponent<UITooltip>();
            tip.globalText = GlobalStrings.GlobalText.None;
            tip.enabled = false;
            itemTemplate.gameObject.AddComponent<ItemTooltip>();
        }

        // Start runs after TMP sets the entry text and the canvas lays out the list, so the final label width is known.
        private class ItemTooltip : MonoBehaviour {
            private void Start() {
                var label = GetComponentInChildren<TMP_Text>(true);
                var tip = GetComponent<UITooltip>();
                if (label == null || tip == null || string.IsNullOrEmpty(label.text)) return;
                bool clipped = label.preferredWidth > label.rectTransform.rect.width + 0.5f;
                tip.text = label.text;
                tip.enabled = clipped;
            }
        }

        /// <summary>Replaces the listed entries and disables the control when the list is empty.</summary>
        public void setNames(IList<string> entries) {
            names = entries != null ? new List<string>(entries).ToArray() : new string[0];

            suppress = true;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(names));
            dropdown.SetValueWithoutNotify(-1);
            dropdown.RefreshShownValue();
            suppress = false;
            if (placeholderText != null) placeholderText.text = caption;
            applyInteractable();
        }

        public void setInteractable(bool interactable) {
            this.interactable = interactable;
            applyInteractable();
        }

        // Enable only when the caller allows interaction and options exist; dim the label otherwise.
        private void applyInteractable() {
            bool enabled = interactable && names.Length > 0;
            dropdown.interactable = enabled;
            if (placeholderText != null) {
                placeholderText.color = enabled ? placeholderColor : new Color(placeholderColor.r, placeholderColor.g, placeholderColor.b, placeholderColor.a * DISABLED_CAPTION_ALPHA);
            }
        }

        private void onValueChanged(int index) {
            if (suppress) return;
            if (index < 0 || index >= names.Length) return;
            var name = names[index];
            // Return to "nothing selected" so the label reappears and this option can be chosen again.
            // TMP ignores a value set equal to the current one, so leaving the selected value would block a repeat.
            suppress = true;
            dropdown.SetValueWithoutNotify(-1);
            dropdown.RefreshShownValue();
            suppress = false;
            onPicked?.Invoke(name);
        }
    }
}
