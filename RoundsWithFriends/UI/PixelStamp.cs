using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RWF.UI
{
    public static class PixelStamp
    {
        public const string Text = "BAG'S MOD";
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;
        public const int LetterGap = 3;

        public static readonly Color Green = new Color(0.25f, 0.95f, 0.35f, 1f);

        static readonly Dictionary<char, int[]> Glyphs = new Dictionary<char, int[]>
        {
            { 'A', new[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 } },
            { 'B', new[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 } },
            { 'D', new[] { 0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110 } },
            { 'G', new[] { 0b01111, 0b10000, 0b10000, 0b10111, 0b10001, 0b10001, 0b01111 } },
            { 'M', new[] { 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001 } },
            { 'O', new[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 } },
            { 'S', new[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110 } },
            { '\'', new[] { 0b00110, 0b00110, 0b01100, 0b00000, 0b00000, 0b00000, 0b00000 } },
        };

        static Sprite sprite;

        public static RectTransform Build(Transform parent, int pixel)
        {
            GameObject root = new GameObject("PixelTitle");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;

            string text = PixelStamp.Text;
            int cursor = 0;
            int width = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    width += pixel * 3;
                    continue;
                }

                width += PixelStamp.GlyphWidth * pixel;
                if (i < text.Length - 1 && text[i + 1] != ' ')
                {
                    width += PixelStamp.LetterGap;
                }
            }

            rootRect.sizeDelta = new Vector2(width, PixelStamp.GlyphHeight * pixel);
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == ' ')
                {
                    cursor += pixel * 3;
                    continue;
                }

                int[] rows;
                if (!PixelStamp.Glyphs.TryGetValue(character, out rows))
                {
                    cursor += PixelStamp.GlyphWidth * pixel + PixelStamp.LetterGap;
                    continue;
                }

                for (int y = 0; y < rows.Length; y++)
                {
                    int bits = rows[y];
                    for (int x = 0; x < PixelStamp.GlyphWidth; x++)
                    {
                        if ((bits & (1 << (PixelStamp.GlyphWidth - 1 - x))) == 0)
                        {
                            continue;
                        }

                        GameObject dot = new GameObject("Px");
                        dot.transform.SetParent(root.transform, false);
                        RectTransform dotRect = dot.AddComponent<RectTransform>();
                        dotRect.anchorMin = new Vector2(0f, 1f);
                        dotRect.anchorMax = new Vector2(0f, 1f);
                        dotRect.pivot = new Vector2(0f, 1f);
                        dotRect.sizeDelta = new Vector2(pixel, pixel);
                        dotRect.anchoredPosition = new Vector2(cursor + x * pixel, -(y * pixel));
                        Image image = dot.AddComponent<Image>();
                        image.sprite = PixelStamp.Sprite;
                        image.color = PixelStamp.Green;
                        image.raycastTarget = false;
                    }
                }

                cursor += PixelStamp.GlyphWidth * pixel + PixelStamp.LetterGap;
            }

            return rootRect;
        }

        static Sprite Sprite
        {
            get
            {
                if (PixelStamp.sprite == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    PixelStamp.sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }

                return PixelStamp.sprite;
            }
        }
    }
}
