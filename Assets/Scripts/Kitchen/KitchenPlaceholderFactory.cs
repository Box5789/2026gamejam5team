using UnityEngine;

namespace KimbapGame.Kitchen
{
    public static class KitchenPlaceholderFactory
    {
        private static Sprite whiteSprite;

        public static Sprite CreateWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "KitchenWhitePlaceholderTexture",
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            whiteSprite.name = "KitchenWhitePlaceholderSprite";
            return whiteSprite;
        }
    }
}
