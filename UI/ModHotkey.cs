using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModUIToolkit.UI {
    /// <summary>A rebindable hotkey listed under your mod's heading in Options &gt; Controls.</summary>
    /// <remarks>
    /// The game saves the binding and checks it through HotkeyManager. Register it from your mod's constructor
    /// so it appears in Controls before a park loads. The binding lasts for the game session.
    /// Start listening in onEnabled and stop listening in onDisabled; the handler lasts only while the park is open.
    /// </remarks>
    public class ModHotkey {
        public string Identifier { get; private set; }
        public KeyCode DefaultKey { get; private set; }
        public string GroupIdentifier { get; private set; }

        private KeyMapping mapping;
        private Hotkey hotkey; // registered with the game's HotkeyManager between listen and stopListening

        // Keep one instance per identifier for the session so repeated registration reuses its mapping.
        private static readonly Dictionary<string, ModHotkey> registered = new Dictionary<string, ModHotkey>();

        private ModHotkey() {
        }

        /// <summary>Registers a binding in the game's Controls settings for the session; there is no unregister method.</summary>
        /// <remarks>
        /// Call this once from your mod's constructor. A later call with the same identifier returns the existing
        /// ModHotkey. Failures are logged and return null instead of throwing from the mod constructor. Groups are
        /// registered only when missing because the game's registration path can throw for an existing group; setting
        /// the group on the mapping also avoids the game's automatic group registration during onEnabled.
        /// </remarks>
        /// <param name="identifier">Unique across all mods; prefix it with your mod's name.</param>
        /// <param name="groupIdentifier">
        /// Together with groupName, identifies the heading in Options &gt; Controls. Use your mod's identifier.
        /// </param>
        /// <returns>Null when the binding could not be registered.</returns>
        public static ModHotkey Register(string identifier, KeyCode defaultKey, string name, string description, string groupIdentifier, string groupName) {
            UILog.Debug("ModHotkey.Register: " + identifier);
            try {
                var inputManager = ScriptableSingleton<InputManager>.Instance;
                if (inputManager == null) {
                    UILog.Error("ModHotkey: InputManager unavailable, " + identifier + " cannot be registered or rebound");
                    return null;
                }

                ModHotkey modHotkey;
                if (!registered.TryGetValue(identifier, out modHotkey)) {
                    modHotkey = new ModHotkey();
                    modHotkey.Identifier = identifier;
                    modHotkey.DefaultKey = defaultKey;
                    modHotkey.GroupIdentifier = groupIdentifier;
                    modHotkey.mapping = new KeyMapping(identifier, defaultKey, KeyCode.None);
                    modHotkey.mapping.keyName = name;
                    modHotkey.mapping.keyDescription = description;
                    modHotkey.mapping.keyGroupIdentifier = groupIdentifier;
                    registered[identifier] = modHotkey;
                }

                if (!string.IsNullOrEmpty(groupIdentifier) && !HasKeyGroup(inputManager, groupIdentifier)) {
                    var group = new KeyGroup(groupIdentifier);
                    group.keyGroupName = groupName;
                    inputManager.registerKeyGroup(group);
                }
                inputManager.registerKeyMapping(modHotkey.mapping); // replaces an existing registration under the same identifier
                UILog.Info($"ModHotkey: registered {identifier}, bound to {InputManager.getKeyCodeName(identifier)} (default {defaultKey})");
                return modHotkey;
            } catch (Exception e) {
                UILog.Error("ModHotkey: registering " + identifier + " failed: " + e);
                return null;
            }
        }

        /// <summary>Starts listening for the binding and calls onPressed when its key goes down; call it in onEnabled.</summary>
        /// <remarks>
        /// The game ignores hotkeys while input is locked or a text field is focused. Exceptions from onPressed are
        /// caught and logged. If the player clears the binding, it maps to KeyCode.None and the handler does not run.
        /// </remarks>
        public void listen(Action onPressed) {
            UILog.Debug("ModHotkey.listen: " + Identifier);
            stopListening();
            if (onPressed == null) return;
            hotkey = new Hotkey(mapping);
            hotkey.onKeyDown = () => {
                try {
                    onPressed();
                } catch (Exception e) {
                    UILog.Error("ModHotkey: handler for " + Identifier + " threw: " + e);
                }
            };
            HotkeyManager.Instance.registerHotkey(hotkey);
        }

        /// <summary>Stops listening for the binding. Call it in onDisabled.</summary>
        public void stopListening() {
            if (hotkey == null) return;
            UILog.Debug("ModHotkey.stopListening: " + Identifier);
            HotkeyManager.Instance.unregisterHotkey(hotkey);
            hotkey = null;
        }

        private static bool HasKeyGroup(InputManager inputManager, string groupIdentifier) {
            var groups = inputManager.getKeyGroups();
            for (int i = 0; i < groups.Count; i++) {
                if (groups[i] != null && groups[i].keyGroupIdentifier == groupIdentifier) return true;
            }
            return false;
        }

        public string KeyName => InputManager.getKeyCodeName(Identifier);
    }
}
