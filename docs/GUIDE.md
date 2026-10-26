# ModUIToolkit guide

ModUIToolkit helps you build mod windows that look and behave like Parkitect's own UI. This guide assumes you have a working C# mod project. For your first window, follow Setup, Windows, and Hotkeys. Use the other sections as you add controls.

1. [Project structure](#1-project-structure)
2. [Setup](#2-setup)
3. [Controls](#3-controls)
4. [Windows](#4-windows)
5. [Layout: groups, tabs, and lists](#5-layout-groups-tabs-and-lists)
6. [Labels, buttons, and styling](#6-labels-buttons-and-styling)
7. [Callbacks](#7-callbacks)
8. [Tooltips](#8-tooltips)
9. [Hotkeys](#9-hotkeys)
10. [Dialogs and popups](#10-dialogs-and-popups)
11. [Icons](#11-icons)
12. [Logging](#12-logging)

For exact method signatures and optional arguments, use IntelliSense or the XML comments in [`UI/`](../UI/). The [demo window](../Demo/DemoWindow.cs) shows every control in use.

## 1. Project structure

The toolkit is shared source: its code becomes part of your mod's DLL.

| Location | Purpose |
|---|---|
| [`UI/`](../UI/) | The toolkit. Copy or link this folder into your mod. All types use the `ModUIToolkit.UI` namespace. |
| [`Demo/MinimalWindow.cs`](../Demo/MinimalWindow.cs) | A small window you can copy as a starting point. |
| [`Demo/DemoWindow.cs`](../Demo/DemoWindow.cs) | Examples of controls, tabs, lists, presets, and custom rows. |
| [`Demo/ModUIToolkitDemo.cs`](../Demo/ModUIToolkitDemo.cs) | A mod entry point with hotkeys, settings, logging, and cleanup. |
| [`docs/THEME.md`](THEME.md) | The shared colors and sizes. |
| `Libs/` | Local copies of Parkitect's reference assemblies, used to compile the demo. These are not included in the repository. |

This repository builds the **UI Toolkit Demo** mod. You do not need to reference its `ModUIToolkit.dll` or ask players to install it. Include `UI/` in your own build instead. The toolkit needs only Parkitect's UI and Unity assemblies.

## 2. Setup

1. Copy the whole `UI/` folder into your mod project. Make sure the `.cs` files are included in the build. This project uses C# 10 and .NET Framework 4.7.2.
2. Add any missing assembly references from the [README's reference list](../README.md#building-and-running-the-demo). Copy those DLLs from your Parkitect installation's `Parkitect_Data\Managed\` folder.
3. Add `using ModUIToolkit.UI;` to files that use the toolkit. Keep the toolkit's namespace, but put your own window classes in your mod's namespace.
4. Copy [`MinimalWindow.cs`](../Demo/MinimalWindow.cs), or use the complete window in [section 4](#4-windows). Give it a mod-specific namespace and window tag.
5. Open the window from a button or hotkey while a park is loaded. [Section 9](#9-hotkeys) includes a complete mod entry point that registers a hotkey and closes the window when the mod is disabled.

Include the toolkit's [license notice](../LICENSE) with your mod's source or third-party notices.

### Linking the source instead of copying it

If your mod and this repository are sibling folders, a classic `.csproj` can include the toolkit like this:

```xml
<ItemGroup>
  <Compile Include="..\ModUIToolkit\UI\**\*.cs">
    <Link>UI\%(RecursiveDir)%(Filename)%(Extension)</Link>
  </Compile>
</ItemGroup>
```

Adjust the relative path for your folders. Linking compiles the same source into your DLL without keeping a second copy. Use either linked files or copied files so each file is compiled once.

To try the controls before building your own window, follow [Building and running the demo](../README.md#building-and-running-the-demo). Load a park, press **I**, and explore its three tabs.

## 3. Controls

A **row** is a complete setting: usually a label and a control, arranged together. For example, `SliderRow` contains its caption, slider, and number field. You do not need to create those parts separately.

Most controls follow the same pattern:

```csharp
int shape = 0;
var shapeRow = DropdownRow.Create(
    root, "Shape", new[] { "Round", "Square" }, shape,
    onChanged: index => shape = index
);
```

`root` is the parent `Transform`, usually the `RectTransform` passed to a window's `buildContent` method. Put these short control examples inside `buildContent`; [section 4](#4-windows) shows the full class and its `using` statements.

The callback receives the player's choice. Here, `index => shape = index` means “when the selection changes, store its index in `shape`.” Indices start at zero, so `0` means Round.

Keep the returned row if you need to update it later. Each row has a `Root` GameObject and a `setInteractable(bool)` method. Use `row.Root.SetActive(false)` to hide a row, or `row.setInteractable(false)` to leave it visible but disable editing. Resize the window after showing or hiding content.

| Control | Use it for | Update it from code |
|---|---|---|
| `SliderRow` | A number with a range, adjusted by dragging or typing. | `setValue(value)`; `calibrate(min, max, value)` also changes the range. |
| `NumberRow` | A typed number, optionally with minus and plus buttons. | `setValue(value)` |
| `TextRow` | A single line of text, such as a name or search query. | `setValue(text)` |
| `ToggleRow` | An on/off setting shown as a checkbox. | `setValue(enabled)` |
| `DropdownRow` | Choosing from a list of options. | `setSelectedIndex(index)`, `setOptions(options)` |
| `OptionRow` | Cycling through a short list with arrow buttons. The arrows wrap around. | `setSelectedIndex(index)`, `setOptions(options)` |
| `RadioRow` | A few choices shown as buttons, with one selected. | `setSelectedIndex(index)` |
| `ColorSwatchRow` | One or more colors edited with the native picker. | `setColor(color)`, `setColor(index, color)`, `setColors(colors)` |
| `ActionDropdown` | Choosing an action, such as applying a named preset. | `setNames(names)` |

These setters do not call your callbacks. Update your mod's values as well as the display when applying a reset or preset; see [section 7](#7-callbacks).

### Numbers and text

For an integer slider, set both `wholeNumbers: true` and `format: "0"`. `wholeNumbers` controls snapping and allowed input; `format` controls how the value is displayed. For decimal values, use formats such as `"0.0"` or `"0.00"`. Keep units in the label rather than using formats that add `%` or currency symbols to the editable number.

Numeric fields use a decimal point, such as `0.5`, regardless of the player's language. A decimal comma such as `0,5` does not parse. Invalid text stays visible in `NumberRow` without calling `onCommit`; `SliderRow` restores its current value without calling either callback. Validate any limits your mod requires in `NumberRow`'s callback, since it has no built-in range.

`NumberRow` adds nudge buttons only when you supply `onNudge`. The callback receives `-1` or `1`; you choose the step and update the field:

```csharp
float offset = 0f;
NumberRow offsetRow = null;
offsetRow = NumberRow.Create(root, "Offset", offset,
    onCommit: value => offset = value,
    onNudge: direction => {
        offset += direction * 0.25f;
        offsetRow.setValue(offset);
    }
);
```

For `TextRow`, use `placeholder` for a hint (it appears semi-transparent in the field until edited) and `characterLimit` for a length limit. An empty label makes the field use the full row width. The same applies to `RadioRow` when you want a button group without a label. `DropdownRow` places its caption above the control; use `rowHeight: DropdownRow.COMPACT_ROW_HEIGHT` for a shorter row.

### Colors and action menus

`ColorSwatchRow.Create` makes one swatch. `CreateMulti` takes a color array and reports which slot changed:

```csharp
var palette = new[] { Color.white, Color.gray };
var paletteRow = ColorSwatchRow.CreateMulti(root, "Palette", palette,
    onChanged: (index, color) => palette[index] = color
);
```

The native picker supports opaque colors only. Each row supports up to `ColorSwatchRow.MAX_SWATCH_COUNT` colors (currently four). `CreateMulti` throws `ArgumentException` before creating an oversized row, and `addSwatch` throws `InvalidOperationException` before changing a full row. Use separate rows for more colors. `setColors` updates existing slots without adding or removing them.

`ActionDropdown` keeps a fixed caption and lets the player choose the same action again. Populate it with `setNames`; an empty list disables it:

```csharp
var actions = ActionDropdown.Create(root, "Apply preset",
    onPicked: name => UILog.Info("Picked preset: " + name)
);
actions.setNames(new[] { "Default", "Compact" });
```

Replace the logging callback with your preset-loading code. The toolkit provides the menu - your mod stores and applies the presets.

## 4. Windows

Derive your window from `ThemedWindow` and add controls in `buildContent`. The base class supplies the background, layout, frame, and title bar.

This complete example opens a window, edits a value, and resets it:

```csharp
using ModUIToolkit.UI;
using UnityEngine;

namespace ExampleMod {
    public class SettingsWindow : ThemedWindow {
        private const string WINDOW_TAG = "ExampleMod.SettingsWindow";
        private static float opacity = 0.75f;
        private SliderRow opacityRow;

        public static SettingsWindow Open() {
            return Spawn<SettingsWindow>(new ThemedWindowOptions {
                title = "Example Mod",
                width = 280f,
                uniqueTag = WINDOW_TAG
            });
        }

        protected override void buildContent(RectTransform root) {
            opacityRow = SliderRow.Create(root, "Opacity", 0f, 1f, opacity,
                onChanged: value => opacity = value,
                onCommit: () => UILog.Info("Opacity committed: " + opacity),
                tooltip: "How visible the object is."
            );

            var buttons = UIBuilder.CreateButtonRow(root);
            UIBuilder.CreateThemedButton(buttons.transform, "Reset", () => {
                opacity = 0.75f;
                opacityRow.setValue(opacity);
            });
            UIBuilder.CreateThemedButton(buttons.transform, "Close", close);
        }
    }
}
```

Call `SettingsWindow.Open()` while a park is loaded. The static value survives closing and reopening the window during the session, but this example does not save it to disk or apply it to a game object. In your mod, keep values in your settings or data model and apply changes from the callbacks.

`Spawn<SettingsWindow>` means “create a window of the `SettingsWindow` class.” The same notation selects a window type for `FindOpen<SettingsWindow>()` and `CloseAll<SettingsWindow>()`.

### Opening, reusing, and closing

A non-null `uniqueTag` lets `Spawn` reuse a window of the same type and bring it to the front. Use a mod-specific string for something like a single settings window (e.g. "MyAwesomeMod.Settings"). For separate windows editing different objects, use distinct mod-owned tags. Leave the tag null to allow multiple instances.

Tags are shared with the game's windows. Do not use a `Person`, `Attraction`, or another object the game uses as a window tag. `Spawn` logs failures and returns null if the controller is unavailable, content setup fails, or a tag collision prevents it from opening your window. Check the return value before using the window.

To pass data into a new window, use `Spawn`'s `configure` callback. It runs before `buildContent`, so you can assign fields that the controls need. Reusing an existing tagged window does not run `configure` again.

Use `FindOpen<SettingsWindow>()` to look for an existing window without bringing it forward. With no tag, it returns the most recently opened window of that type, or null. Use `close()` inside a window, and call `ThemedWindow.CloseAll<SettingsWindow>()` in your mod's `onDisabled`. Repeat that cleanup for each window type your mod opens.

Override `onSpawned` for work that needs the frame. Override `onClosing` to unsubscribe from events or release references. `onClosing` also runs when the scene ends or setup fails, so cleanup should allow for partially built content. Let the base class handle `OnDestroy`.

### Size and position

The default layout stacks controls vertically. `width` sets the content width; height is calculated from the content. The `height` option is only an initial size, not a fixed height. Call `resizeWindowToContent()` after adding, removing, showing, hiding, or resizing content. The initial call happens automatically after the frame is attached.

For columns, set `horizontalLayout = true` in the options object, then build a vertical group for each column. `fitWidthToContent = true` also lets the content determine the width. See [ThemedWindowOptions](../UI/ThemedWindowOptions.cs) for all options.

`screenAnchor` sets the window's initial center as a fraction of the screen: `(0, 0)` is bottom left, `(1, 1)` is top right, and `(0.5f, 0.5f)` is centered. By default, another window of the same type opens slightly below and to the right of the most recent one. This `cascade` setting takes priority over the anchor and saved position.

The game remembers position using the content object's name. For `ExampleMod.SettingsWindow`, that name is `ExampleMod.SettingsWindowContent`. Instances of one window type share a position. A same-named class in another namespace gets a different position, but names are compared without regard to case. Keep your class name and namespace stable after release to preserve saved positions. Set both `rememberPosition = false` and `cascade = false` to always use `screenAnchor`.

## 5. Layout: groups, tabs, and lists

Build controls under the container where you want them to appear. Layout groups place and size their children, so you usually do not need to position controls manually.

### Showing and hiding a group

A vertical group lets you show several related rows together. This example belongs inside `buildContent`:

```csharp
bool advanced = false;
GameObject advancedGroup = null;
ToggleRow.Create(root, "Advanced", advanced, onChanged: enabled => {
    advanced = enabled;
    advancedGroup.SetActive(enabled);
    resizeWindowToContent();
});

advancedGroup = UIBuilder.CreateVerticalGroup(root, "AdvancedGroup", leftPadding: 20);
UIBuilder.CreateThemedLabel(advancedGroup.transform, "Additional settings go here.");
advancedGroup.SetActive(advanced);
```

Hidden groups take no layout space. `spacing` controls the gap between rows, and `leftPadding` indents them. A negative `topPadding` can reduce the gap below the preceding row. When restoring a saved toggle value, update the toggle and the group's visibility yourself; setters do not run the callback.

### Tabs

Create a `TabBar`, then build each tab under the pane returned by `addTab`:

```csharp
var tabs = TabBar.Create(root, onSelected: index => resizeWindowToContent());
var settingsPane = tabs.addTab("Settings");
var aboutPane = tabs.addTab("About");

UIBuilder.CreateThemedLabel(settingsPane, "Settings go here.");
UIBuilder.CreateWrappedLabel(aboutPane, "Information about the mod goes here.");
```

The first tab is selected automatically without calling `onSelected`. Selecting a tab shows its pane and hides the others. The resize callback keeps the window fitted to panes with different heights.

`tabs.select(index)` calls `onSelected`; `tabs.setSelectedIndex(index)` changes the tab silently. If you use the silent setter after opening the window, resize it yourself. `tabs.Root` contains both the header strip and every pane, so hiding or destroying it affects the whole tab set.

### Scrolling lists

Add list rows under `Content`, not `Root`:

```csharp
var list = ScrollList.Create(root, maxHeight: 160f, inset: true, padding: 4);
foreach (var name in new[] { "Ferris wheel", "Log flume", "Teacups" }) {
    UIBuilder.CreateThemedButton(list.Content, name,
        () => UILog.Info("Selected: " + name),
        alignment: TextAnchor.MiddleLeft
    );
}
list.fitToContent();
```

A positive `maxHeight` lets the list grow up to that height, then scroll. The scrollbar hides when all rows fit. After changing rows, call `fitToContent()`, then `resizeWindowToContent()` to update an open window. This also works when the list was built in a hidden tab.

`clear()` removes the displayed rows; it does not clear your mod's data. Rebuild the rows from that data, fit the list, and resize the window. Use `scrollToTop()` when the new content should start at the top. For a single removed row, deactivate it before calling `UnityEngine.Object.Destroy` so the next layout does not count it while destruction is pending.

With `maxHeight` at zero or below, the list fills available height. Use that mode only inside a container with a defined height; a positive cap is simpler for a window that sizes itself to its content.

### Combining a field and a button

Use `UIBuilder` when a setting needs a custom row. This example creates an integer seed field beside a Randomize button. Add `using UnityEngine.UI;` for `InputField`:

```csharp
int seed = 1234;
var row = UIBuilder.CreateHorizontalRow(root, "SeedEditor", UIBuilder.ROW_HEIGHT);
UIBuilder.CreateRowLabel(row.transform, "Seed");
var input = UIBuilder.CreateInputField(row.transform, seed.ToString(),
    InputField.ContentType.IntegerNumber);
var inputLayout = UIBuilder.FixHeight(input.gameObject, UIBuilder.FIELD_HEIGHT);
inputLayout.minWidth = UIBuilder.FIELD_MIN_WIDTH;
inputLayout.flexibleWidth = 1f;

input.onEndEdit.AddListener(text => {
    if (!int.TryParse(text, out var value)) return;
    seed = value;
    input.SetTextWithoutNotify(seed.ToString());
});
UIBuilder.CreateThemedButton(row.transform, "Randomize", () => {
    seed = UnityEngine.Random.Range(0, 100000);
    input.SetTextWithoutNotify(seed.ToString());
}, width: 100f, height: UIBuilder.FIELD_HEIGHT);
```

The field takes the remaining width while the button stays fixed. Keep `CreateHorizontalRow`'s default `childControlWidth = true` so the layout respects those sizes. Your mod owns the seed, its validation, and what Randomize does. The [demo's `buildSeedEditor`](../Demo/DemoWindow.cs) uses the same pattern with stored state.

## 6. Labels, buttons, and styling

Use `UIBuilder.CreateThemedLabel` for a single line, `CreateWrappedLabel` for longer text, and `CreateRowLabel` for the fixed-width label in a custom row. They return a `UILabel`, which you can keep and update with `setText`:

```csharp
var status = UIBuilder.CreateWrappedLabel(root, "Ready.");
status.setText("Select an item to edit its settings.");
```

The labels use Parkitect's TextMesh Pro font. (**TMP** in source comments means TextMesh Pro, Unity's text-rendering system.) `UILabel` lets you update text without working with TMP components directly. Prefer plain ASCII captions: the game's font lacks many glyphs, like `✕`, while `✓`, `◀`, and `▶` are supported. Use a text `X` for a close button. If you use a symbol and it appears as a square in-game, it is not supported.

For the `alignment` parameter on `CreateThemedLabel` and `CreateThemedButton`, use `TextAnchor.MiddleLeft`, `TextAnchor.MiddleCenter`, or `TextAnchor.MiddleRight`. Text is vertically centered.

Create buttons with `CreateThemedButton`, usually inside a `CreateButtonRow`. A positive `width` fixes the button width. The default, zero, shares spare width with other flexible buttons. The returned value is a Unity `Button`, so use `button.interactable = false` to disable it.

Use `UIBuilder.GetButtonCaption(button).setText("New caption")` to update a text button. Use `UIBuilder.SetButtonSelected(button, selected)` for a persistent selection highlight; tabs and radio rows already do this. It preserves hover and press feedback.

The shared theme uses dark labels on light panels and light text inside dark fields. Use its existing colors and size constants when composing controls. The [theme reference](THEME.md) lists them. `CreateSeparator` adds a divider, `CreateSpacer` adds a fixed gap, and `CreateFlexibleSpacer` uses remaining space to push later controls toward an edge.

If a layout group controls an object's size, use `FixWidth`, `FixHeight`, or `EnsureLayoutElement` instead of changing only its `RectTransform.sizeDelta`. If wrapped text changes height, resize the window afterward.

## 7. Callbacks

A callback is code you supply for a later event, such as a click or edit. `onChanged` usually handles live updates. `onCommit` marks the end of an edit where the control supports it. The timing depends on the control:

| Control | Callback timing |
|---|---|
| `SliderRow` | `onChanged(value)` during slider changes. `onCommit()` on pointer release. A valid typed edit calls both, in that order, on Enter or focus loss. |
| `NumberRow` | `onCommit(value)` on Enter or focus loss if the text parses. Nudge buttons call only `onNudge(direction)`. |
| `TextRow` | Optional `onChanged(text)` while typing; `onCommit(text)` on Enter or focus loss. |
| `ToggleRow`, `DropdownRow`, `OptionRow`, `RadioRow` | `onChanged(value)` for a selection change. There is no separate commit callback. |
| `ColorSwatchRow` | `onChanged` for live color changes. Commit timing is described below. |
| `ActionDropdown` | `onPicked(name)` for each choice, including repeated choices of the same name. |
| `TabBar` | `onSelected(index)` after showing a pane through a click or `select`. |

For sliders and color pickers, apply live changes in `onChanged` and save finished edits in `onCommit`. Their commit callbacks have no value argument, so save the state you already updated. For a checkbox or dropdown, the discrete change can be applied and saved in the same callback. If a number row has nudge buttons, include any saving or applying work in `onNudge` too.

Slider completion is tied to pointer release and typed input. It is not a general “value changed” notification for every input path. Keyboard slider navigation has no separate commit handler. A pointer release can also commit without changing the value. Keep saving safe to repeat and use your mod's normal save lifecycle if it must cover other input paths.

### Updating controls from your mod

Your mod owns its settings. Setters update the display without calling callbacks. The Reset button in [section 4](#4-windows) therefore updates `opacity` before calling `opacityRow.setValue(opacity)`. If your callback also applies a game change, updates a summary, or saves settings, do that work explicitly when resetting or applying a preset.

`SliderRow` clamps values to its range and snaps them in whole-number mode. For example, `setValue(20f)` on a slider ranging from 0 to 10 displays 10 and leaves `Value` at 10, without firing either callback. Read `Value` back if you need to normalize saved data to a new range.

When refreshing from external data, avoid replacing text while a row's `Input.isFocused` is true. For color refreshes, check `ColorSwatchRow.PickerUncommitted` so you do not overwrite an unfinished picker edit.

### Color edit completion

An edited color picker commits when it closes. Opening and closing it without changes does not commit.

Dropping colors on rows without an open picker calls `onChanged` for each changed slot, then commits once per affected row at the end of the frame. This lets a color swap finish before saving. Hiding the row with `Root.SetActive(false)` also flushes a queued drop commit. A drop on the row with the open picker joins that edit and commits when the picker closes.

`PickerUncommitted` is true only after a row with an open picker changes color, until that picker closes. It covers the rows in your compiled copy of the toolkit. Drops on other rows do not replace or clear the pending picker edit.

## 8. Tooltips

Pass `tooltip: "Helpful text"` when creating a row or button. I recommend keeping tooltip strings with your mod's other text. [`DemoTooltips.cs`](../Demo/DemoTooltips.cs) is an example.

To attach or update a tooltip on the dropdown from [section 3](#3-controls):

```csharp
Tooltips.Attach(shapeRow.Root, "The shape used for new objects.");
```

For a button, pass `button.gameObject`. For a custom row returned by `CreateHorizontalRow`, pass the row itself, since it is already a GameObject.

`Attach` reuses an existing tooltip and makes the target respond to hover, including gaps in a row. Calling it with empty text does nothing; use `Tooltips.Detach(target)` to remove a tooltip. Detaching leaves any transparent hover Image in place. The supplied text is used directly, so translate it in your mod before attaching it if needed.

## 9. Hotkeys

`ModHotkey` registers a binding that players can change in **Options > Controls**. Register it in your mod's constructor so the setting appears before a park loads. Start listening in `onEnabled`, and stop in `onDisabled`.

This entry point works with `ExampleMod.SettingsWindow` from [section 4](#4-windows). For an existing mod, merge the constructor and lifecycle work into your current entry point:

```csharp
using ModUIToolkit.UI;
using UnityEngine;

namespace ExampleMod {
    public class ExampleMod : AbstractMod {
        private readonly ModHotkey hotkey;

        public override string getIdentifier() => "examplemod";
        public override string getName() => "Example Mod";
        public override string getDescription() => "A mod with a settings window.";
        public override string getVersionNumber() => "1.0";

        public ExampleMod() {
            UILog.Prefix = "Example Mod";
            hotkey = ModHotkey.Register(
                "ExampleMod_ToggleWindow", KeyCode.I,
                "Toggle settings", "Opens or closes the settings window.",
                "examplemod", "Example Mod"
            );
        }

        public override void onEnabled() {
            if (hotkey != null) hotkey.listen(toggleWindow);
        }

        public override void onDisabled() {
            if (hotkey != null) hotkey.stopListening();
            ThemedWindow.CloseAll<SettingsWindow>();
        }

        private void toggleWindow() {
            var open = ThemedWindow.FindOpen<SettingsWindow>();
            if (open != null) open.close();
            else SettingsWindow.Open();
        }
    }
}
```

Replace the example identifiers and display names with your mod's own. Hotkey identifiers must be unique across mods. `Register` returns null and logs an error if registration fails, which is why the example checks before listening.

The binding remains registered for the game session; there is no unregister method. `stopListening` removes your handler. Calling `listen` again replaces the previous handler. The game ignores hotkeys while input is locked or a text field is focused. A cleared binding also does nothing. Use `hotkey.KeyName` for instructions shown to the player so they reflect the current binding instead of always saying “I.”

## 10. Dialogs and popups

`Dialogs` opens the game's own confirmation and name windows. Call these from a button or other player action while a park is loaded, and check whether opening succeeded.

To ask for confirmation:

```csharp
var confirmation = Dialogs.Confirm(
    text: "Clear the current selection?",
    title: "Clear selection",
    onConfirmed: () => UILog.Info("Clear confirmed")
);
if (confirmation == null) UILog.Error("Could not open the confirmation dialog.");
```

To ask for a name:

```csharp
bool opened = Dialogs.PromptForName("Save preset", "New preset", name => {
    if (string.IsNullOrWhiteSpace(name)) return;
    UILog.Info("Save preset as: " + name);
});
if (!opened) UILog.Error("Could not open the name prompt.");
```

Replace the logging callbacks with your action or save code. `Confirm` also accepts an optional `onCanceled` callback. `PromptForName` returns whether the prompt opened; the entered name arrives later through its callback. Validate that name before using it.

Use `Dialogs.ShowError("Enter a valid name.")` for the game's red error notice near the cursor.

Dialogs have their own lifetime and can outlive the window that opened them. If a callback needs that window's controls, check `this == null || IsClosing` inside the callback before accessing them. Decide whether an action should still update your mod's data after its window has closed.

The game runs a dialog callback before closing the dialog. The toolkit catches callback exceptions, logs them through `UILog`, and lets the dialog close. It does not undo work the callback already performed.

## 11. Icons

Use a white icon on a transparent background for a themed icon button. The button tints it to the theme's dark text color.

Embed the image in your mod by adding an item to its `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Icons\eye.png" />
</ItemGroup>
```

With a project `RootNamespace` of `ExampleMod`, the resource name is `ExampleMod.Icons.eye.png`. Match the name and case exactly:

```csharp
var icon = EmbeddedSprites.Get("ExampleMod.Icons.eye.png");
if (icon != null) {
    UIBuilder.CreateThemedIconButton(root, icon,
        () => UILog.Info("Preview toggled"),
        tooltip: "Toggle preview"
    );
}
```

`Get` loads from the calling assembly and caches the sprite. It returns null on failure. If the resource name is wrong, the log lists the assembly's available names. Use `GetTexture` when you need a `Texture2D` instead of a sprite.

For a button with two states, update `UIBuilder.GetButtonIcon(button).sprite` and attach a tooltip that names the new action. The demo's eye button shows this pattern. Parkitect's UI sprites are not available through `Resources.Load`; toolkit controls obtain their native assets through `ThemedPrefabs`, which you normally do not need to call yourself.

## 12. Logging

`UILog` works without setup and writes to Unity's log. Set its prefix to your mod's name, usually in the constructor:

```csharp
UILog.Prefix = "Example Mod";
UILog.MinLevel = UILog.Level.Info;
UILog.Info("Settings loaded.");
```

The default level is `Info`. Set `MinLevel` to `UILog.Level.Debug` while troubleshooting to include toolkit construction traces. `UILog.Error` reports failures, including windows that could not open, missing resources, and exceptions caught by the toolkit.

If your mod already has a logger, assign `UILog.MessageHandler` to route messages there. It receives the level and message after filtering, without `Prefix`, and replaces the normal Unity output. Set it to null to restore the default logger.

When a window fails to open, check the log first. Prefab-based controls require the game's prefabs; missing assets cause setup to fail. `ThemedWindow.Spawn` logs the error, removes the unfinished content, and returns null. Keep these failures visible while developing so you can fix the setup or callback that caused them.
