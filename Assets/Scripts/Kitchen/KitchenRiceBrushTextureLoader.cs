using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    public static class KitchenRiceBrushTextureLoader
    {
        private const string ResourcesPrefix = "Assets/Resources/";
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> DuplicateWarningKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Sprite> spriteNameIndex;
        private static Dictionary<string, Texture2D> textureNameIndex;

        public static Texture2D Load(string resourcesPath)
        {
            string normalizedPath = NormalizeResourcePath(resourcesPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return null;
            }

            if (Cache.TryGetValue(normalizedPath, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            Texture2D texture = LoadTextureByDirectPath(normalizedPath);
            if (texture == null)
            {
                texture = LoadSpriteByDirectPath(normalizedPath);
            }

            if (texture == null)
            {
                texture = LoadSpriteByName(normalizedPath);
            }

            if (texture == null)
            {
                texture = LoadTextureByName(normalizedPath);
            }

            if (texture == null)
            {
                Debug.LogWarning($"Kitchen rice brush texture not found in Resources: {normalizedPath}");
                return null;
            }

            Cache[normalizedPath] = texture;
            return texture;
        }

        public static string NormalizeResourcePath(string resourcesPath)
        {
            string normalizedPath = (resourcesPath ?? string.Empty).Trim().Replace('\\', '/');
            if (normalizedPath.StartsWith(ResourcesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath.Substring(ResourcesPrefix.Length);
            }

            string extension = Path.GetExtension(normalizedPath);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                normalizedPath = normalizedPath.Substring(0, normalizedPath.Length - extension.Length);
            }

            return normalizedPath.Trim('/');
        }

        private static Texture2D LoadTextureByDirectPath(string normalizedPath)
        {
            Texture2D texture = Resources.Load<Texture2D>(normalizedPath);
            return texture == null ? null : EnsureReadable(texture, normalizedPath);
        }

        private static Texture2D LoadSpriteByDirectPath(string normalizedPath)
        {
            Sprite sprite = Resources.Load<Sprite>(normalizedPath);
            if (sprite != null)
            {
                return CreateReadableTextureFromSprite(sprite, normalizedPath);
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(normalizedPath) ?? new Sprite[0];
            sprite = FindSprite(sprites, GetLastPathSegment(normalizedPath));
            return sprite == null ? null : CreateReadableTextureFromSprite(sprite, normalizedPath);
        }

        private static Texture2D LoadSpriteByName(string normalizedPath)
        {
            Dictionary<string, Sprite> index = GetSpriteNameIndex();
            return index.TryGetValue(NormalizeLookupKey(normalizedPath), out Sprite sprite)
                ? CreateReadableTextureFromSprite(sprite, normalizedPath)
                : null;
        }

        private static Texture2D LoadTextureByName(string normalizedPath)
        {
            Dictionary<string, Texture2D> index = GetTextureNameIndex();
            return index.TryGetValue(NormalizeLookupKey(normalizedPath), out Texture2D texture)
                ? EnsureReadable(texture, normalizedPath)
                : null;
        }

        private static Dictionary<string, Sprite> GetSpriteNameIndex()
        {
            if (spriteNameIndex != null)
            {
                return spriteNameIndex;
            }

            spriteNameIndex = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            Sprite[] sprites = Resources.LoadAll<Sprite>(string.Empty) ?? new Sprite[0];
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite != null)
                {
                    AddIndexedResource(spriteNameIndex, sprite.name, sprite, "sprite");
                }
            }

            return spriteNameIndex;
        }

        private static Dictionary<string, Texture2D> GetTextureNameIndex()
        {
            if (textureNameIndex != null)
            {
                return textureNameIndex;
            }

            textureNameIndex = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            Texture2D[] textures = Resources.LoadAll<Texture2D>(string.Empty) ?? new Texture2D[0];
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                if (texture != null)
                {
                    AddIndexedResource(textureNameIndex, texture.name, texture, "texture");
                }
            }

            return textureNameIndex;
        }

        private static void AddIndexedResource<T>(
            Dictionary<string, T> index,
            string key,
            T resource,
            string resourceKind)
            where T : UnityEngine.Object
        {
            string normalizedKey = NormalizeLookupKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                return;
            }

            if (index.ContainsKey(normalizedKey))
            {
                string warningKey = $"{resourceKind}:{normalizedKey}";
                if (DuplicateWarningKeys.Add(warningKey))
                {
                    Debug.LogWarning($"Duplicate kitchen rice brush {resourceKind} name '{normalizedKey}' found in Resources. Using the first match.");
                }

                return;
            }

            index.Add(normalizedKey, resource);
        }

        private static Sprite FindSprite(Sprite[] sprites, string desiredName)
        {
            string normalizedName = NormalizeLookupKey(desiredName);
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite != null && string.Equals(NormalizeLookupKey(sprite.name), normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return sprite;
                }
            }

            return sprites.Length == 1 ? sprites[0] : null;
        }

        private static Texture2D CreateReadableTextureFromSprite(Sprite sprite, string resourcesPath)
        {
            if (sprite == null || sprite.texture == null)
            {
                return null;
            }

            Texture2D readableSource = EnsureReadable(sprite.texture, resourcesPath);
            if (readableSource == null)
            {
                return null;
            }

            Rect textureRect = sprite.textureRect;
            int xMin = Mathf.Clamp(Mathf.FloorToInt(textureRect.xMin), 0, readableSource.width);
            int yMin = Mathf.Clamp(Mathf.FloorToInt(textureRect.yMin), 0, readableSource.height);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(textureRect.xMax), 0, readableSource.width);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(textureRect.yMax), 0, readableSource.height);
            int width = xMax - xMin;
            int height = yMax - yMin;
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            try
            {
                Color[] pixels = readableSource.GetPixels(xMin, yMin, width, height);
                Texture2D croppedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = $"{sprite.name}_BrushTexture"
                };
                croppedTexture.SetPixels(pixels);
                croppedTexture.Apply(false, false);
                return croppedTexture;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Kitchen rice brush sprite could not be cropped: {resourcesPath}. {exception.Message}");
                return null;
            }
        }

        private static Texture2D EnsureReadable(Texture2D texture, string resourcesPath)
        {
            if (texture == null || IsReadable(texture))
            {
                return texture;
            }

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture temporary = null;
            try
            {
                temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;

                Texture2D readableCopy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false)
                {
                    name = $"{texture.name}_ReadableCopy"
                };
                readableCopy.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                readableCopy.Apply(false, false);
                return readableCopy;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Kitchen rice brush texture could not be made readable: {resourcesPath}. {exception.Message}");
                return null;
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (temporary != null)
                {
                    RenderTexture.ReleaseTemporary(temporary);
                }
            }
        }

        private static bool IsReadable(Texture2D texture)
        {
            try
            {
                texture.GetPixels32();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string NormalizeLookupKey(string value)
        {
            return GetLastPathSegment(NormalizeResourcePath(value));
        }

        private static string GetLastPathSegment(string value)
        {
            string normalizedValue = NormalizeResourcePath(value);
            int separatorIndex = normalizedValue.LastIndexOf('/');
            return separatorIndex < 0 ? normalizedValue : normalizedValue.Substring(separatorIndex + 1);
        }
    }
}
