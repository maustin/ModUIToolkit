using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A row with arrow buttons to cycle through named options, calling onChanged with the selected index.</summary>
    /// <remarks>
    /// Useful for short lists the player can step through while watching the result.
    /// For longer lists, prefer using DropdownRow.
    /// </remarks>
    public class OptionRow {
        public GameObject Root { get; private set; }

        private string[] options;
        private readonly Action<int> onChanged;
        private readonly UILabel valueLabel;
        private readonly Button previousButton;
        private readonly Button nextButton;
        private int index;

        public static OptionRow Create(Transform parent, string label, string[] options, int initial, Action<int> onChanged, float labelWidth = UIBuilder.DEFAULT_LABEL_WIDTH, string tooltip = null) {
            return new OptionRow(parent, label, options, initial, onChanged, labelWidth, tooltip);
        }

        private OptionRow(Transform parent, string label, string[] options, int initial, Action<int> onChanged, float labelWidth, string tooltip) {
            UILog.Debug("OptionRow.ctor: " + label);
            this.options = options ?? new string[0];
            this.onChanged = onChanged;
            this.index = this.options.Length > 0 ? Mathf.Clamp(initial, 0, this.options.Length - 1) : 0;

            Root = UIBuilder.CreateHorizontalRow(parent, "OptionRow", UIBuilder.ROW_HEIGHT);

            UIBuilder.CreateRowLabel(Root.transform, label, labelWidth);

            previousButton = UIBuilder.CreateStepperButton(Root.transform, "◀", () => step(-1));

            valueLabel = UIBuilder.CreateThemedLabel(Root.transform, currentLabel(), alignment: TextAnchor.MiddleCenter);
            var valueLe = UIBuilder.EnsureLayoutElement(valueLabel.Root);
            valueLe.flexibleWidth = 1f;
            valueLe.minWidth = 80f;

            nextButton = UIBuilder.CreateStepperButton(Root.transform, "▶", () => step(1));

            Tooltips.Attach(Root, tooltip);
        }

        public int SelectedIndex => index;

        private string currentLabel() {
            return options.Length > 0 ? options[index] : "(none)";
        }

        /// <summary>Replaces the options without calling onChanged, keeping the current index when it is still valid.</summary>
        public void setOptions(string[] options) {
            this.options = options ?? new string[0];
            index = this.options.Length > 0 ? Mathf.Clamp(index, 0, this.options.Length - 1) : 0;
            valueLabel.setText(currentLabel());
        }

        /// <summary>Selects an index without calling onChanged.</summary>
        public void setSelectedIndex(int index) {
            this.index = options.Length > 0 ? Mathf.Clamp(index, 0, options.Length - 1) : 0;
            valueLabel.setText(currentLabel());
        }

        public void setInteractable(bool interactable) {
            previousButton.interactable = interactable;
            nextButton.interactable = interactable;
        }

        private void step(int dir) {
            if (options.Length == 0) return;
            index = (index + dir + options.Length) % options.Length;
            valueLabel.setText(currentLabel());
            onChanged?.Invoke(index);
        }
    }
}
