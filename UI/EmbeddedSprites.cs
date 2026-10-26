using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModUIToolkit.UI {
    /// <summary>Loads icons embedded in a mod assembly and caches the decoded images.</summary>
    /// <remarks>
    /// Parkitect's UI sprites are outside Resources and have no public handles, so Resources.Load cannot access them.
    ///
    /// Add image files as EmbeddedResource entries in the project file, for example
    /// &lt;EmbeddedResource Include="Icons\my_icon.png" /&gt;.
    /// Resource names are the project's RootNamespace plus the file's folder path with the separators turned into dots.
    /// Example: A file at Icons\my_icon.png in a project whose RootNamespace is MyMod is "MyMod.Icons.my_icon.png".
    /// When a name doesn't resolve, the error log lists every resource the assembly actually carries.
    /// </remarks>
    public static class EmbeddedSprites {
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        /// <summary>Loads an embedded image from the calling assembly as a Sprite for a Unity UI Image.</summary>
        /// <remarks>
        /// The calling assembly is used, so this overload works when the toolkit is compiled into your mod.
        /// If the toolkit is a separate DLL, use the overload that accepts an assembly.
        /// </remarks>
        /// <param name="resourceName">
        /// Full manifest resource name, such as MyMod.Icons.icon.png.
        /// </param>
        /// <returns>The cached Sprite, or null if the resource is missing or cannot be decoded.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)] // GetCallingAssembly reports the caller's caller if the JIT inlines this method
        public static Sprite Get(string resourceName) {
            return Get(Assembly.GetCallingAssembly(), resourceName);
        }

        /// <summary>Loads an embedded image from the given assembly as a Sprite for a Unity UI Image.</summary>
        /// <param name="resourceName">
        /// Full manifest resource name, such as MyMod.Icons.icon.png.
        /// </param>
        /// <returns>The cached Sprite, or null if the assembly or name is invalid or the resource cannot be decoded.</returns>
        public static Sprite Get(Assembly assembly, string resourceName) {
            if (assembly == null || string.IsNullOrEmpty(resourceName)) return null;
            var key = assembly.GetName().Name + "|" + resourceName;
            if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var texture = GetTexture(assembly, resourceName);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = resourceName;
            spriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>Loads an embedded image from the calling assembly into a Texture2D.</summary>
        /// <param name="resourceName">Full manifest resource name, such as MyMod.Icons.icon.png.</param>
        [MethodImpl(MethodImplOptions.NoInlining)] // GetCallingAssembly reports the caller's caller if the JIT inlines this method
        public static Texture2D GetTexture(string resourceName) {
            return GetTexture(Assembly.GetCallingAssembly(), resourceName);
        }

        /// <summary>Loads an embedded image into a Texture2D and caches it by assembly and resource name.</summary>
        /// <param name="resourceName">Full manifest resource name, such as MyMod.Icons.icon.png.</param>
        /// <returns>
        /// The cached texture, or null if the assembly or name is invalid or the resource is missing or cannot be decoded.
        /// </returns>
        public static Texture2D GetTexture(Assembly assembly, string resourceName) {
            if (assembly == null || string.IsNullOrEmpty(resourceName)) return null;
            var key = assembly.GetName().Name + "|" + resourceName;
            if (textureCache.TryGetValue(key, out var cached) && cached != null) return cached;

            Texture2D texture = null;
            bool textureCached = false;
            try {
                using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                    if (stream == null) {
                        // A mismatch in the root namespace, folder path, or casing is the usual cause.
                        // Log available names so the correct resource name can be copied.
                        UILog.Error("EmbeddedSprites: resource NOT FOUND: " + resourceName + " - available: " + string.Join(", ", assembly.GetManifestResourceNames()));
                        return null;
                    }
                    var bytes = new byte[stream.Length];
                    int read = 0;
                    while (read < bytes.Length) {
                        int chunk = stream.Read(bytes, read, bytes.Length - read);
                        if (chunk <= 0) break;
                        read += chunk;
                    }
                    // These icons are drawn near their authored size; mipmaps would soften them.
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(bytes)) {
                        UILog.Error("EmbeddedSprites: resource is not a decodable image: " + resourceName);
                        return null;
                    }
                    texture.wrapMode = TextureWrapMode.Clamp; // an icon must never tile
                    texture.filterMode = FilterMode.Bilinear;
                    textureCache[key] = texture;
                    textureCached = true;
                    UILog.Debug($"EmbeddedSprites: loaded {resourceName} ({texture.width}x{texture.height})");
                    return texture;
                }
            } catch (Exception e) {
                UILog.Error("EmbeddedSprites: failed to load " + resourceName + ": " + e);
                return null;
            } finally {
                // The cache owns successful loads; failed loads must release their native texture allocation.
                if (texture != null && !textureCached) UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
