using System;
using System.Collections.Generic;
using Parkitect.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A color swatch control that opens Parkitect's color picker when clicked.</summary>
    /// <remarks>
    /// Each swatch is a slot in the button's palette. `Create` makes a one-color row; `CreateMulti` or `addSwatch` adds more
    /// swatches to the same button, up to the native picker's four-slot limit.
    ///
    /// onChanged is called for each live color change from the picker or a swatch drop. onCommit is called once
    /// when an edited picker closes. Drops on rows without an open picker commit once per row at the end of the
    /// frame, after all changed slots have reported onChanged, or when the row is disabled if that happens first.
    /// A drop on the row with the open picker joins that picker edit. Parkitect's picker supports only fully opaque colors.
    /// </remarks>
    public class ColorSwatchRow {
        /// <summary>Maximum number of editable swatches supported by Parkitect's native picker.</summary>
        public const int MAX_SWATCH_COUNT = 4;

        private const float SWATCH_HEIGHT = 18f;

        public GameObject Root { get; private set; }

        private ColorPickerButton pickerButton;
        private readonly List<Color> colors = new List<Color>();
        private readonly List<Action<Color>> slotCallbacks = new List<Action<Color>>();
        private readonly Action<int, Color> indexedCallback;
        private readonly Action onCommit;
        private bool pickerOpen;
        private DropCommitRelay dropCommitRelay;

        private static ColorSwatchRow uncommittedRow;

        /// <summary>Returns true while a picker has selected a color that has not yet been committed.</summary>
        /// <remarks>
        /// Parkitect opens one color picker at a time, so this covers every row. Hold external updates while it is true
        /// to avoid overwriting the player's in-progress edit. Drops on other rows commit separately and do not change it.
        /// </remarks>
        public static bool PickerUncommitted {
            get {
                if (uncommittedRow == null) return false;
                if (uncommittedRow.Root == null || !uncommittedRow.pickerOpen) {
                    uncommittedRow = null;
                    return false;
                }
                return true;
            }
        }

        public static ColorSwatchRow Create(Transform parent, string label, Color initial, Action<Color> onChanged, Action onCommit = null, float labelWidth = UIBuilder.DEFAULT_LABEL_WIDTH, string tooltip = null) {
            var row = new ColorSwatchRow(parent, label, null, onCommit, labelWidth, tooltip);
            row.addSwatch(initial, onChanged);
            return row;
        }

        /// <summary>Creates a color picker with multiple swatches. onChanged receives the changed slot index and color.</summary>
        /// <remarks>Throws ArgumentException before creating the row if initial contains more than MAX_SWATCH_COUNT colors.</remarks>
        public static ColorSwatchRow CreateMulti(Transform parent, string label, Color[] initial, Action<int, Color> onChanged, Action onCommit = null, float labelWidth = UIBuilder.DEFAULT_LABEL_WIDTH, string tooltip = null) {
            if (initial != null && initial.Length > MAX_SWATCH_COUNT) throw new ArgumentException("Parkitect's color picker supports at most " + MAX_SWATCH_COUNT + " swatches.", nameof(initial));
            var row = new ColorSwatchRow(parent, label, onChanged, onCommit, labelWidth, tooltip);
            if (initial != null) {
                foreach (var color in initial) row.addSwatch(color, null);
            }
            return row;
        }

        private ColorSwatchRow(Transform parent, string label, Action<int, Color> indexedCallback, Action onCommit, float labelWidth, string tooltip) {
            UILog.Debug("ColorSwatchRow.ctor: " + label);
            this.onCommit = onCommit;
            this.indexedCallback = indexedCallback;

            Root = UIBuilder.CreateHorizontalRow(parent, "ColorSwatchRow", UIBuilder.ROW_HEIGHT, childControlWidth: false); // The picker sizes itself with its ContentSizeFitter.
            // Shift the row contents down slightly to separate them from the control above.
            Root.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 5, -3);

            UIBuilder.CreateRowLabel(Root.transform, label, labelWidth);

            buildPicker();
            Tooltips.Attach(Root, tooltip);
        }

        // This prefab can be cloned active: Awake removes placeholder children, and setPalette creates the swatches.
        private void buildPicker() {
            UILog.Debug("ColorSwatchRow.buildPicker");

            var prefab = ThemedPrefabs.Require(m => m.colorPickerButton);

            pickerButton = UnityEngine.Object.Instantiate(prefab);
            pickerButton.name = "ColorPickerButton";
            pickerButton.transform.SetParent(Root.transform, false);
            pickerButton.launchColorPicker = true;
            UIBuilder.FixHeight(pickerButton.gameObject, SWATCH_HEIGHT);
            pickerButton.OnColorChanged += onPickerColorChanged;
            pickerButton.OnColorPickerOpened += onPickerOpened;
            pickerButton.OnColorPickerClosed += onPickerClosed;
            if (onCommit != null) {
                dropCommitRelay = pickerButton.gameObject.AddComponent<DropCommitRelay>();
                dropCommitRelay.OnCommit = onCommit;
            }
        }

        public int Count => colors.Count;

        /// <summary>Adds a swatch slot. onChanged may be null when CreateMulti's indexed callback handles every slot.</summary>
        /// <remarks>
        /// Throws InvalidOperationException without changing the row if it already contains MAX_SWATCH_COUNT swatches.
        /// Adding a slot may increase the row height. Resize the window afterward if you add slots after opening it.
        /// </remarks>
        public void addSwatch(Color initial, Action<Color> onChanged) {
            if (colors.Count >= MAX_SWATCH_COUNT) throw new InvalidOperationException("Parkitect's color picker supports at most " + MAX_SWATCH_COUNT + " swatches.");
            colors.Add(Opaque(initial));
            slotCallbacks.Add(onChanged);
            refreshPalette();
        }

        public Color getColor(int index = 0) {
            return index >= 0 && index < colors.Count ? colors[index] : Color.white;
        }

        /// <summary>Sets the first swatch's color without calling onChanged or onCommit.</summary>
        /// <remarks>Alpha is forced to 1.</remarks>
        public void setColor(Color color) {
            setColor(0, color);
        }

        /// <summary>Sets a swatch's color without calling onChanged or onCommit.</summary>
        /// <remarks>Does nothing if index is outside the palette. Alpha is forced to 1.</remarks>
        public void setColor(int index, Color color) {
            if (index < 0 || index >= colors.Count) return;
            colors[index] = Opaque(color);
            refreshPalette();
        }

        /// <summary>Updates existing swatches without calling onChanged or onCommit.</summary>
        /// <remarks>
        /// Does not add or remove slots. Extra colors are ignored; slots beyond the supplied array are unchanged.
        /// Alpha is forced to 1.
        /// </remarks>
        public void setColors(Color[] value) {
            if (value == null) return;
            for (int i = 0; i < colors.Count && i < value.Length; i++) {
                colors[i] = Opaque(value[i]);
            }
            refreshPalette();
        }

        /// <summary>Sets whether the picker accepts interaction. When disabled, the swatch is shown at half opacity.</summary>
        /// <remarks>
        /// A CanvasGroup on the picker button disables its swatches, drag-and-drop, and picker opening together.
        /// It also covers new swatches created when setPalette rebuilds the button.
        /// </remarks>
        public void setInteractable(bool interactable) {
            pickerButton.launchColorPicker = interactable;
            var group = pickerButton.GetComponent<CanvasGroup>() ?? pickerButton.gameObject.AddComponent<CanvasGroup>();
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
            group.alpha = interactable ? 1f : 0.5f; // dim disabled swatches to match other controls
        }

        private void refreshPalette() {
            if (colors.Count == 0) return;
            pickerButton.setPalette(colors.ToArray(), colors.Count);
            fitHeight();
        }

        // Our fixed heights override the grid's preferred height. Add a cell height plus spacing for each extra line so wrapped swatches fit.
        private void fitHeight() {
            var grid = pickerButton.GetComponent<GridLayoutGroup>();
            float extra = grid == null ? 0f : (countGridLines(grid) - 1) * (grid.cellSize.y + grid.spacing.y);
            UIBuilder.FixHeight(pickerButton.gameObject, SWATCH_HEIGHT + extra);
            UIBuilder.FixHeight(Root, UIBuilder.ROW_HEIGHT + extra);
        }

        private int countGridLines(GridLayoutGroup grid) {
            if (colors.Count <= 1) return 1;
            int columns;
            switch (grid.constraint) {
                case GridLayoutGroup.Constraint.FixedColumnCount: {
                        columns = grid.constraintCount;
                        break;
                    }
                case GridLayoutGroup.Constraint.FixedRowCount: {
                        return Mathf.Max(1, Mathf.Min(grid.constraintCount, colors.Count));
                    }
                default: {
                        // Before layout, the rect may have no width. Match the grid's square-root column rule when its fitter uses preferred width.
                        var fitter = pickerButton.GetComponent<ContentSizeFitter>();
                        if (fitter != null && fitter.horizontalFit == ContentSizeFitter.FitMode.PreferredSize) {
                            columns = Mathf.CeilToInt(Mathf.Sqrt(colors.Count));
                        } else {
                            float width = pickerButton.GetComponent<RectTransform>().rect.width;
                            if (width <= 0f) return 1;
                            columns = Mathf.FloorToInt((width - grid.padding.horizontal + grid.spacing.x + 0.001f) / (grid.cellSize.x + grid.spacing.x));
                        }
                        break;
                    }
            }
            columns = Mathf.Max(1, columns);
            return Mathf.CeilToInt(colors.Count / (float)columns);
        }

        private void onPickerColorChanged(int index) {
            if (index < 0 || index >= colors.Count) return;
            colors[index] = Opaque(pickerButton[index]);
            if (pickerOpen) {
                uncommittedRow = this;
            } else {
                dropCommitRelay?.queueCommit();
            }
            var slotCallback = slotCallbacks[index];
            if (slotCallback != null) slotCallback(colors[index]);
            indexedCallback?.Invoke(index, colors[index]);
        }

        private void onPickerOpened() {
            pickerOpen = true;
            dropCommitRelay?.commitPending();
        }

        private void onPickerClosed() {
            pickerOpen = false;
            if (uncommittedRow != this) return;
            uncommittedRow = null;
            onCommit?.Invoke();
        }

        // Native drops can swap two slots synchronously. Commit after both live updates, once for this row.
        private class DropCommitRelay : MonoBehaviour {
            public Action OnCommit;
            private bool pending;

            public void queueCommit() {
                pending = true;
            }

            public void commitPending() {
                if (!pending) return;
                pending = false;
                OnCommit?.Invoke();
            }

            private void LateUpdate() {
                commitPending();
            }

            private void OnDisable() {
                commitPending();
            }
        }

        private static Color Opaque(Color c) {
            return new Color(c.r, c.g, c.b, 1f);
        }
    }
}
