using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A checkbox based on Parkitect's AttractionSettingToggleUI prefab.</summary>
    /// <remarks>
    /// The label sits beside the checkbox, following the game's toggle layout. The row is UIBuilder.ROW_HEIGHT tall.
    /// </remarks>
    public class ToggleRow {
        public GameObject Root { get; private set; }
        public Toggle Toggle { get; private set; }

        private readonly Action<bool> onChanged;

        public static ToggleRow Create(Transform parent, string label, bool initial, Action<bool> onChanged, string tooltip = null) {
            return new ToggleRow(parent, label, initial, onChanged, tooltip);
        }

        private ToggleRow(Transform parent, string label, bool initial, Action<bool> onChanged, string tooltip) {
            UILog.Debug("ToggleRow.ctor: " + label);
            this.onChanged = onChanged;

            buildThemed(parent, label);

            Toggle.SetIsOnWithoutNotify(initial);
            Toggle.onValueChanged.AddListener(onValueChanged);
            Tooltips.Attach(Root, tooltip);
        }

        public bool Value => Toggle.isOn;

        /// <summary>Sets the toggle value without calling onChanged.</summary>
        public void setValue(bool value) {
            Toggle.SetIsOnWithoutNotify(value);
        }

        public void setInteractable(bool interactable) {
            Toggle.interactable = interactable;
        }

        private void onValueChanged(bool value) {
            onChanged?.Invoke(value);
        }

        // Keep the prefab's Toggle and label while removing its attraction-setting wrapper.
        private void buildThemed(Transform parent, string label) {
            var wrapper = ThemedPrefabs.CloneInactive(m => m.attractionSettingToggleUIGO);

            wrapper.setLabelText(label);
            var toggle = wrapper.GetComponentInChildren<Toggle>(true);
            var go = wrapper.gameObject;

            ThemedPrefabs.StripWrappers(go);

            go.name = "ToggleRow";
            go.transform.SetParent(parent, false);
            UIBuilder.FixHeight(go, UIBuilder.ROW_HEIGHT);

            // Center the checkbox and label vertically; the prefab's default anchors leave them misaligned.
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) {
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlHeight = true;
                hlg.childForceExpandHeight = true;
            }
            CenterGraphicVertically(toggle.targetGraphic);
            CenterGraphicVertically(toggle.graphic);
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true)) {
                if (tmp == null) continue;
                tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                if (tmp.text == label) tmp.color = UIBuilder.TextColor;
            }

            go.SetActive(true);
            Root = go;
            Toggle = toggle;
        }

        // Center the box and checkmark on the label without changing their heights.
        private static void CenterGraphicVertically(Graphic graphic) {
            if (graphic == null) return;
            var rt = graphic.rectTransform;
            float height = rt.rect.height;
            var min = rt.anchorMin;
            var max = rt.anchorMax;
            var pivot = rt.pivot;
            min.y = 0.5f;
            max.y = 0.5f;
            pivot.y = 0.5f;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            if (height > 0f) {
                var size = rt.sizeDelta;
                size.y = height;
                rt.sizeDelta = size;
            }
            var pos = rt.anchoredPosition;
            pos.y = 0f;
            rt.anchoredPosition = pos;
        }
    }
}
