using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KimbapGame.Kitchen
{
    internal static class KitchenButtonSpriteUtility
    {
        private static readonly Dictionary<string, Sprite> RuntimeSprites = new Dictionary<string, Sprite>();

        public static void Apply(Button button, string resourcePath, bool preserveAspect, Object context)
        {
            if (button == null || string.IsNullOrWhiteSpace(resourcePath))
            {
                return;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image == null)
            {
                return;
            }

            Sprite sprite = LoadSprite(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning($"Could not load kitchen button sprite from Resources path '{resourcePath}'.", context);
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            button.targetGraphic = image;
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            if (RuntimeSprites.TryGetValue(resourcePath, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            Sprite runtimeSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeSprite.name = texture.name;
            RuntimeSprites[resourcePath] = runtimeSprite;
            return runtimeSprite;
        }
    }
}
