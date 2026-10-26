# ModUIToolkit

<img width="807" height="387" alt="toolkit-ui-1" src="https://github.com/user-attachments/assets/406127e1-241e-4552-af2a-9a850cb3acb9" />

Add Parkitect-style UI to your own mods!
ModUIToolkit provides popup windows, controls like buttons and checkboxes, tabs, lists, tooltips, and more.

The toolkit is **source code you include in your mod**. Copy or link the [`UI/`](UI/) folder into your project.
This repo also has a demo mod project that lets you try each of these controls in-game and shows you how to set them up in code.

For a complete walkthrough, read the **[GUIDE](docs/GUIDE.md)**.

## What you can build

- Windows with the game's frame and title bar, automatic sizing and remembered positions.
- Settings panels with sliders, dropdowns, checkboxes, text and number fields, color pickers, and buttons.
- Larger interfaces with tabs, collapsible groups, and scrolling lists.
- Actions with text or icon buttons, confirmation dialogs, and text prompts.
- A hotkey players can change in **Options > Controls**.

## Using it in your mod

Start with an existing Parkitect mod project. This library uses C# 10 and .NET Framework 4.7.2.

1. Copy `UI/` into your project and make sure its `.cs` files are included in the build (.csproj). Keep the `ModUIToolkit.UI` namespace, and add `using ModUIToolkit.UI;` in files that use the toolkit. You can also [link the source files](docs/GUIDE.md#linking-the-source-instead-of-copying-it).
2. Add any missing game assembly references from the [list below](#building-and-running-the-demo).
3. Copy [`Demo/MinimalWindow.cs`](Demo/MinimalWindow.cs) as a starting window. Change its namespace to your mod's namespace and its `WINDOW_TAG` to a unique string.
4. Call `MinimalWindow.Open()` from a button or hotkey while a park is loaded. The GUIDE has a [complete hotkey example](docs/GUIDE.md#9-hotkeys).
5. In your mod's `onDisabled`, call `ThemedWindow.CloseAll<MinimalWindow>()`. Do this for each window type your mod opens.

## Basic use

<img width="313" height="269" alt="toolkit-ui-2" src="https://github.com/user-attachments/assets/2e209300-be14-4548-b287-4ef5917e6caa" />

A window inherits from `ThemedWindow`. Add controls in `buildContent`. The toolkit supplies the panel, layout, and frame. For example, these lines from the minimal window add a slider, a dropdown, and a color swatch:

```csharp
protected override void buildContent(RectTransform root) {
    SliderRow.Create(root, "Sag", 0f, 5f, sag, v => sag = v);
    DropdownRow.Create(root, "Shape", new[] { "Round", "Square" }, shape, i => shape = i);
    ColorSwatchRow.Create(root, "Color", color, c => color = c);

    var buttons = UIBuilder.CreateButtonRow(root);
    UIBuilder.CreateThemedButton(buttons.transform, "Close", close);
}
```

Here, `sag`, `shape`, and `color` are values owned by the window or your mod. Each callback receives the player's new value. The [full example](Demo/MinimalWindow.cs) includes those fields, the `Open()` method, and a hidden group shown by a checkbox.

Keep a returned row when you need to update it later. Methods such as `setValue` update the display without calling your callback (update your mod's state as well when applying a reset or preset). After showing or hiding rows, call `resizeWindowToContent()` to resize the window to fit the new content layout.

For sliders and color pickers, use `onChanged` to apply live changes and the optional `onCommit` to save when the edit finishes. The GUIDE explains [callbacks and saving values](docs/GUIDE.md#7-callbacks) for each control.

## Building and running the demo

The demo is a working reference for the toolkit. You can download the release DLL or build it yourself. The installation steps below are for Windows.

### Option 1: Use the release DLL

1. Download `ModUIToolkit.dll` from the **Assets** section of a release on the [Releases page](https://github.com/maustin/ModUIToolkit/releases).
2. Close Parkitect before installing or replacing the DLL.
3. Open File Explorer and enter `%USERPROFILE%\Documents\Parkitect\` in the address bar. Open the `Mods` folder, creating it if needed, then create a `ModUIToolkit` folder inside it.
4. Copy the downloaded DLL into that folder. The full path should be `%USERPROFILE%\Documents\Parkitect\Mods\ModUIToolkit\ModUIToolkit.dll`.

### Option 2: Build it yourself

Use Visual Studio with .NET Framework 4.7.2 development tools, or its MSBuild command-line tool.

1. Clone or download this repository, then create a `Libs/` folder in it. Copy these DLLs from your Parkitect installation's `Parkitect_Data\Managed\` folder into it:

   ```text
   Parkitect.dll
   Unity.TextMeshPro.dll
   UnityEngine.dll
   UnityEngine.CoreModule.dll
   UnityEngine.UI.dll
   UnityEngine.UIModule.dll
   UnityEngine.TextRenderingModule.dll
   UnityEngine.IMGUIModule.dll
   UnityEngine.ImageConversionModule.dll
   UnityEngine.JSONSerializeModule.dll
   UnityEngine.InputLegacyModule.dll
   ```

2. With Parkitect closed, build `ModUIToolkit.sln` in Visual Studio, or run this command from the repository folder:

   ```powershell
   & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ModUIToolkit.sln
   ```

3. The build automatically creates `%USERPROFILE%\Documents\Parkitect\Mods\ModUIToolkit\` if needed and copies `ModUIToolkit.dll` into it.

### Run the demo

After either option, start the game, enable **UI Toolkit Demo**, load a park, and press **I**. You can change the hotkey in **Options > Controls**.

The demo has three tabs:

| Tab | What to try |
|---|---|
| Controls | Rows of settings, a collapsible group, color swatches, a presets system. |
| Lists | Add, select, and remove items in a scrolling list. |
| More | Native dialogs, an icon button, and the minimal example window. |

Read [`Demo/DemoWindow.cs`](Demo/DemoWindow.cs) for control examples and [`Demo/ModUIToolkitDemo.cs`](Demo/ModUIToolkitDemo.cs) for logging, hotkeys, and mod lifecycle setup. The GUIDE shows [how to combine a field and a button](docs/GUIDE.md#combining-a-field-and-a-button).

To uninstall the demo, close the game and remove its `Mods\ModUIToolkit\` folder.

## Learn more

- [GUIDE](docs/GUIDE.md): setup, controls, window lifecycle, layout, and callbacks.
- [Theme reference](docs/THEME.md): the toolkit's color and sizing properties.
- [`UI/`](UI/): API comments and implementation.

## To Do

- Icon button tabs
- Resizable windows

## License

[MIT](LICENSE). If you include the toolkit in your mod, include its copyright and permission notice, for example in your repository's license or third-party notices file.
