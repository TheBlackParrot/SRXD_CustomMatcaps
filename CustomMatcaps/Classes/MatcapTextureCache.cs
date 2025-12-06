using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomMatcaps.Classes;

internal static class MatcapTextureCache
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    internal static async Task<Texture2D> Add(string path)
    {
        if (Cache.TryGetValue(path, out Texture2D? cachedTexture))
        {
            return cachedTexture;
        }
        
        byte[] bytes = await File.ReadAllBytesAsync(path);
        
        await Awaitable.MainThreadAsync();
        
        Texture2D texture = new(1, 1);
        texture.LoadImage(bytes);

        try
        {
            Cache.Add(path, texture);
        }
        catch (ArgumentException)
        {
            texture = Cache[path];
        }
        
        return texture;
    }

    internal static Texture2D? Get(string path) => Cache.GetValueOrDefault(path);
}