using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Hud.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Services.Hud;

public sealed class AssemblyEmbeddedResourceStreamProvider : IEmbeddedResourceStreamProvider
{
    private readonly Assembly _asm;

    public AssemblyEmbeddedResourceStreamProvider(Assembly asm) => _asm = asm;

    public Stream? Open(string resourceName) => _asm.GetManifestResourceStream(resourceName);

    public string[] GetNames() => _asm.GetManifestResourceNames();
}

public sealed class EmbeddedPngSpriteProvider : IThreatIconSpriteProvider
{
    private readonly IEmbeddedResourceStreamProvider _resources;
    private readonly ILogger _log;
    private readonly Dictionary<ThreatResponseHint, Sprite?> _cache = new();

    private const string BlockRes = "Valheim.Foresight.Assets.Icons.block_icon.png";
    private const string ParryRes = "Valheim.Foresight.Assets.Icons.parry_icon.png";
    private const string DodgeRes = "Valheim.Foresight.Assets.Icons.roll_icon.png";

    public EmbeddedPngSpriteProvider(IEmbeddedResourceStreamProvider resources, ILogger log)
    {
        _resources = resources;
        _log = log;
        _cache[ThreatResponseHint.Block] = Load(BlockRes);
        _cache[ThreatResponseHint.Parry] = Load(ParryRes);
        _cache[ThreatResponseHint.Dodge] = Load(DodgeRes);
        _cache[ThreatResponseHint.None] = null;
    }

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

        var rect = new Rect(0, 0, tex.width, tex.height);
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
    }
}
