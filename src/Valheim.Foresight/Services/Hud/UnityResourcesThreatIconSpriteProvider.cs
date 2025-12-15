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

    private const string BlockResource = "Valheim.Foresight.Assets.Icons.block_icon.png";
    private const string ParryResource = "Valheim.Foresight.Assets.Icons.parry_icon.png";
    private const string DodgeResource = "Valheim.Foresight.Assets.Icons.roll_icon.png";

    public UnityResourcesThreatIconSpriteProvider(IResourcesWrapper resources, ILogger logger)
    {
        _resources = resources;
        _logger = logger;
        Preload();
    }

    private void Preload()
    {
        _cache[ThreatResponseHint.Block] = LoadSprite(BlockResource);
        _cache[ThreatResponseHint.Parry] = LoadSprite(ParryResource);
        _cache[ThreatResponseHint.Dodge] = LoadSprite(DodgeResource);
        _cache[ThreatResponseHint.None] = null;

        _logger.LogDebug(
            $"[{nameof(Preload)}] Preload completed. "
                + $"Block={_cache[ThreatResponseHint.Block] is not null}, "
                + $"Parry={_cache[ThreatResponseHint.Parry] is not null}, "
                + $"Dodge={_cache[ThreatResponseHint.Dodge] is not null}"
        );
    }

    private Sprite? LoadSprite(string path)
    {
        var sprite = _resources.LoadSprite(path);
        if (sprite == null)
            _logger.LogWarning($"Failed to load threat icon sprite at '{path}'.");
        return sprite;
    }

    public Sprite? GetIcon(ThreatResponseHint hint)
    {
        if (!_cache.TryGetValue(hint, out var sprite))
        {
            _logger.LogWarning($"No cached sprite entry for hint={hint}.");
            return null;
        }

        _logger.LogDebug(
            $"[{nameof(GetIcon)}] {nameof(GetIcon)}(hint={hint}) -> spriteNull={sprite is null}"
        );
        return sprite;
    }
}
