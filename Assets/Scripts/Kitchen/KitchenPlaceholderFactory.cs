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

        public static GameObject CreateSpriteObject(string name, Transform parent, Vector3 position, Vector2 size, Color color, int sortingOrder = 0)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return gameObject;
        }

        public static TextMesh CreateLabel(string text, Transform parent, Vector3 position, int fontSize = 44, float characterSize = 0.08f)
        {
            GameObject labelObject = new GameObject($"{text}_Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = position;

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            return textMesh;
        }
    }
}
