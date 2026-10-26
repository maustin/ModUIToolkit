using ModUIToolkit.UI;
using System.IO;
using UnityEngine;

namespace ModUIToolkit.Demo {
    // An example of using the toolkit in a mod. The constructor sets up logging and the hotkey binding for the game session.
    // onEnabled starts the hotkey handler for a park; onDisabled stops it and closes the windows.
    public class ModUIToolkitDemo : AbstractMod, IModSettings {
        public const string VERSION_NUMBER = "1.0";
        public const string MOD_NAME = "UI Toolkit Demo";
        public const string MOD_IDENTIFIER = "moduitoolkit.demo";
        private const string HOTKEY_IDENTIFIER = "ModUIToolkitDemo_ToggleWindow";
        private const KeyCode DEFAULT_HOTKEY = KeyCode.I; // Players can change this binding in Options -> Controls

        public override string getIdentifier() => MOD_IDENTIFIER;
        public override string getName() => MOD_NAME;
        public override string getDescription() => "'Everything' demo of the Mod UI toolkit. Press the hotkey in a park to open the UI.";
        public override string getVersionNumber() => VERSION_NUMBER;

        public override bool isMultiplayerModeCompatible() => true;
        public override bool isRequiredByAllPlayersInMultiplayerMode() => false; // UI only, touches no game state

        public static ModUIToolkitDemo Instance;
        public static DemoSettings Settings = new DemoSettings();

        private ModHotkey hotkey;

        // Runs from the splash screen before a park exists. Avoid throwing here or ModManager will skip this mod.
        public ModUIToolkitDemo() {
            UILog.Prefix = MOD_NAME;

            // Register the binding here; its handler is registered in onEnabled for each park.
            hotkey = ModHotkey.Register(HOTKEY_IDENTIFIER, DEFAULT_HOTKEY, "Toggle demo window", "Opens and closes the UI Toolkit demo window.", MOD_IDENTIFIER, MOD_NAME);
        }

        public override void onEnabled() {
            UILog.Info("Starting v" + VERSION_NUMBER);
            Instance = this;
            loadSettings();

            // HotkeyManager checks input guards before calling the handler. Register it while the mod is enabled in a park.
            if (hotkey != null) hotkey.listen(() => DemoWindow.Toggle());
            UILog.Info("Press " + HotkeyName + " in a park to open the demo window");
        }

        public override void onDisabled() {
            UILog.Info("Disabled");
            ThemedWindow.CloseAll<DemoWindow>();
            ThemedWindow.CloseAll<MinimalWindow>();
            if (hotkey != null) hotkey.stopListening();
            Instance = null;
        }

        // The key currently bound to the toggle, for instructions shown in the window.
        public static string HotkeyName {
            get {
                if (Instance == null || Instance.hotkey == null) return "the demo hotkey";
                return Instance.hotkey.KeyName;
            }
        }

        // --== Settings (the game's IMGUI mod-settings panel) ==--

        // Here you can define the UI for changing your mod's settings.
        public void onDrawSettingsUI() {
            GUILayout.BeginVertical();
            Settings.debugLogging = GUILayout.Toggle(Settings.debugLogging, " Enable debug logging");
            GUILayout.Space(8f);
            if (GameController.Instance != null && GameController.Instance.park != null) {
                if (GUILayout.Button("Open demo window", GUILayout.ExpandWidth(false))) DemoWindow.Open();
            } else {
                GUILayout.Label("Load a park, then press " + HotkeyName + " to open the demo window.");
            }
            GUILayout.EndVertical();
        }

        public void onSettingsOpened() {
            loadSettings();
        }

        public void onSettingsClosed() {
            saveSettings();
            GUI.FocusControl(null);
        }

        // Store player settings in the game's data folder, outside the mod folder managed by Steam Workshop.
        public static string GetDataFolder() => FilePaths.getFolderPath("ModUIToolkitDemo");

        private static string getSettingsPath() {
            return System.IO.Path.Combine(GetDataFolder(), "settings.json"); // Use System.IO.Path because the game defines a global Path type.
        }

        private void loadSettings() {
            try {
                if (File.Exists(getSettingsPath())) {
                    Settings = JsonUtility.FromJson<DemoSettings>(File.ReadAllText(getSettingsPath())) ?? new DemoSettings();
                } else {
                    Settings = new DemoSettings();
                }
            } catch (System.Exception e) {
                UILog.Error("Could not load settings, using defaults: " + e);
                Settings = new DemoSettings();
            }
            applyLogLevel();
        }

        private void saveSettings() {
            try {
                Directory.CreateDirectory(GetDataFolder());
                File.WriteAllText(getSettingsPath(), JsonUtility.ToJson(Settings, true));
            } catch (System.Exception e) {
                UILog.Error("Could not save settings: " + e);
            }
            applyLogLevel();
        }

        private static void applyLogLevel() {
            UILog.MinLevel = Settings.debugLogging ? UILog.Level.Debug : UILog.Level.Info;
        }
    }
}
