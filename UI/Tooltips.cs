using Parkitect.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>Adds and removes Parkitect-style hover tooltips.</summary>
    /// <remarks>
    /// Store tooltip text with your mod's other strings so it can be maintained in one place.
    /// </remarks>
    public static class Tooltips {
        /// <summary>Shows the text when the target is hovered. Calling this again updates the existing tooltip.</summary>
        /// <remarks>
        /// To make a whole row respond to hover, its root needs a raycastable Graphic. A transparent full-size Image
        /// lets the row receive pointer events in the gaps while child controls remain usable. Existing UITooltip
        /// components are reused because Parkitect displays all tooltips on an object together.
        /// </remarks>
        public static void Attach(GameObject target, string text) {
            if (target == null || string.IsNullOrEmpty(text)) return;

            var graphic = target.GetComponent<Graphic>();
            if (graphic == null) {
                var img = target.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0f); // invisible, but still a raycast target
                img.raycastTarget = true;
            } else {
                graphic.raycastTarget = true;
            }

            var tip = target.GetComponent<UITooltip>() ?? target.AddComponent<UITooltip>();
            tip.globalText = GlobalStrings.GlobalText.None;
            tip.text = text;
            tip.context = null; // use the supplied text without a context-specific translation lookup
        }

        /// <summary>Removes the tooltip component from the target, leaving any Image added by Attach as a raycast target.</summary>
        public static void Detach(GameObject target) {
            if (target == null) return;
            var tip = target.GetComponent<UITooltip>();
            if (tip != null) UnityEngine.Object.Destroy(tip);
        }
    }
}
