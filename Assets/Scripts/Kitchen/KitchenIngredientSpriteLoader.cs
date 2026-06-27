using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    public static class KitchenIngredientSpriteLoader
    {
        private const string ResourcesPrefix = "Assets/Resources/";
        private static readonly string[] SearchRoots = { "Kitchen", "Kitchen/재료" };
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite[]> SpriteSetCache = new Dictionary<string, Sprite[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Dictionary<string, Sprite>> RootIndexCache = new Dictionary<string, Dictionary<string, Sprite>>(StringComparer.OrdinalIgnoreCase);

        public static Sprite Load(string imageName, string displayName, string variantId)
        {
            string explicitImageName = NormalizeResourcePath(imageName);
            if (!string.IsNullOrWhiteSpace(explicitImageName))
            {
                Sprite explicitSprite = LoadCandidate(explicitImageName);
                if (explicitSprite != null)
                {
                    return explicitSprite;
                }

                Debug.LogWarning($"Kitchen ingredient sprite not found in Resources: {explicitImageName}");
            }

            Sprite displayNameSprite = LoadCandidate(displayName);
            if (displayNameSprite != null)
            {
                return displayNameSprite;
            }

            return LoadCandidate(variantId);
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

        private static Sprite LoadCandidate(string candidate)
        {
            string normalizedCandidate = NormalizeResourcePath(candidate);
            if (string.IsNullOrWhiteSpace(normalizedCandidate))
            {
                return null;
            }

            if (Cache.TryGetValue(normalizedCandidate, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Sprite sprite = LooksLikePath(normalizedCandidate)
                ? LoadByPath(normalizedCandidate)
                : LoadByName(normalizedCandidate);
            if (sprite == null && LooksLikePath(normalizedCandidate))
            {
                sprite = LoadByName(GetLastPathSegment(normalizedCandidate));
            }

            Cache[normalizedCandidate] = sprite;
            return sprite;
        }

        private static Sprite LoadByPath(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            Sprite[] sprites = LoadAllSprites(resourcePath);
            return FindSprite(sprites, GetLastPathSegment(resourcePath), sprites.Length == 1);
        }

        private static Sprite LoadByName(string spriteName)
        {
            string normalizedName = NormalizeLookupKey(spriteName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            for (int i = 0; i < SearchRoots.Length; i++)
            {
                Sprite sprite = LoadByPath($"{SearchRoots[i]}/{normalizedName}");
                if (sprite != null)
                {
                    return sprite;
                }
            }

            for (int i = 0; i < SearchRoots.Length; i++)
            {
                Dictionary<string, Sprite> index = GetRootIndex(SearchRoots[i]);
                if (index.TryGetValue(normalizedName, out Sprite sprite))
                {
                    return sprite;
                }
            }

            return null;
        }

        private static Sprite[] LoadAllSprites(string resourcePath)
        {
            string normalizedPath = NormalizeResourcePath(resourcePath);
            if (SpriteSetCache.TryGetValue(normalizedPath, out Sprite[] cachedSprites))
            {
                return cachedSprites;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(normalizedPath) ?? new Sprite[0];
            SpriteSetCache[normalizedPath] = sprites;
            return sprites;
        }

        private static Dictionary<string, Sprite> GetRootIndex(string root)
        {
            string normalizedRoot = NormalizeResourcePath(root);
            if (RootIndexCache.TryGetValue(normalizedRoot, out Dictionary<string, Sprite> cachedIndex))
            {
                return cachedIndex;
            }

            Dictionary<string, Sprite> index = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            Sprite[] sprites = LoadAllSprites(normalizedRoot);
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite != null)
                {
                    AddSpriteIndex(index, sprite.name, sprite);
                }
            }

            RootIndexCache[normalizedRoot] = index;
            return index;
        }

        private static Sprite FindSprite(Sprite[] sprites, string desiredName, bool allowSingleFallback)
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

            return allowSingleFallback && sprites.Length == 1 ? sprites[0] : null;
        }

        private static void AddSpriteIndex(Dictionary<string, Sprite> index, string key, Sprite sprite)
        {
            string normalizedKey = NormalizeLookupKey(key);
            if (!string.IsNullOrWhiteSpace(normalizedKey) && !index.ContainsKey(normalizedKey))
            {
                index.Add(normalizedKey, sprite);
            }
        }

        private static string NormalizeLookupKey(string value)
        {
            return NormalizeResourcePath(value);
        }

        private static bool LooksLikePath(string value)
        {
            return value.Contains("/");
        }

        private static string GetLastPathSegment(string value)
        {
            string normalizedValue = NormalizeResourcePath(value);
            int separatorIndex = normalizedValue.LastIndexOf('/');
            return separatorIndex < 0 ? normalizedValue : normalizedValue.Substring(separatorIndex + 1);
        }
    }
}
