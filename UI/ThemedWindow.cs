using System;
using System.Collections.Generic;
using Parkitect.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>Base class for a window with the game's frame and title bar around a panel of controls.</summary>
    /// <remarks>
    /// Derive from this class, add controls in buildContent, and open the window with Spawn&lt;T&gt;.
    /// T is a placeholder for your window class: Spawn&lt;MyWindow&gt;(options) opens and returns a MyWindow.
    /// The constraint where T : ThemedWindow means your class must inherit from ThemedWindow.
    /// FindOpen&lt;MyWindow&gt;() and CloseAll&lt;MyWindow&gt;() use the same notation to choose the window type.
    ///
    /// The base class handles the panel, sizing, placement, and reuse of windows with the same uniqueTag.
    /// Call resizeWindowToContent after showing or hiding rows. See docs/GUIDE.md section 4 for window setup.
    ///
    /// Each window is a MonoBehaviour on the content GameObject. Set fields in Spawn's configure callback to make
    /// them available in buildContent. Use Update as usual, and override onClosing for cleanup rather than OnDestroy.
    ///
    /// The inherited windowFrame gives access to the game's frame, including setSubtitle, highlight, and rectTransform.
    /// </remarks>
    public abstract class ThemedWindow : UIWindow {
        private const float CASCADE_OFFSET_X = 28f;
        private const float CASCADE_OFFSET_Y = -28f;

        public ThemedWindowOptions Options { get; private set; }
        public RectTransform Content { get; private set; }
        public bool IsClosing { get; private set; }

        /// <summary>Opens a window of type T, or brings an existing T with the same non-null uniqueTag to the front.</summary>
        /// <remarks>An existing window is returned without running configure again.</remarks>
        /// <param name="configure">
        /// Runs on a new window before buildContent. Use it to pass in the data the window edits.
        /// </param>
        /// <returns>
        /// The new or existing window. Returns null if the window controller is unavailable, the tag belongs to another
        /// window type, or content setup fails. Failures are logged through UILog.
        /// </returns>
        public static T Spawn<T>(ThemedWindowOptions options, Action<T> configure = null) where T : ThemedWindow {
            UILog.Debug("ThemedWindow.Spawn: " + typeof(T).Name);
            if (options == null) options = new ThemedWindowOptions();

            var controller = UIWindowsController.Instance;
            if (controller == null) {
                UILog.Error("ThemedWindow.Spawn: UIWindowsController is unavailable (no park loaded?), cannot open " + typeof(T).Name);
                return null;
            }

            UIWindowFrame latestFrame;
            var existing = FindWindow<T>(controller, options.uniqueTag, false, out latestFrame);
            if (existing != null) {
                controller.bringWindowToFront(existing.windowFrame);
                existing.windowFrame.highlight();
                return existing;
            }

            // The game saves positions by content name, in one case-insensitive table shared by every mod.
            // Qualify it with the namespace so two mods with a window class of the same name do not share a position.
            var name = typeof(T).FullName + "Content";
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(options.width, options.height);
            bool hasSavedPosition = options.rememberPosition && HasSavedPosition(name);

            var ws = go.AddComponent<UIWindowSettings>();
            ws.title = options.title;
            ws.closable = options.closable;
            ws.pinnable = options.pinnable;
            ws.destroyWhenClosed = true;
            ws.contentDeterminesWindowSize = false; // resizeWindowToContent manages the size
            ws.resizeability = 0;
            ws.spawnLocation = UIWindowSettings.SpawnLocation.Free;
            ws.defaultWindowPosition = options.screenAnchor;

            var window = go.AddComponent<T>();
            window.Options = options;
            window.Content = rt;
            // If setup fails, remove the unfinished content so it is not left in the scene without a window frame.
            try {
                if (configure != null) configure(window);
                UIBuilder.SetupWindowPanel(rt, options.padding, options.spacing, options.horizontalLayout);
                window.buildContent(rt);
            } catch (Exception e) {
                UILog.Error("ThemedWindow.Spawn: building " + typeof(T).Name + " threw, window not opened: " + e);
                UnityEngine.Object.Destroy(go);
                return null;
            }

            var frame = controller.spawnWindow(window, uniqueTag: options.uniqueTag);
            if (frame == null || frame.windowContent != window) {
                // The game's tag lookup spans all window types. A collision can return another window without attaching our content.
                UILog.Error("ThemedWindow.Spawn: spawnWindow did not frame " + typeof(T).Name + " (uniqueTag collision with another window?)");
                UnityEngine.Object.Destroy(go);
                return null;
            }

            // spawnWindow attaches the frame before returning, so the window can now be sized and positioned.
            window.resizeWindowToContent();
            if (latestFrame != null && options.cascade) {
                window.applyCascadeOffset(latestFrame);
            } else if (!hasSavedPosition) {
                window.applyAnchorPosition();
            }
            window.onSpawned();
            return window;
        }

        /// <summary>Add the window's controls to root, which already has a panel background and layout group.</summary>
        /// <remarks>
        /// The frame is not attached yet. Spawn calls resizeWindowToContent after attaching it; calling it here does nothing.
        /// </remarks>
        protected abstract void buildContent(RectTransform root);

        /// <summary>Runs after the frame is attached and the window is sized and positioned.</summary>
        protected virtual void onSpawned() {
        }

        /// <summary>Runs during destruction, whether the player closes the window, close() is called or the scene ends.</summary>
        /// <remarks>Called from OnDestroy, including when setup fails before a frame is attached.</remarks>
        protected virtual void onClosing() {
        }

        public void close() {
            if (IsClosing) return;
            IsClosing = true;
            if (windowFrame != null) {
                windowFrame.close();
            } else {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        public void setTitle(string title) {
            if (windowFrame != null) windowFrame.setTitle(title);
        }

        /// <summary>Fits the window height to its content, and the width too when fitWidthToContent is enabled.</summary>
        /// <remarks>
        /// Call after showing, hiding or resizing rows or groups. Does nothing before the frame is attached.
        /// The frame sizes itself from a LayoutElement on the content object, so changing sizeDelta alone is not enough.
        /// This method updates that element from the root layout group's preferred size, then rebuilds the frame layout.
        /// Reading the group directly avoids reusing the old size stored in the LayoutElement.
        /// </remarks>
        public void resizeWindowToContent() {
            if (windowFrame == null || Content == null) return;
            var contentLayoutElement = GetComponent<LayoutElement>();
            if (contentLayoutElement == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            var group = Content.GetComponent<HorizontalOrVerticalLayoutGroup>();
            float height = group != null ? group.preferredHeight : LayoutUtility.GetPreferredHeight(Content);
            Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            contentLayoutElement.minHeight = height;
            contentLayoutElement.preferredHeight = height;
            if (Options != null && Options.fitWidthToContent) {
                float width = group != null ? group.preferredWidth : LayoutUtility.GetPreferredWidth(Content);
                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                contentLayoutElement.minWidth = width;
                contentLayoutElement.preferredWidth = width;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(windowFrame.rectTransform);
        }

        // Position after resizing so centering uses the final size. The game also keeps the frame clear of screen edges and UI bars.
        private void applyAnchorPosition() {
            if (windowFrame == null) return;
            windowFrame.setToCenteredScreenPosition(Options.screenAnchor);
        }

        private void applyCascadeOffset(UIWindowFrame latestFrame) {
            if (windowFrame == null || latestFrame == null) return;
            windowFrame.rectTransform.anchoredPosition = latestFrame.rectTransform.anchoredPosition + new Vector2(CASCADE_OFFSET_X, CASCADE_OFFSET_Y);
            windowFrame.rectTransform.ForceUpdateRectTransforms();
            LayoutRebuilder.ForceRebuildLayoutImmediate(windowFrame.rectTransform);
            // Repeated offsets can move a window off-screen. Keep it in bounds before and after avoiding other UI elements.
            windowFrame.limitToScreenBounds();
            UIWindowsController.Instance.pushOutOfColliders(windowFrame);
            windowFrame.limitToScreenBounds();
        }

        private static bool HasSavedPosition(string windowName) {
            try {
                var settings = Settings.Instance;
                if (settings == null) return false;
                return settings.getWindowSettings(windowName).position != Vector2.zero;
            } catch (Exception e) {
                UILog.Error("ThemedWindow: could not read the saved window position: " + e);
                return false;
            }
        }

        // Search newest first. Track the latest T for cascading, and match by tag or, if allowed, by type alone.
        private static T FindWindow<T>(UIWindowsController controller, object uniqueTag, bool anyWhenUntagged, out UIWindowFrame latestFrame) where T : ThemedWindow {
            latestFrame = null;
            var windows = controller.getWindows();
            for (int i = windows.Count - 1; i >= 0; i--) {
                var frame = windows[i];
                if (frame == null) continue;
                var window = frame.windowContent as T;
                if (window == null) continue;
                if (latestFrame == null) latestFrame = frame;
                if (uniqueTag == null) {
                    if (anyWhenUntagged) return window;
                } else if (frame.windowSettings != null && uniqueTag.Equals(frame.windowSettings.uniqueTag)) {
                    return window;
                }
            }
            return null;
        }

        /// <summary>Finds an open window of type T without bringing it to the front.</summary>
        /// <param name="uniqueTag">
        /// The tag passed to Spawn. Null matches the most recently opened T, regardless of tag.
        /// </param>
        /// <returns>The matching window, or null if none is open or the window controller is unavailable.</returns>
        public static T FindOpen<T>(object uniqueTag = null) where T : ThemedWindow {
            var controller = UIWindowsController.Instance;
            if (controller == null) return null;
            return FindWindow<T>(controller, uniqueTag, true, out _);
        }

        /// <summary>Closes every open window of type T.</summary>
        /// <returns>How many windows it closed.</returns>
        public static int CloseAll<T>() where T : ThemedWindow {
            var controller = UIWindowsController.Instance;
            if (controller == null) return 0;
            var toClose = new List<ThemedWindow>();
            foreach (var frame in controller.getWindows()) {
                if (frame == null) continue;
                var window = frame.windowContent as T;
                if (window != null) toClose.Add(window);
            }
            foreach (var window in toClose) window.close();
            return toClose.Count;
        }

        protected override void OnDestroy() {
            IsClosing = true;
            try {
                onClosing();
            } catch (Exception e) {
                UILog.Error(GetType().Name + ".onClosing threw: " + e);
            }
            base.OnDestroy();
        }
    }
}
