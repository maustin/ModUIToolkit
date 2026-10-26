using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A row of themed buttons that switches between content panes.</summary>
    /// <remarks>
    /// Root holds the header strip and every pane, so hiding or destroying Root takes the whole tab set with it. Build each
    /// pane's content under the RectTransform that addTab returns. The first tab is selected automatically.
    /// </remarks>
    public class TabBar {
        public GameObject Root { get; private set; }
        public int SelectedIndex { get; private set; }

        private readonly Transform headers;
        private readonly Action<int> onSelected;
        private readonly float buttonHeight;
        private readonly List<Tab> tabs = new List<Tab>();

        private class Tab {
            public Button button;
            public RectTransform pane;
        }

        /// <param name="onSelected">
        /// Called after a tab is shown. Use it to resize the window when panes can have different heights.
        /// </param>
        /// <param name="spacing">Space between tab headers; zero places them together.</param>
        /// <param name="paneGap">
        /// Space between the headers and the pane below. The default matches a window's default row spacing.
        /// </param>
        public static TabBar Create(Transform parent, Action<int> onSelected = null, float height = UIBuilder.BUTTON_HEIGHT, float spacing = 4f, float paneGap = 6f) {
            return new TabBar(parent, onSelected, height, spacing, paneGap);
        }

        private TabBar(Transform parent, Action<int> onSelected, float height, float spacing, float paneGap) {
            UILog.Debug("TabBar.ctor");
            this.onSelected = onSelected;
            buttonHeight = height;
            SelectedIndex = -1;
            Root = UIBuilder.CreateVerticalGroup(parent, "Tabs", paneGap);
            headers = UIBuilder.CreateHorizontalRow(Root.transform, "TabHeaders", height + 2f, spacing: spacing).transform;
        }

        public int Count => tabs.Count;

        /// <summary>Adds a tab header and its content pane.</summary>
        /// <remarks>
        /// The first tab is selected without calling onSelected.
        /// </remarks>
        /// <param name="width">
        /// Zero or less shares the available width evenly; a positive value sets the header width.
        /// </param>
        /// <returns>The pane to build into.</returns>
        public RectTransform addTab(string label, float width = 0f, string tooltip = null, float paneSpacing = 4f) {
            var index = tabs.Count;
            var tab = new Tab();
            tab.button = UIBuilder.CreateThemedButton(headers, label, () => select(index), width, buttonHeight, tooltip);
            tab.pane = (RectTransform)UIBuilder.CreateVerticalGroup(Root.transform, label + "Tab", paneSpacing, 0, 2).transform;
            tabs.Add(tab);
            if (index == 0) {
                setSelectedIndex(0);
            } else {
                tab.pane.gameObject.SetActive(false);
                UIBuilder.SetButtonSelected(tab.button, false);
            }
            return tab.pane;
        }

        public RectTransform getPane(int index) {
            return index >= 0 && index < tabs.Count ? tabs[index].pane : null;
        }

        public Button getButton(int index) {
            return index >= 0 && index < tabs.Count ? tabs[index].button : null;
        }

        /// <summary>Shows the selected pane, hides the others, updates the header tint, and calls onSelected.</summary>
        public void select(int index) {
            setSelectedIndex(index);
            onSelected?.Invoke(SelectedIndex);
        }

        /// <summary>Selects a tab without calling onSelected.</summary>
        public void setSelectedIndex(int index) {
            if (tabs.Count == 0) return;
            SelectedIndex = Mathf.Clamp(index, 0, tabs.Count - 1);
            for (int i = 0; i < tabs.Count; i++) {
                tabs[i].pane.gameObject.SetActive(i == SelectedIndex);
                UIBuilder.SetButtonSelected(tabs[i].button, i == SelectedIndex);
            }
        }

        public void setInteractable(bool interactable) {
            foreach (var tab in tabs) tab.button.interactable = interactable;
        }
    }
}
