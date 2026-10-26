using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A single-line text field styled like Parkitect's rounded search box.</summary>
    /// <remarks>
    /// Calls onCommit on Enter or focus loss. If provided, onChanged runs as the text changes, for example for live filtering.
    /// Pass an empty or null label for a bare full-width field.
    /// </remarks>
    public class TextRow {
        public GameObject Root { get; private set; }
        public InputField Input { get; private set; }

        private readonly Action<string> onCommit;
        private readonly Action<string> onChanged;
        private bool suppress;

        /// <param name="label">Null or empty makes the field full-width.</param>
        /// <param name="characterLimit">Maximum number of characters. Zero or less makes the field unlimited.</param>
        public static TextRow Create(Transform parent, string label, string initial, Action<string> onCommit, Action<string> onChanged = null, float labelWidth = UIBuilder.DEFAULT_LABEL_WIDTH, string tooltip = null, string placeholder = "", int characterLimit = 0) {
            return new TextRow(parent, label, initial, onCommit, onChanged, labelWidth, tooltip, placeholder, characterLimit);
        }

        private TextRow(Transform parent, string label, string initial, Action<string> onCommit, Action<string> onChanged, float labelWidth, string tooltip, string placeholder, int characterLimit) {
            UILog.Debug("TextRow.ctor: " + label);
            this.onCommit = onCommit;
            this.onChanged = onChanged;

            Root = UIBuilder.CreateHorizontalRow(parent, "TextRow", UIBuilder.ROW_HEIGHT);

            if (!string.IsNullOrEmpty(label)) UIBuilder.CreateRowLabel(Root.transform, label, labelWidth);

            Input = UIBuilder.CreateInputField(Root.transform, initial, InputField.ContentType.Standard, placeholder);
            if (characterLimit > 0) Input.characterLimit = characterLimit;
            var inputLe = UIBuilder.FixHeight(Input.gameObject, UIBuilder.FIELD_HEIGHT);
            inputLe.flexibleWidth = 1f;
            inputLe.minWidth = UIBuilder.FIELD_MIN_WIDTH;

            Input.onEndEdit.AddListener(onInputEndEdit);
            if (onChanged != null) Input.onValueChanged.AddListener(onInputChanged);
            Tooltips.Attach(Root, tooltip);
        }

        public string Value => Input != null ? Input.text : "";

        /// <summary>Replaces the field's text without calling onCommit or onChanged.</summary>
        public void setValue(string value) {
            suppress = true;
            Input.text = value ?? "";
            suppress = false;
        }

        public void setInteractable(bool interactable) {
            if (Input != null) Input.interactable = interactable;
        }

        private void onInputChanged(string text) {
            if (suppress) return;
            onChanged?.Invoke(text);
        }

        private void onInputEndEdit(string text) {
            if (suppress) return;
            onCommit?.Invoke(text);
        }
    }
}
