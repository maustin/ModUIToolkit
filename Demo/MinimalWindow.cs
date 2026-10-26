using ModUIToolkit.UI;
using UnityEngine;

namespace ModUIToolkit.Demo {
    // The README quick start, compiled with the demo so it stays aligned with the API. It shows rows editing values,
    // a group controlled by a toggle, and a button row. Copy this file to start a window in your mod.
    public class MinimalWindow : ThemedWindow {
        private const string WINDOW_TAG = "ModUIToolkit.MinimalWindow"; // a mod-specific tag lets Spawn reuse this window

        // Example values for the controls. A mod would usually store these in a settings object.
        private static float sag = 1f;
        private static int shape = 0;
        private static bool sway = false;
        private static float power = 0.5f;
        private static Color color = Color.white;

        private SliderRow sagRow;
        private GameObject swayGroup;

        public static MinimalWindow Open() {
            var options = new ThemedWindowOptions { title = "Minimal Example", width = 280f, uniqueTag = WINDOW_TAG };
            return Spawn<MinimalWindow>(options);
        }

        protected override void buildContent(RectTransform root) {
            sagRow = SliderRow.Create(
                root, // parent object's transform
                "Sag", // label
                0f, // minimum value
                5f, // maximum value
                sag, // initial value
                v => sag = v, // onChanged
                onCommit: () => UILog.Info("Sag committed: " + sag),
                tooltip: "How far the wire droops."
            );
            DropdownRow.Create(root, "Shape", new[] { "Round", "Square" }, shape, i => shape = i);
            ToggleRow.Create(root, "Sway", sway, v => {
                sway = v;
                swayGroup.SetActive(v);
                resizeWindowToContent(); // Resize the window after showing or hiding content.
            });
            swayGroup = UIBuilder.CreateVerticalGroup(root, "SwayGroup", 4f, 20, -4); // Indent the group and place it close under the toggle.
            SliderRow.Create(swayGroup.transform, "Power", 0f, 1f, power, v => power = v);
            swayGroup.SetActive(sway);
            ColorSwatchRow.Create(root, "Color", color, c => color = c);
            UIBuilder.CreateSeparator(root);
            var buttons = UIBuilder.CreateButtonRow(root);
            UIBuilder.CreateThemedButton(buttons.transform, "Reset", () => {
                sag = 1f; // Update the mod's state before refreshing the display; setValue does not call onChanged.
                sagRow.setValue(sag);
            });
            UIBuilder.CreateThemedButton(buttons.transform, "Close", close);
        }
    }
}
