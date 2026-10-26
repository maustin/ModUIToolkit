using System;
using Parkitect.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>Clones the game's themed control prefabs and provides their panel sprite and label style.</summary>
    /// <remarks>
    /// AttractionSetting*UI components connect controls to the game's attraction settings. Those wrapper components
    /// can throw during cleanup when used outside that system. Clone the prefab inactive, read its controls, remove
    /// the wrappers, then activate it. Missing assets cause an exception.
    /// </remarks>
    public static class ThemedPrefabs {
        /// <summary>Gets a required prefab or asset from the game's UIAssetManager.</summary>
        /// <remarks>Throws if the asset manager or selected asset is unavailable.</remarks>
        /// <param name="pick">Selects an asset, for example m =&gt; m.attractionSettingDropdownUIGO.</param>
        /// <returns>The selected asset, never null.</returns>
        public static T Require<T>(Func<UIAssetManager, T> pick) where T : UnityEngine.Object {
            var mgr = ScriptableSingleton<UIAssetManager>.Instance;
            if (mgr == null) throw new InvalidOperationException("ThemedPrefabs: UIAssetManager is unavailable");
            var asset = pick(mgr);
            if (asset == null) throw new InvalidOperationException("ThemedPrefabs: the picked UIAssetManager asset is null");
            return asset;
        }

        /// <summary>Clones a game control prefab while keeping it inactive and leaving its wrapper component attached.</summary>
        /// <remarks>
        /// Use the wrapper to set label text or access the inner control. Then call StripWrappers before activating
        /// the clone. Throws if the prefab is unavailable.
        /// </remarks>
        /// <param name="pick">Selects the control prefab from UIAssetManager.</param>
        public static T CloneInactive<T>(Func<UIAssetManager, T> pick) where T : Component {
            UILog.Debug("ThemedPrefabs.CloneInactive");
            var prefab = Require(pick);
            var prefabGO = prefab.gameObject;
            bool prevActive = prefabGO.activeSelf;
            prefabGO.SetActive(false);
            T wrapper;
            try {
                wrapper = UnityEngine.Object.Instantiate(prefab);
            } finally {
                prefabGO.SetActive(prevActive);
            }
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>Immediately removes the supported attraction-setting wrapper components from a clone.</summary>
        /// <remarks>
        /// Call only on a clone that has never been active. Its wrappers have not run Awake, so Unity does not call
        /// their unsafe OnDestroy cleanup when they are removed.
        /// </remarks>
        public static void StripWrappers(GameObject instance) {
            if (instance == null) return;
            var components = instance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in components) {
                if (component == null) continue;
                if (component is AttractionSettingDropdownUI
                    || component is AttractionSettingNumberSliderUI
                    || component is AttractionSettingToggleUI
                    || component is AttractionSettingDurationUI) {
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }
        }

        private static Sprite cachedPanelSprite;

        /// <summary>The game's rounded panel sprite for custom buttons, panels, and input fields.</summary>
        /// <remarks>
        /// Cached from a temporary clone of the themed dropdown prefab. Throws if no sprite is found.
        /// </remarks>
        public static Sprite PanelSprite {
            get {
                if (cachedPanelSprite != null) return cachedPanelSprite;
                var clone = CloneInactive(m => m.attractionSettingDropdownUIGO);
                try {
                    foreach (var img in clone.GetComponentsInChildren<Image>(true)) {
                        if (img != null && img.sprite != null) {
                            cachedPanelSprite = img.sprite;
                            break;
                        }
                    }
                } finally {
                    UnityEngine.Object.DestroyImmediate(clone.gameObject);
                }
                if (cachedPanelSprite == null) throw new InvalidOperationException("ThemedPrefabs: no Image with a sprite on the dropdown prefab");
                return cachedPanelSprite;
            }
        }

        private static bool labelStyleResolved;
        private static TMP_FontAsset cachedLabelFont;
        private static float cachedLabelFontSize;
        private static FontStyles cachedLabelFontStyle;

        /// <summary>The game's TextMesh Pro font for row labels.</summary>
        /// <remarks>LabelFontSize and LabelFontStyle come from the same prefab label. Throws if the label cannot be read.</remarks>
        public static TMP_FontAsset LabelFont {
            get {
                EnsureLabelStyle();
                return cachedLabelFont;
            }
        }

        public static float LabelFontSize {
            get {
                EnsureLabelStyle();
                return cachedLabelFontSize;
            }
        }

        public static FontStyles LabelFontStyle {
            get {
                EnsureLabelStyle();
                return cachedLabelFontStyle;
            }
        }

        private static void EnsureLabelStyle() {
            if (labelStyleResolved) return;

            const string SENTINEL = "__muit_label_probe__";
            var clone = CloneInactive(m => m.attractionSettingDropdownUIGO);
            try {
                clone.setLabelText(SENTINEL);
                foreach (var tmp in clone.GetComponentsInChildren<TMP_Text>(true)) {
                    if (tmp != null && tmp.text == SENTINEL) {
                        cachedLabelFont = tmp.font;
                        cachedLabelFontSize = tmp.fontSize;
                        cachedLabelFontStyle = tmp.fontStyle;
                        break;
                    }
                }
            } finally {
                UnityEngine.Object.DestroyImmediate(clone.gameObject);
            }

            if (cachedLabelFont == null) throw new InvalidOperationException("ThemedPrefabs: the dropdown prefab's label could not be sampled");
            labelStyleResolved = true;
        }
    }
}
