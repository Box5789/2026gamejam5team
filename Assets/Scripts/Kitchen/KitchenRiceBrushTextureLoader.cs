using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    public static class KitchenRiceBrushTextureLoader
    {
        private const string ResourcesPrefix = "Assets/Resources/";
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

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

            Texture2D texture = Resources.Load<Texture2D>(normalizedPath);
            if (texture == null)
            {
                Debug.LogWarning($"Kitchen rice brush texture not found in Resources: {normalizedPath}");
                return null;
            }

            Texture2D readableTexture = EnsureReadable(texture, normalizedPath);
            Cache[normalizedPath] = readableTexture;
            return readableTexture;
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
                return texture;
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
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
