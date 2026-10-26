using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ModUIToolkit.UI {
    /// <summary>A dropdown row based on Parkitect's themed AttractionSettingDropdownUI prefab.</summary>
    /// <remarks>
    /// The label sits above the control, leaving the full row width for option names.
    /// </remarks>
    public class DropdownRow {
        /// <summary>Default height for the label above a full-width dropdown.</summary>
        public const float DEFAULT_ROW_HEIGHT = 46f;
        /// <summary>The minimum height that fits the prefab's 16-unit label above its 14-unit control.</summary>
        public const float COMPACT_ROW_HEIGHT = 30f;

        public GameObject Root { get; private set; }

        private readonly Action<int> onChanged;
        private TMP_Dropdown dropdown;
        private bool suppress;

        public static DropdownRow Create(Transform parent, string label, string[] options, int initial, Action<int> onChanged, string tooltip = null, float rowHeight = DEFAULT_ROW_HEIGHT) {
            return new DropdownRow(parent, label, options, initial, onChanged, tooltip, rowHeight);
        }

        private DropdownRow(Transform parent, string label, string[] options, int initial, Action<int> onChanged, string tooltip, float rowHeight) {
            UILog.Debug("DropdownRow.ctor: " + label);
            this.onChanged = onChanged;
            options = options ?? new string[0];

            buildThemed(parent, label, options, initial, rowHeight);

            Tooltips.Attach(Root, tooltip);
        }

        private void buildThemed(Transform parent, string label, string[] options, int initial, float rowHeight) {
            var wrapper = ThemedPrefabs.CloneInactive(m => m.attractionSettingDropdownUIGO);

            wrapper.setLabelText(label);
            dropdown = wrapper.GetComponentInChildren<TMP_Dropdown>(true);
            var go = wrapper.gameObject;

            // Remove the wrapper before activation; keep the dropdown itself for this row to control.
            ThemedPrefabs.StripWrappers(go);

            go.name = "DropdownRow";
            go.transform.SetParent(parent, false);
            UIBuilder.FixHeight(go, rowHeight);

            applyOptions(options, initial);
            dropdown.onValueChanged.AddListener(onValueChanged);

            // Use dark text for the row label and keep light text inside the dark dropdown and popup list.
            UIBuilder.DarkenLabelText(go, label);

            go.SetActive(true);
            Root = go;
        }

        private void applyOptions(string[] options, int selected) {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            var index = options.Length > 0 ? Mathf.Clamp(selected, 0, options.Length - 1) : 0;
            dropdown.SetValueWithoutNotify(index);
            dropdown.RefreshShownValue();
        }

        public int SelectedIndex => dropdown.value;

        /// <summary>Selects an index without calling onChanged.</summary>
        public void setSelectedIndex(int index) {
            suppress = true;
            var clamped = dropdown.options.Count > 0 ? Mathf.Clamp(index, 0, dropdown.options.Count - 1) : 0;
            dropdown.SetValueWithoutNotify(clamped);
            dropdown.RefreshShownValue();
            suppress = false;
        }

        /// <summary>Replaces the options without calling onChanged, keeping the current index when it is still valid.</summary>
        public void setOptions(string[] options) {
            options = options ?? new string[0];
            suppress = true;
            applyOptions(options, dropdown.value);
            suppress = false;
        }

        public void setInteractable(bool interactable) {
            dropdown.interactable = interactable;
        }

        private void onValueChanged(int index) {
            if (suppress) return;
            onChanged?.Invoke(index);
        }
    }
}
