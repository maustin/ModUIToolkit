using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A numeric entry row with optional nudge buttons and no slider.</summary>
    /// <remarks>
    /// The field calls onCommit on Enter or focus loss. Nudge buttons call onNudge with a direction; the caller chooses
    /// the step and updates the field with setValue. onCommit receives the parsed value. Unparseable text stays in the
    /// field until setValue replaces it.
    /// </remarks>
    public class NumberRow {
        public GameObject Root { get; private set; }
        public InputField Input { get; private set; }

        private readonly string format;
        private readonly Action<float> onCommit;
        private readonly Button minusButton; // both nudge buttons are null when no onNudge is set
        private readonly Button plusButton;
        private bool suppress;

        /// <param name="onNudge">If provided, adds minus and plus buttons that pass -1 or 1. Apply the step and update the field with setValue.</param>
        /// <param name="format">A .NET numeric format for the field: "0" for integers, "0.0" for tenths.</param>
        public static NumberRow Create(Transform parent, string label, float initial, Action<float> onCommit, Action<int> onNudge = null, float labelWidth = UIBuilder.DEFAULT_LABEL_WIDTH, string tooltip = null, string format = "0.00") {
            return new NumberRow(parent, label, initial, onCommit, onNudge, labelWidth, tooltip, format);
        }

        private NumberRow(Transform parent, string label, float initial, Action<float> onCommit, Action<int> onNudge, float labelWidth, string tooltip, string format) {
            UILog.Debug("NumberRow.ctor: " + label);
            this.format = format;
            this.onCommit = onCommit;

            Root = UIBuilder.CreateHorizontalRow(parent, "NumberRow", UIBuilder.ROW_HEIGHT);

            UIBuilder.CreateRowLabel(Root.transform, label, labelWidth);

            if (onNudge != null) minusButton = UIBuilder.CreateStepperButton(Root.transform, "-", () => onNudge(-1));

            Input = UIBuilder.CreateInputField(Root.transform, initial.ToString(format, CultureInfo.InvariantCulture), InputField.ContentType.DecimalNumber);
            var inputLe = UIBuilder.FixHeight(Input.gameObject, UIBuilder.FIELD_HEIGHT);
            inputLe.flexibleWidth = 1f;
            inputLe.minWidth = UIBuilder.FIELD_MIN_WIDTH;

            if (onNudge != null) plusButton = UIBuilder.CreateStepperButton(Root.transform, "+", () => onNudge(1));

            Input.onEndEdit.AddListener(onInputEndEdit);
            Tooltips.Attach(Root, tooltip);
        }

        /// <summary>Sets the field value without calling onCommit.</summary>
        /// <remarks>Avoid per-frame updates while Input.isFocused, since they replace text the player is typing.</remarks>
        public void setValue(float v) {
            suppress = true;
            Input.text = v.ToString(format, CultureInfo.InvariantCulture);
            suppress = false;
        }

        public void setInteractable(bool interactable) {
            if (Input != null) Input.interactable = interactable;
            if (minusButton != null) minusButton.interactable = interactable;
            if (plusButton != null) plusButton.interactable = interactable;
        }

        private void onInputEndEdit(string text) {
            if (suppress) return;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return; // Keep invalid text until the next setValue call.
            setValue(v);
            onCommit?.Invoke(v);
        }
    }
}
