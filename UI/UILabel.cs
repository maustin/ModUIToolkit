using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A text label returned by UIBuilder.</summary>
    /// <remarks>
    /// setText skips writes when the text is unchanged, avoiding unnecessary canvas layout work during refreshes.
    /// </remarks>
    public class UILabel {
        public GameObject Root { get; private set; }
        public Graphic Graphic { get; private set; }

        private readonly TMP_Text tmp;

        public UILabel(TMP_Text text) {
            tmp = text;
            Graphic = text;
            Root = text != null ? text.gameObject : null;
        }

        public string Text => tmp != null ? tmp.text : null;

        public void setText(string text) {
            if (tmp != null && tmp.text != text) tmp.text = text;
        }

        public void setColor(Color color) {
            if (Graphic != null) Graphic.color = color;
        }
    }
}
