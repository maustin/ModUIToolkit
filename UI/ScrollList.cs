using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>Arranges rows in a vertical list that can scroll and optionally show a themed scrollbar.</summary>
    /// <remarks>
    /// When maxHeight is greater than zero, the list grows with its rows up to that height, then scrolls. Call
    /// fitToContent after changing rows in this mode, then resize the window. When maxHeight is zero or less, the list
    /// fills the available height in a parent with a set height. Add rows under Content.
    /// If a scrollbar is enabled, it hides automatically when all rows fit.
    /// </remarks>
    public class ScrollList {
        private const float SCROLLBAR_WIDTH = 10f;

        public GameObject Root { get; private set; }
        public RectTransform Content { get; private set; }
        public ScrollRect ScrollRect { get; private set; }

        private readonly LayoutElement rootLayout;
        private readonly VerticalLayoutGroup contentLayout;
        private readonly float maxHeight;

        /// <param name="maxHeight">
        /// Greater than zero caps the list height; zero or less makes it fill available height.
        /// </param>
        /// <param name="inset">
        /// Shows a dark backing behind the list.
        /// </param>
        public static ScrollList Create(Transform parent, float maxHeight, float spacing = 4f, bool inset = false, bool showScrollbar = true, int padding = 0) {
            return new ScrollList(parent, maxHeight, spacing, inset, showScrollbar, padding);
        }

        private ScrollList(Transform parent, float maxHeight, float spacing, bool inset, bool showScrollbar, int padding) {
            UILog.Debug("ScrollList.ctor");
            this.maxHeight = maxHeight;

            Root = UIBuilder.CreateUIObject(parent, "ScrollList", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            var backing = Root.GetComponent<Image>();
            if (inset) {
                UIBuilder.ApplyPanelSprite(backing, UIBuilder.ListInsetColor);
            } else {
                backing.color = Color.clear; // Invisible, but keeps the whole list area a raycast target so mouse wheel scrolls over gaps too.
            }
            backing.raycastTarget = true;
            rootLayout = Root.GetComponent<LayoutElement>();
            if (maxHeight > 0f) {
                rootLayout.flexibleHeight = 0f; // min and preferred height come from the CappedHeight element added at the end
            } else {
                rootLayout.flexibleHeight = 1f;
                rootLayout.minHeight = 40f;
            }

            var viewportGo = UIBuilder.CreateUIObject(Root.transform, "Viewport", typeof(RectTransform), typeof(RectMask2D));
            var viewportRt = (RectTransform)viewportGo.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportRt.pivot = new Vector2(0f, 1f);

            var contentGo = UIBuilder.CreateUIObject(viewportGo.transform, "Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Content = (RectTransform)contentGo.transform;
            // Top-stretch anchoring so the content grows downward from the top and the ScrollRect can slide it up.
            Content.anchorMin = new Vector2(0f, 1f);
            Content.anchorMax = new Vector2(1f, 1f);
            Content.pivot = new Vector2(0.5f, 1f);
            Content.offsetMin = Vector2.zero;
            Content.offsetMax = Vector2.zero;
            contentLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(padding, padding, padding, padding);
            contentLayout.spacing = spacing;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect = Root.GetComponent<ScrollRect>();
            ScrollRect.content = Content;
            ScrollRect.viewport = viewportRt;
            ScrollRect.horizontal = false;
            ScrollRect.vertical = true;
            ScrollRect.movementType = ScrollRect.MovementType.Clamped;
            ScrollRect.scrollSensitivity = 20f;
            if (showScrollbar) {
                ScrollRect.verticalScrollbar = BuildScrollbar(Root.transform);
                ScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                ScrollRect.verticalScrollbarSpacing = 4f;
            }
            if (maxHeight > 0f) Root.AddComponent<CappedHeight>().list = this;
        }

        /// <summary>Schedules a layout pass after rows change, when maxHeight is greater than zero.</summary>
        /// <remarks>
        /// The height is recalculated during layout, including when the list was built in a hidden pane. This call
        /// schedules a layout pass for the end of the frame; resizeWindowToContent forces a pass immediately and sizes
        /// the window to match.
        /// </remarks>
        public void fitToContent() {
            if (maxHeight <= 0f || Root == null) return;
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)Root.transform);
        }

        /// <summary>Removes all rows from the list.</summary>
        /// <remarks>
        /// Rows are deactivated before Unity destroys them at the end of the frame, so a relayout does not measure
        /// them alongside replacement rows.
        /// </remarks>
        public void clear() {
            if (Content == null) return;
            for (int i = Content.childCount - 1; i >= 0; i--) {
                var child = Content.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        /// <summary>Scrolls to the top after forcing a layout pass to size the content.</summary>
        public void scrollToTop() {
            if (ScrollRect == null || Content == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            ScrollRect.verticalNormalizedPosition = 1f;
        }

        // Recalculate the capped height in each layout pass that reaches Root. A stored height could be wrong if the
        // list was measured while hidden: inactive panes measure zero, and measuring during OnEnable is too early
        // because row components have not been enabled yet. Content is its own layout root, so rebuild it here first.
        // A parent's rebuild stops at the Viewport, which has no layout group.
        private class CappedHeight : UIBehaviour, ILayoutElement {
            public ScrollList list;
            private float height;

            public void CalculateLayoutInputHorizontal() {
            }

            public void CalculateLayoutInputVertical() {
                if (list == null || list.Content == null) return;
                LayoutRebuilder.ForceRebuildLayoutImmediate(list.Content);
                height = Mathf.Min(LayoutUtility.GetPreferredHeight(list.Content), list.maxHeight);
            }

            public float minWidth => -1f;
            public float preferredWidth => -1f;
            public float flexibleWidth => -1f;
            public float minHeight => height;
            public float preferredHeight => height;
            public float flexibleHeight => -1f;
            public int layoutPriority => 1;

            protected override void OnEnable() {
                base.OnEnable();
                setDirty();
            }

            protected override void OnDisable() {
                setDirty();
                base.OnDisable();
            }

            protected override void OnTransformParentChanged() {
                setDirty();
            }

            // Schedule a parent layout pass when the list is shown, hidden, or re-parented, including outside a ThemedWindow.
            private void setDirty() {
                if (!IsActive()) return;
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
            }
        }

        private static Scrollbar BuildScrollbar(Transform parent) {
            var barGo = UIBuilder.CreateUIObject(parent, "Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var barRt = (RectTransform)barGo.transform;
            barRt.anchorMin = new Vector2(1f, 0f);
            barRt.anchorMax = Vector2.one;
            barRt.pivot = new Vector2(1f, 0.5f);
            barRt.sizeDelta = new Vector2(SCROLLBAR_WIDTH, 0f);
            UIBuilder.ApplyPanelSprite(barGo.GetComponent<Image>(), UIBuilder.FieldColor);

            var areaGo = UIBuilder.CreateUIObject(barGo.transform, "SlidingArea", typeof(RectTransform));
            var areaRt = (RectTransform)areaGo.transform;
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(1f, 1f);
            areaRt.offsetMax = new Vector2(-1f, -1f);

            var handleGo = UIBuilder.CreateUIObject(areaGo.transform, "Handle", typeof(RectTransform), typeof(Image));
            var handleRt = (RectTransform)handleGo.transform;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;
            UIBuilder.ApplyPanelSprite(handleGo.GetComponent<Image>(), UIBuilder.ButtonColor);

            var scrollbar = barGo.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.targetGraphic = handleGo.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            return scrollbar;
        }
    }
}
