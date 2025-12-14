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

    private const string BlockRes = "Valheim.Foresight.Assets.Icons.block.png";
    private const string ParryRes = "Valheim.Foresight.Assets.Icons.parry.png";
    private const string DodgeRes = "Valheim.Foresight.Assets.Icons.dodge.png";

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
        using var s = _resources.Open(resourceName);
        if (s == null)
        {
            _log.LogWarning($"[Foresight] Embedded sprite not found: {resourceName}");
            return null;
        }

        using var ms = new MemoryStream();
        s.CopyTo(ms);
        var bytes = ms.ToArray();

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        // Use reflection to avoid Span<T> compilation issues with Unity's ImageConversion
        var imageConvType = typeof(Texture2D).Assembly.GetType("UnityEngine.ImageConversion");
        var loadImageMethod = imageConvType?.GetMethod("LoadImage", 
            new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
        var success = (bool)(loadImageMethod?.Invoke(null, new object[] { tex, bytes, false }) ?? false);
        
        if (!success)
        {
            _log.LogWarning($"[Foresight] PNG decode failed: {resourceName}");
            return null;
        }

        var rect = new Rect(0, 0, tex.width, tex.height);
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
    }
}
