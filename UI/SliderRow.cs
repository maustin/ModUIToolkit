using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModUIToolkit.UI {
    /// <summary>A slider with an editable value field.</summary>
    /// <remarks>
    /// The row clones Parkitect's AttractionSettingNumberSliderUI and adds an editable field beside it. The
    /// prefab supplies the row label, so there is no labelWidth. The slider and input field are Unity controls.
    ///
    /// onChanged is called for each live slider change. onCommit is called once on pointer release or when a typed
    /// value is committed with Enter or focus loss. A valid typed value calls onChanged before onCommit.
    /// Invalid text is replaced by the slider's current value without calling either callback.
    /// Initial, assigned and typed values use the slider's range clamping and whole-number snapping; the field and
    /// onChanged use that resulting value.
    /// </remarks>
    public class SliderRow {
        // Keep the row height; RestyleNativeSlider moves the bar closer to its label using UIBuilder.SLIDER_BAR_RAISE.
        private const float THEMED_ROW_HEIGHT = 30f;
        private const float THEMED_INPUT_HEIGHT = 20f; // sits level with the raised bar
        private const float INPUT_WIDTH = 52f;
        private const float INPUT_MIN_WIDTH = 42f;

        public GameObject Root { get; private set; }
        public Slider Slider { get; private set; }
        public InputField Input { get; private set; }

        private readonly string format;
        private readonly Action<float> onChanged;
        private readonly Action onCommit;
        private bool suppress;

        /// <param name="format">
        /// A .NET numeric format for the field: "0" for integers, "0.0" for tenths.
        /// </param>
        /// <param name="wholeNumbers">
        /// Snaps the slider to integers and makes the field integer-only, so pair it with format "0".
        /// </param>
        public static SliderRow Create(Transform parent, string label, float min, float max, float initial, Action<float> onChanged, Action onCommit = null, string tooltip = null, string format = "0.00", bool wholeNumbers = false) {
            return new SliderRow(parent, label, min, max, initial, onChanged, onCommit, tooltip, format, wholeNumbers);
        }

        private SliderRow(Transform parent, string label, float min, float max, float initial, Action<float> onChanged, Action onCommit, string tooltip, string format, bool wholeNumbers) {
            UILog.Debug("SliderRow.ctor: " + label);
            this.format = format;
            this.onChanged = onChanged;
            this.onCommit = onCommit;

            buildThemed(parent, label, min, max, initial, wholeNumbers);

            Slider.onValueChanged.AddListener(onSliderChanged);
            Input.onEndEdit.AddListener(onInputEndEdit);
            attachCommitRelay();
            Tooltips.Attach(Root, tooltip);
        }

        private void buildThemed(Transform parent, string label, float min, float max, float initial, bool wholeNumbers) {
            var wrapper = ThemedPrefabs.CloneInactive(m => m.attractionSettingNumberSliderUIGO);

            wrapper.setLabelText(label);
            var slider = wrapper.GetComponentInChildren<Slider>(true);
            var go = wrapper.gameObject;

            // Hide the prefab's read-only value label so only our editable field shows a number.
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true)) {
                if (tmp != null && tmp.text != label) tmp.gameObject.SetActive(false);
            }

            ThemedPrefabs.StripWrappers(go);

            // The row must control child widths so the slider can expand while the input keeps its preferred width.
            // Without this, the prefab root collapses and its label wraps one character per line.
            Root = UIBuilder.CreateHorizontalRow(parent, "SliderRow", THEMED_ROW_HEIGHT);
            go.name = "ThemedSlider";
            go.transform.SetParent(Root.transform, false);
            var themedLe = UIBuilder.FixHeight(go, THEMED_ROW_HEIGHT);
            themedLe.flexibleWidth = 1f;
            themedLe.minWidth = 120f;

            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            // Set a different value first so Unity refreshes the initial slider visuals even if initial matches the prefab value.
            slider.SetValueWithoutNotify(min - 1f);
            slider.value = initial;
            Slider = slider;

            RestyleNativeSlider(slider);
            UIBuilder.DarkenLabelText(go, label);
            go.SetActive(true);

            // The wrapper participates in row layout while the input is positioned inside it. A layout group would
            // override the input's anchored position if it were a direct child.
            var inputWrapGo = UIBuilder.CreateUIObject(Root.transform, "InputWrap", typeof(RectTransform));
            var wrapLe = UIBuilder.FixHeight(inputWrapGo, THEMED_ROW_HEIGHT);
            wrapLe.preferredWidth = INPUT_WIDTH;
            wrapLe.minWidth = INPUT_MIN_WIDTH;
            Input = UIBuilder.CreateInputField(inputWrapGo.transform, formatValue(Slider.value), ContentTypeFor(wholeNumbers));
            var inputRt = (RectTransform)Input.transform;
            inputRt.anchorMin = new Vector2(0f, 0.5f);
            inputRt.anchorMax = new Vector2(1f, 0.5f);
            inputRt.sizeDelta = new Vector2(0f, THEMED_INPUT_HEIGHT);
            inputRt.anchoredPosition = new Vector2(0f, -3f);
        }

        /// <summary>Sets both controls to the clamped, snapped slider value without calling onChanged or onCommit.</summary>
        public void setValue(float v) {
            suppress = true;
            Slider.value = v;
            Input.text = formatValue(Slider.value);
            suppress = false;
        }

        public float Value => Slider.value;

        /// <summary>Changes the slider range and value without calling onChanged.</summary>
        /// <remarks>
        /// Changing the limits can clamp the slider value. The suppress flag prevents that change from calling
        /// onChanged before setValue applies the requested value.
        /// </remarks>
        public void calibrate(float min, float max, float value) {
            suppress = true;
            Slider.minValue = min;
            Slider.maxValue = max;
            suppress = false;
            setValue(value);
        }

        public void setInteractable(bool interactable) {
            Slider.interactable = interactable;
            Input.interactable = interactable;
        }

        private void onSliderChanged(float v) {
            if (suppress) return;
            suppress = true;
            Input.text = formatValue(v);
            suppress = false;
            onChanged?.Invoke(v);
        }

        private void onInputEndEdit(string text) {
            if (suppress) return;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) {
                setValue(Slider.value); // Unparseable input. Put the current value back in the field.
                return;
            }
            setValue(v);
            onChanged?.Invoke(Slider.value);
            onCommit?.Invoke();
        }

        private string formatValue(float v) {
            return v.ToString(format, CultureInfo.InvariantCulture);
        }

        private static InputField.ContentType ContentTypeFor(bool wholeNumbers) {
            return wholeNumbers ? InputField.ContentType.IntegerNumber : InputField.ContentType.DecimalNumber;
        }

        // The slider calls onChanged for each changed value. PointerUp follows a click or drag, so it provides one
        // commit signal when the interaction ends. Does nothing when onCommit is null.
        private void attachCommitRelay() {
            if (onCommit == null) return;
            var relay = Slider.gameObject.AddComponent<PointerCommitRelay>();
            relay.OnCommit = () => onCommit?.Invoke();
        }

        private class PointerCommitRelay : MonoBehaviour, IPointerUpHandler {
            public Action OnCommit;
            public void OnPointerUp(PointerEventData eventData) => OnCommit?.Invoke();
        }

        // Set heights directly without depending on the prefab's anchors. Identify the track by excluding fill and handle images and their containers.
        private static void RestyleNativeSlider(Slider slider) {
            if (slider == null) return;
            var fill = slider.fillRect;
            var handle = slider.handleRect;

            Image background = null;
            foreach (var img in slider.GetComponentsInChildren<Image>(true)) {
                if (img == null) continue;
                var rt = img.rectTransform;
                if (rt == fill || rt == handle) continue;
                if (fill != null && fill.IsChildOf(rt)) continue; // This is the Fill Area container
                if (handle != null && handle.IsChildOf(rt)) continue; // This is the Handle Slide Area container
                background = img;
                break;
            }

            // Move the track, fill and handle together to reduce the gap below the label without misaligning the bar.
            if (background != null) {
                background.color = UIBuilder.SliderTrackColor;
                VerticalCenterTo(background.rectTransform, UIBuilder.SLIDER_TRACK_HEIGHT, UIBuilder.SLIDER_BAR_RAISE);
            }
            if (fill != null) {
                // Resize the Fill Area so the fill grows with the taller track; use the fill itself if needed.
                VerticalCenterTo(fill.parent as RectTransform ?? fill, UIBuilder.SLIDER_TRACK_HEIGHT, UIBuilder.SLIDER_BAR_RAISE);
            }
            if (handle != null) {
                // Keep the handle at a fixed height and raise it with the track. The Slider controls its width and
                // horizontal position.
                VerticalCenterTo(handle, UIBuilder.SLIDER_HANDLE_HEIGHT, UIBuilder.SLIDER_BAR_RAISE);
            }
        }

        // Keep the width and horizontal position, set a fixed height, and move up from the parent's vertical center by yOffset.
        private static void VerticalCenterTo(RectTransform rt, float height, float yOffset = 0f) {
            if (rt == null) return;
            var min = rt.anchorMin;
            var max = rt.anchorMax;
            min.y = 0.5f;
            max.y = 0.5f;
            rt.anchorMin = min;
            rt.anchorMax = max;
            var size = rt.sizeDelta;
            size.y = height;
            rt.sizeDelta = size;
            var pos = rt.anchoredPosition;
            pos.y = yOffset;
            rt.anchoredPosition = pos;
        }
    }
}
