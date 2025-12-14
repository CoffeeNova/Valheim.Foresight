using System.Collections.Generic;
using UnityEngine;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Hud.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Services.Hud;

public sealed class UnityResourcesThreatIconSpriteProvider : IThreatIconSpriteProvider
{
    private readonly IResourcesWrapper _resources;
    private readonly ILogger _logger;
    private readonly Dictionary<ThreatResponseHint, Sprite?> _cache = new();

    // todo: adjust to actual Resources / AssetBundle paths.
    private const string BlockIconPath = "Assets/Icons/block_icon.png";
    private const string ParryIconPath = "Assets/Icons/parry_icon.png";
    private const string DodgeIconPath = "Assets/Icons/roll_icon.png";

    public UnityResourcesThreatIconSpriteProvider(IResourcesWrapper resources, ILogger logger)
    {
        _resources = resources;
        _logger = logger;
        Preload();
    }

    private void Preload()
    {
        _cache[ThreatResponseHint.Block] = LoadSprite(BlockIconPath);
        _cache[ThreatResponseHint.Parry] = LoadSprite(ParryIconPath);
        _cache[ThreatResponseHint.Dodge] = LoadSprite(DodgeIconPath);
        _cache[ThreatResponseHint.None] = null;

        _logger.LogDebug(
            $"[Foresight][{nameof(Preload)}] Preload completed. "
                + $"Block={_cache[ThreatResponseHint.Block] is not null}, "
                + $"Parry={_cache[ThreatResponseHint.Parry] is not null}, "
                + $"Dodge={_cache[ThreatResponseHint.Dodge] is not null}"
        );
    }

    private Sprite? LoadSprite(string path)
    {
        var sprite = _resources.LoadSprite(path);
        if (sprite == null)
            _logger.LogWarning($"[Foresight] Failed to load threat icon sprite at '{path}'.");
        return sprite;
    }

    public Sprite? GetIcon(ThreatResponseHint hint)
    {
        if (!_cache.TryGetValue(hint, out var sprite))
        {
            _logger.LogWarning($"[Foresight] No cached sprite entry for hint={hint}.");
            return null;
        }

        _logger.LogDebug(
            $"[Foresight][{nameof(GetIcon)}] {nameof(GetIcon)}(hint={hint}) -> spriteNull={sprite is null}"
        );
        return sprite;
    }
}
