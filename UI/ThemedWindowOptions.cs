using UnityEngine;

namespace ModUIToolkit.UI {
    /// <summary>Window appearance, layout and placement settings for ThemedWindow.Spawn.</summary>
    /// <remarks>Create a new options object for each Spawn call.</remarks>
    public class ThemedWindowOptions {
        public string title = "Window";

        /// <summary>Content width, kept fixed unless fitWidthToContent is enabled.</summary>
        public float width = 280f;
        /// <summary>Initial content height before the first layout.</summary>
        /// <remarks>Replaced by the content's preferred height after spawning and on every resizeWindowToContent call.</remarks>
        public float height = 200f;

        /// <summary>Identifies a window to reuse when Spawn is called again. Null allows multiple instances.</summary>
        /// <remarks>
        /// Use a mod-specific string for a single shared window, or a mod-owned object for a window that edits that object.
        /// Tags are shared across all game windows. Do not use a Person, Attraction or other object the game uses as a
        /// window tag: the game's lookup could return its own window instead of yours.
        /// </remarks>
        public object uniqueTag;

        public bool closable = true;
        public bool pinnable = false;

        /// <summary>Arranges content side by side instead of vertically, with each column filling the content height.</summary>
        /// <remarks>Useful for a preview beside a column of controls. Usually paired with fitWidthToContent.</remarks>
        public bool horizontalLayout = false;
        public int padding = 10;
        public float spacing = 6f;

        /// <summary>Fits the window width to its content on each resizeWindowToContent call.</summary>
        /// <remarks>
        /// Off by default to preserve the chosen width. A vertical stack's preferred width may be narrower than intended.
        /// </remarks>
        public bool fitWidthToContent = false;

        /// <summary>Initial center position as a fraction of the screen: (0,0) is bottom left and (1,1) is top right.</summary>
        /// <remarks>
        /// Used when there is no saved position and no cascade offset. See rememberPosition and cascade.
        /// </remarks>
        public Vector2 screenAnchor = new Vector2(0.5f, 0.5f);

        /// <summary>Opens below and to the right of the most recently opened window of the same type, when one is already open.</summary>
        /// <remarks>Takes priority over screenAnchor and any saved position.</remarks>
        public bool cascade = true;

        /// <summary>Uses the game's saved position when reopening this window type.</summary>
        /// <remarks>
        /// If false, uses screenAnchor unless cascade applies. Positions are saved under the window class's namespace-qualified
        /// name, so instances of one window type share a position and a same-named window class in another mod does not.
        /// </remarks>
        public bool rememberPosition = true;
    }
}
