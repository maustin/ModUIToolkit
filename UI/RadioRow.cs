using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A labeled set of buttons where only one option may be selected at a time.</summary>
    /// <remarks>
    /// Clicking the selected option again does nothing. Buttons share available width when buttonWidth is 0 or less;
    /// positive values set a fixed width. The row is 2 units taller than each button.
    /// </remarks>
    public class RadioRow {
        public GameObject Root { get; private set; }
        public int SelectedIndex { get; private set; }

        private readonly Action<int> onChanged;
        private readonly List<Button> buttons = new List<Button>();

        /// <param name="label">
        /// Null or empty gives a bare button group.
        /// </param>
        /// <param name="buttonWidth">Values at or below 0 share available width; positive values set each button's width.</param>
        public static RadioRow Create(Transform parent, string label, string[] options, int initial, Action<int> onChanged, float labelWidth = UIBuilder.DEFAULT_LABEL_WIDTH, string tooltip = null, float buttonWidth = 0f, float height = UIBuilder.BUTTON_HEIGHT) {
            return new RadioRow(parent, label, options, initial, onChanged, labelWidth, tooltip, buttonWidth, height);
        }

        private RadioRow(Transform parent, string label, string[] options, int initial, Action<int> onChanged, float labelWidth, string tooltip, float buttonWidth, float height) {
            UILog.Debug("RadioRow.ctor: " + label);
            this.onChanged = onChanged;
            options = options ?? new string[0];

            Root = UIBuilder.CreateHorizontalRow(parent, "RadioRow", height + 2f, spacing: 4f);

            if (!string.IsNullOrEmpty(label)) UIBuilder.CreateRowLabel(Root.transform, label, labelWidth);

            for (int i = 0; i < options.Length; i++) {
                var index = i;
                buttons.Add(UIBuilder.CreateThemedButton(Root.transform, options[i], () => onClicked(index), buttonWidth, height));
            }

            SelectedIndex = options.Length > 0 ? Mathf.Clamp(initial, 0, options.Length - 1) : -1;
            refreshTints();
            Tooltips.Attach(Root, tooltip);
        }

        /// <summary>Selects an option without calling onChanged.</summary>
        public void setSelectedIndex(int index) {
            if (buttons.Count == 0) return;
            SelectedIndex = Mathf.Clamp(index, 0, buttons.Count - 1);
            refreshTints();
        }

        public void setInteractable(bool interactable) {
            foreach (var button in buttons) button.interactable = interactable;
        }

        private void onClicked(int index) {
            if (index == SelectedIndex) return; // Only a change calls onChanged; clicking the active option again does nothing.
            SelectedIndex = index;
            refreshTints();
            onChanged?.Invoke(index);
        }

        private void refreshTints() {
            for (int i = 0; i < buttons.Count; i++) {
                UIBuilder.SetButtonSelected(buttons[i], i == SelectedIndex);
            }
        }
    }
}
