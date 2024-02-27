using UnityEngine;

namespace Foxglove.Settings {
    [CreateAssetMenu(fileName = "New Interface Theme", menuName = "Foxglove/Interface Theme")]
    public sealed class InterfaceTheme : ScriptableObject {
        public Color BackgroundMain;
        public Color BackgroundSecondary;
        public Color Border;
        public Color BorderActive;
        public Color BorderHover;
        public Color TextFaint;
        public Color TextMuted;
        public Color TextPrimary;
        public Color Red;
        public Color Orange;
        public Color Yellow;
        public Color Green;
        public Color Cyan;
        public Color Blue;
        public Color Purple;
        public Color Magenta;

        private void Reset() {
            BackgroundMain = new Color(16 / 255f, 15 / 255f, 15 / 255f);
            BackgroundSecondary = new Color(28 / 255f, 27 / 255f, 26 / 255f);
            Border = new Color(40 / 255f, 39 / 255f, 38 / 255f);
            BorderActive = new Color(64 / 255f, 62 / 255f, 60 / 255f);
            BorderHover = new Color(52 / 255f, 51 / 255f, 49 / 255f);
            TextFaint = new Color(87 / 255f, 86 / 255f, 83 / 255f);
            TextMuted = new Color(135 / 255f, 133 / 255f, 128 / 255f);
            TextPrimary = new Color(206 / 255f, 205 / 255f, 195 / 255f);
            Red = new Color(209 / 255f, 77 / 255f, 65 / 255f);
            Orange = new Color(218 / 255f, 112 / 255f, 44 / 255f);
            Yellow = new Color(208 / 255f, 162 / 255f, 21 / 255f);
            Green = new Color(135 / 255f, 154 / 255f, 57 / 255f);
            Cyan = new Color(58 / 255f, 169 / 255f, 159 / 255f);
            Blue = new Color(67 / 255f, 133 / 255f, 190 / 255f);
            Purple = new Color(139 / 255f, 126 / 255f, 200 / 255f);
            Magenta = new Color(206 / 255f, 93 / 255f, 151 / 255f);
        }
    }
}
