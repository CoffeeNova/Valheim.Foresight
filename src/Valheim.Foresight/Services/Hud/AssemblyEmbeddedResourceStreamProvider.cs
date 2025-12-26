using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Hud.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;
using Object = UnityEngine.Object;

namespace Valheim.Foresight.Services.Hud;

/// <summary>
/// Provides access to assembly embedded resources
/// </summary>
public sealed class AssemblyEmbeddedResourceStreamProvider : IEmbeddedResourceStreamProvider
{
    private readonly Assembly _asm;

    /// <summary>
    /// Creates a new provider for the given assembly
    /// </summary>
    public AssemblyEmbeddedResourceStreamProvider(Assembly asm) => _asm = asm;

    /// <inheritdoc/>
    public Stream? Open(string resourceName) => _asm.GetManifestResourceStream(resourceName);

    /// <inheritdoc/>
    public string[] GetNames() => _asm.GetManifestResourceNames();
}

/// <summary>
/// Provides threat icons by loading PNG sprites from embedded assembly resources
/// </summary>
public sealed class EmbeddedPngSpriteProvider : IThreatIconSpriteProvider, IDisposable
{
    private readonly IEmbeddedResourceStreamProvider _resources;
    private readonly ILogger _log;
    private readonly Dictionary<ThreatResponseHint, Sprite?> _cache = new();

    private const string BlockRes = "Valheim.Foresight.Assets.Icons.block_icon.png";
    private const string ParryRes = "Valheim.Foresight.Assets.Icons.parry_icon.png";
    private const string DodgeRes = "Valheim.Foresight.Assets.Icons.roll_icon.png";

    private readonly List<Sprite> _createdSprites = new();
    private readonly List<Texture2D> _createdTextures = new();

    /// <summary>
    /// Creates a new embedded PNG sprite provider
    /// </summary>
    public EmbeddedPngSpriteProvider(IEmbeddedResourceStreamProvider resources, ILogger log)
    {
        _resources = resources;
        _log = log;
        _cache[ThreatResponseHint.Block] = Load(BlockRes);
        _cache[ThreatResponseHint.Parry] = Load(ParryRes);
        _cache[ThreatResponseHint.Dodge] = Load(DodgeRes);
        _cache[ThreatResponseHint.None] = null;
    }

    /// <inheritdoc/>
    public Sprite? GetIcon(ThreatResponseHint hint) =>
        _cache.TryGetValue(hint, out var s) ? s : null;

    private Sprite? Load(string resourceName)
    {
        using var sprite = _resources.Open(resourceName);
        if (sprite == null)
        {
            _log.LogWarning($"Embedded sprite not found: {resourceName}");
            return null;
        }

        using var ms = new MemoryStream();
        sprite.CopyTo(ms);
        var bytes = ms.ToArray();
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        // var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        tex.LoadImage(bytes);
        _createdTextures.Add(tex);

        var rect = new Rect(0, 0, tex.width, tex.height);
        var createdSprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
        _createdSprites.Add(createdSprite);
        return createdSprite;
    }

    public void Dispose()
    {
        foreach (var sprite in _createdSprites)
        {
            if (sprite != null)
                Object.Destroy(sprite);
        }
        _createdSprites.Clear();

        foreach (var texture in _createdTextures)
        {
            if (texture != null)
                Object.Destroy(texture);
        }

        _createdTextures.Clear();
    }
}
