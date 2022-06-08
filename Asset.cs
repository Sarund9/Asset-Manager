/*
SUPER SIMPLE ASSET MANAGER

1. Initialize using Asset.Init()
2. Create a loader using Asset.

MIT License

Copyright (c) 2022 Sarund9

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StackMod;

public delegate object? AssetLoader(string path);

public class Asset
{
    
    #region DATA
    
    readonly ConcurrentDictionary<(Type, string), WeakReference> assets
        = new ConcurrentDictionary<(Type, string), WeakReference>();

    readonly Dictionary<Type, AssetLoader> loaders
        = new Dictionary<Type, AssetLoader>();

    private string m_AssetPath = "";

    #endregion

    #region SINGLETON
    private Asset() { }

    public static Asset I { get; } = new Asset();
    public static bool Initialized { get; private set; }

    public static bool Initialize(string assetPath)
    {
        if (Initialized)
            return false;
        I.m_AssetPath = assetPath;
        return true;
    }
    private static void VerifyInit()
    {
        if (!Initialized)
            throw new InvalidOperationException("Asset system not initialized");
    }

    #endregion

    #region LOADERS

    public static bool AddLoader<T>(AssetLoader loader) =>
        AddLoader(typeof(T), loader);

    public static bool AddLoader(Type type, AssetLoader loader) =>
        I.loaders.TryAdd(type, loader);

    #endregion

    #region LOAD_FUNC

    // TODO: LoadAsync()
    public static T? Load<T>(string assetPath) =>
        (T?) Load(typeof(T), assetPath);

    public static object? Load(Type type, string assetPath)
    {
        if (I.loaders.TryGetValue(type, out var loader))
        {
            return I.GetAsset(type, assetPath, loader);
        }

        return null;
    }

    #endregion

    #region MISC

    // TODO: ClearCacheAsync()

    public static void ClearCache()
    {
        lock (I.assets)
        {
            HashSet<(Type, string)> set =
                new HashSet<(Type, string)>();
            foreach (var ase in I.assets)
                if (ase.Value.Target == null)
                    set.Add(ase.Key);
            
            foreach (var rem in set)
                I.assets.TryRemove(rem, out var _);
        }
    }

    #endregion

    #region PRIVATE

    object? GetAsset(Type type, string assetPath, AssetLoader loader)
    {
        if (assets.TryGetValue((type, assetPath), out var val))
        {
            var t = val.Target;
            if (t != null)
                return t;
        }
        var obj = loader(assetPath);
        if (obj != null)
            assets.TryAdd((type, assetPath), new WeakReference(obj));

        return obj;
    }

    #endregion
}


