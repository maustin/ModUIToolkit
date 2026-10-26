using ModUIToolkit.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.Demo {
    // Shows the toolkit controls across three tabs. Static values keep edits when the window closes and reopens.
    public class DemoWindow : ThemedWindow {
        public const string WINDOW_TAG = "ModUIToolkitDemoWindow"; // a mod-specific string, never a game object (see ThemedWindowOptions.uniqueTag)
        private const float WINDOW_WIDTH = 320f;
        private const float LIST_MAX_HEIGHT = 160f;
        private const string ICON_EYE = "ModUIToolkit.Demo.demo_icon_eye.png";
        private const string ICON_EYE_STRIKE = "ModUIToolkit.Demo.demo_icon_eye_strike.png";

        private static readonly string[] FLAVORS = { "Vanilla", "Strawberry", "Chocolate Chip", "Absurdly Large Cowboy Hat" };
        private static readonly string[] SIZES = { "Small", "Medium", "Large" };
        private static readonly string[] MODES = { "Include", "Exclude" };

        // Values shown by the controls. Snapshots let Reset and Presets update the whole set at once.
        private class Snapshot {
            public float opacity = 0.75f;
            public int count = 3;
            public int flavor = 0;
            public int size = 1;
            public bool advanced = false;
            public float offset = 0f;
            public int seed = 1234;
            public Color tint = new Color(0.9f, 0.45f, 0.2f, 1f);
            public Color[] palette = { new Color(0.2f, 0.5f, 0.9f, 1f), new Color(0.95f, 0.85f, 0.3f, 1f), new Color(0.4f, 0.75f, 0.35f, 1f), new Color(0.7f, 0.35f, 0.8f, 1f) };
            public string name = "Untitled";
            public int mode = 0;

            public static Snapshot Duplicate(Snapshot source) {
                var copy = new Snapshot();
                copy.opacity = source.opacity;
                copy.count = source.count;
                copy.flavor = source.flavor;
                copy.size = source.size;
                copy.advanced = source.advanced;
                copy.offset = source.offset;
                copy.seed = source.seed;
                copy.tint = source.tint;
                copy.palette = (Color[])source.palette.Clone();
                copy.name = source.name;
                copy.mode = source.mode;
                return copy;
            }
        }

        private static Snapshot state = new Snapshot();
        private static readonly Dictionary<string, Snapshot> presets = new Dictionary<string, Snapshot>();
        private static readonly List<string> items = new List<string> { "Ferris wheel", "Log flume", "Teacups", "Wooden coaster" };
        private static int selectedItem = -1;
        private static int lastTab = 0; // When the window is reopened, show the previously selected tab

        private TabBar tabs;
        private SliderRow opacityRow;
        private SliderRow countRow;
        private DropdownRow flavorRow;
        private OptionRow sizeRow;
        private ToggleRow advancedRow;
        private GameObject advancedGroup;
        private NumberRow offsetRow;
        private InputField seedInput;
        private ColorSwatchRow tintRow;
        private ColorSwatchRow paletteRow;
        private TextRow nameRow;
        private RadioRow modeRow;
        private ActionDropdown presetDropdown;
        private UILabel summaryLabel;
        private UILabel itemCountLabel;
        private ScrollList itemList;
        private Image eyeIcon;
        private Button eyeButton;
        private Sprite eyeSprite;
        private Sprite eyeStrikeSprite;
        private bool eyeOpen = true;

        public static DemoWindow Open() {
            var options = new ThemedWindowOptions { title = "UI Toolkit Demo", width = WINDOW_WIDTH, uniqueTag = WINDOW_TAG, screenAnchor = new Vector2(0.3f, 0.6f) };
            return Spawn<DemoWindow>(options);
        }

        // Opens the window if closed, closes it if open. Triggered by hotkey.
        public static bool Toggle() {
            var open = FindOpen<DemoWindow>(WINDOW_TAG);
            if (open != null) {
                open.close();
                return false;
            }
            return Open() != null;
        }

        protected override void buildContent(RectTransform root) {
            UILog.Debug("DemoWindow.buildContent");

            tabs = TabBar.Create(root, onTabSelected);
            var controlsPane = tabs.addTab("Controls", tooltip: DemoTooltips.TAB_CONTROLS);
            var listsPane = tabs.addTab("Lists", tooltip: DemoTooltips.TAB_LISTS);
            var morePane = tabs.addTab("More", tooltip: DemoTooltips.TAB_MORE);

            buildControlsTab(controlsPane);
            buildListsTab(listsPane);
            buildMoreTab(morePane);
            tabs.select(lastTab);
        }

        protected override void onSpawned() {
            UILog.Info("DemoWindow open; close it with the 'X' button, Escape, or " + ModUIToolkitDemo.HotkeyName);
        }

        protected override void onClosing() {
            UILog.Debug("DemoWindow.onClosing");
        }

        private void onTabSelected(int index) {
            lastTab = index;
            resizeWindowToContent(); // tabs can have different heights; this does nothing before the frame is attached
        }

        // ---== Controls tab: one of every row ==---

        private void buildControlsTab(RectTransform pane) {
            // onChanged updates the value during a drag; onCommit runs once when the drag ends.
            opacityRow = SliderRow.Create(pane, "Opacity", 0f, 1f, state.opacity, v => { state.opacity = v; refreshSummary(); }, tooltip: DemoTooltips.OPACITY, onCommit: () => UILog.Info("Opacity committed: " + state.opacity));

            // Use format "0" to show whole numbers without decimals.
            countRow = SliderRow.Create(pane, "Count", 1f, 10f, state.count, v => { state.count = Mathf.RoundToInt(v); refreshSummary(); }, format: "0", wholeNumbers: true, tooltip: DemoTooltips.COUNT);

            flavorRow = DropdownRow.Create(pane, "Flavor", FLAVORS, state.flavor, i => { state.flavor = i; refreshSummary(); }, tooltip: DemoTooltips.FLAVOR);
            // OptionRow suits short lists the player cycles through; use DropdownRow for longer lists.
            sizeRow = OptionRow.Create(pane, "Size", SIZES, state.size, i => { state.size = i; refreshSummary(); }, tooltip: DemoTooltips.SIZE);

            // Show or hide the group when the toggle changes, then resize the window to fit.
            advancedRow = ToggleRow.Create(pane, "Advanced", state.advanced, v => {
                state.advanced = v;
                advancedGroup.SetActive(v);
                resizeWindowToContent();
            }, tooltip: DemoTooltips.ADVANCED);

            advancedGroup = UIBuilder.CreateVerticalGroup(pane, "AdvancedGroup", 4f, 20, -4); // Indent the group and place it close under the toggle.
            offsetRow = NumberRow.Create(advancedGroup.transform, "Offset", state.offset, v => { state.offset = v; refreshSummary(); }, dir => {
                state.offset += dir * 0.25f;
                offsetRow.setValue(state.offset);
                refreshSummary();
            }, tooltip: DemoTooltips.OFFSET);
            buildSeedEditor(advancedGroup.transform);
            advancedGroup.SetActive(state.advanced);

            UIBuilder.CreateSeparator(pane);

            nameRow = TextRow.Create(pane, "Name", state.name, v => { state.name = v; refreshSummary(); }, placeholder: "Type a name", tooltip: DemoTooltips.NAME, characterLimit: 40);
            modeRow = RadioRow.Create(pane, "Mode", MODES, state.mode, i => { state.mode = i; refreshSummary(); }, tooltip: DemoTooltips.MODE);
            tintRow = ColorSwatchRow.Create(pane, "Tint", state.tint, c => { state.tint = c; refreshSummary(); }, tooltip: DemoTooltips.TINT, onCommit: () => UILog.Info("Tint committed: " + state.tint));
            paletteRow = ColorSwatchRow.CreateMulti(pane, "Palette", state.palette, (i, c) => { state.palette[i] = c; refreshSummary(); }, tooltip: DemoTooltips.PALETTE, onCommit: () => UILog.Info("Palette committed"));

            UIBuilder.CreateSeparator(pane);

            // [Apply preset v] [Save] [Reset]
            var presetRow = UIBuilder.CreateHorizontalRow(pane, "PresetRow", UIBuilder.BUTTON_HEIGHT + 2f, spacing: 4f);
            presetDropdown = ActionDropdown.Create(presetRow.transform, "Apply preset", applyPreset, tooltip: DemoTooltips.APPLY_PRESET);
            UIBuilder.CreateThemedButton(presetRow.transform, "Save", savePreset, 56f, tooltip: DemoTooltips.SAVE_PRESET);
            UIBuilder.CreateThemedButton(presetRow.transform, "Reset", () => applySnapshot(new Snapshot()), 56f, tooltip: DemoTooltips.RESET);
            presetDropdown.setNames(new List<string>(presets.Keys));

            summaryLabel = UIBuilder.CreateWrappedLabel(pane, "");

            refreshSummary();
        }

        // Compose a label, integer field and button; the demo owns validation and the action.
        private void buildSeedEditor(Transform parent) {
            UILog.Debug("DemoWindow.buildSeedEditor");

            var row = UIBuilder.CreateHorizontalRow(parent, "SeedEditor", UIBuilder.ROW_HEIGHT);
            UIBuilder.CreateRowLabel(row.transform, "Seed");
            seedInput = UIBuilder.CreateInputField(row.transform, state.seed.ToString(), InputField.ContentType.IntegerNumber);
            var inputLayout = UIBuilder.FixHeight(seedInput.gameObject, UIBuilder.FIELD_HEIGHT);
            inputLayout.minWidth = UIBuilder.FIELD_MIN_WIDTH;
            inputLayout.flexibleWidth = 1f;

            seedInput.onEndEdit.AddListener(text => {
                // Leave invalid text visible and keep the last valid seed.
                if (!int.TryParse(text, out var value)) return;
                state.seed = value;
                seedInput.SetTextWithoutNotify(state.seed.ToString());
                refreshSummary();
            });
            UIBuilder.CreateThemedButton(row.transform, "Randomize", () => {
                state.seed = Random.Range(0, 100000);
                seedInput.SetTextWithoutNotify(state.seed.ToString());
                refreshSummary();
            }, width: 100f, height: UIBuilder.FIELD_HEIGHT);
            Tooltips.Attach(row, DemoTooltips.SEED);
        }

        private void refreshSummary() {
            if (summaryLabel == null) return;
            var text = $"{state.name}: {FLAVORS[state.flavor]}, {SIZES[state.size].ToLower()}, x{state.count} at {state.opacity:0%}, {MODES[state.mode].ToLower()} mode";
            if (state.advanced) text += $", offset {state.offset:0.00}, seed {state.seed}";
            summaryLabel.setText(text);
        }

        private void savePreset() {
            UILog.Debug("DemoWindow.savePreset");
            var suggested = "Preset " + (presets.Count + 1);
            // If the game's rename prompt is unavailable, save with the suggested name.
            if (!Dialogs.PromptForName("Save preset", suggested, savePresetAs)) savePresetAs(suggested);
        }

        private void savePresetAs(string name) {
            if (string.IsNullOrEmpty(name)) return;
            presets[name] = Snapshot.Duplicate(state);
            presetDropdown.setNames(new List<string>(presets.Keys));
            UILog.Info("Saved preset " + name);
        }

        private void applyPreset(string name) {
            UILog.Debug("DemoWindow.applyPreset: " + name);
            Snapshot preset;
            if (!presets.TryGetValue(name, out preset)) return;
            applySnapshot(Snapshot.Duplicate(preset));
        }

        // Update each row without calling its callbacks, then resize for any groups whose visibility changed.
        private void applySnapshot(Snapshot snapshot) {
            state = snapshot;
            opacityRow.setValue(state.opacity);
            countRow.setValue(state.count);
            flavorRow.setSelectedIndex(state.flavor);
            sizeRow.setSelectedIndex(state.size);
            advancedRow.setValue(state.advanced);
            advancedGroup.SetActive(state.advanced);
            offsetRow.setValue(state.offset);
            seedInput.SetTextWithoutNotify(state.seed.ToString());
            tintRow.setColor(state.tint);
            paletteRow.setColors(state.palette);
            nameRow.setValue(state.name);
            modeRow.setSelectedIndex(state.mode);
            refreshSummary();
            resizeWindowToContent();
        }

        // ---== Lists tab: a scrolling list with per-row actions ==---

        private void buildListsTab(RectTransform pane) {
            itemCountLabel = UIBuilder.CreateThemedLabel(pane, "", 22f);
            itemList = ScrollList.Create(pane, LIST_MAX_HEIGHT, 2f, inset: true, padding: 4);
            var buttons = UIBuilder.CreateButtonRow(pane);
            UIBuilder.CreateThemedButton(buttons.transform, "Add item", addItem, tooltip: DemoTooltips.ADD_ITEM);
            UIBuilder.CreateThemedButton(buttons.transform, "Clear all", () => Dialogs.Confirm("Remove all items from the list?", "Clear list", clearItems), tooltip: DemoTooltips.CLEAR_ITEMS);
            rebuildItemList();
            itemList.scrollToTop(); // Start at the top each time the list is built.
        }

        // Each row has a name button to select the item and an X button to remove it.
        private void rebuildItemList() {
            itemList.clear();
            for (int i = 0; i < items.Count; i++) {
                var index = i;
                var row = UIBuilder.CreateHorizontalRow(itemList.Content, "ItemRow", UIBuilder.BUTTON_HEIGHT, spacing: 4f);
                var nameButton = UIBuilder.CreateThemedButton(row.transform, items[i], () => selectItem(index), tooltip: DemoTooltips.ITEM, alignment: TextAnchor.MiddleLeft);
                UIBuilder.SetButtonSelected(nameButton, index == selectedItem);
                UIBuilder.CreateThemedButton(row.transform, "X", () => removeItem(index), UIBuilder.BUTTON_HEIGHT, tooltip: DemoTooltips.REMOVE_ITEM);
            }
            itemCountLabel.setText(items.Count == 1 ? "1 item" : items.Count + " items");
            itemList.fitToContent(); // Grow the viewport up to LIST_MAX_HEIGHT, then allow scrolling.
            resizeWindowToContent();
        }

        private void selectItem(int index) {
            selectedItem = selectedItem == index ? -1 : index;
            rebuildItemList();
        }

        private void addItem() {
            items.Add("Item " + (items.Count + 1));
            rebuildItemList();
        }

        private void removeItem(int index) {
            if (index < 0 || index >= items.Count) return;
            items.RemoveAt(index);
            if (selectedItem == index) selectedItem = -1;
            else if (selectedItem > index) selectedItem--;
            rebuildItemList();
        }

        private void clearItems() {
            items.Clear();
            selectedItem = -1;
            rebuildItemList();
        }

        // ---== More tab: dialogs, an icon button, the hotkey ==---

        private void buildMoreTab(RectTransform pane) {
            UIBuilder.CreateWrappedLabel(pane, "The game's own dialogs, through Dialogs, and an icon button fed by an embedded sprite.");

            var dialogRow = UIBuilder.CreateButtonRow(pane);
            UIBuilder.CreateThemedButton(dialogRow.transform, "Confirm...", () => Dialogs.Confirm("Do the thing?", "Demo confirmation", () => Dialogs.ShowError("Confirmed."), () => UILog.Info("Confirmation canceled")), tooltip: DemoTooltips.CONFIRM);
            UIBuilder.CreateThemedButton(dialogRow.transform, "Prompt...", () => {
                if (!Dialogs.PromptForName("Name something", "Untitled", typed => Dialogs.ShowError("You typed: " + typed))) Dialogs.ShowError("The rename prompt is unavailable.");
            }, tooltip: DemoTooltips.PROMPT);
            UIBuilder.CreateThemedButton(dialogRow.transform, "Error", () => Dialogs.ShowError("This is the game's error tooltip."), tooltip: DemoTooltips.ERROR);

            var iconRow = UIBuilder.CreateHorizontalRow(pane, "IconRow", UIBuilder.BUTTON_HEIGHT + 4f);
            UIBuilder.CreateRowLabel(iconRow.transform, "Icon button");
            
            eyeSprite = EmbeddedSprites.Get(ICON_EYE);
            eyeStrikeSprite = EmbeddedSprites.Get(ICON_EYE_STRIKE);
            if (eyeSprite != null && eyeStrikeSprite != null) {
                eyeButton = UIBuilder.CreateThemedIconButton(iconRow.transform, eyeSprite, toggleEye, tooltip: DemoTooltips.EYE_HIDE);
                eyeIcon = UIBuilder.GetButtonIcon(eyeButton);
            } else {
                UIBuilder.CreateThemedLabel(iconRow.transform, "(icons failed to load, see the log)");
            }
            UIBuilder.CreateFlexibleSpacer(iconRow.transform);

            UIBuilder.CreateSeparator(pane);
            UIBuilder.CreateWrappedLabel(pane, "Press '" + ModUIToolkitDemo.HotkeyName + "' to toggle this window. Rebind it in Options > Controls under \"" + ModUIToolkitDemo.MOD_NAME + "\".");
            UIBuilder.CreateWrappedLabel(pane, "Drag the window somewhere else, close it, and then reopen it: the game remembers where it was.");

            var minimalRow = UIBuilder.CreateButtonRow(pane);
            UIBuilder.CreateThemedButton(minimalRow.transform, "Open minimal example", () => MinimalWindow.Open(), tooltip: DemoTooltips.MINIMAL);

            UIBuilder.CreateSpacer(pane, 6f); // separate the close button from the controls above

            var closeRow = UIBuilder.CreateButtonRow(pane);
            UIBuilder.CreateThemedButton(closeRow.transform, "Close window", close, tooltip: DemoTooltips.CLOSE);
        }

        // Update the icon to show the state and the tooltip to name the action.
        private void toggleEye() {
            eyeOpen = !eyeOpen;
            if (eyeIcon != null) eyeIcon.sprite = eyeOpen ? eyeSprite : eyeStrikeSprite;
            if (eyeButton != null) Tooltips.Attach(eyeButton.gameObject, eyeOpen ? DemoTooltips.EYE_HIDE : DemoTooltips.EYE_SHOW);
        }
    }
}
