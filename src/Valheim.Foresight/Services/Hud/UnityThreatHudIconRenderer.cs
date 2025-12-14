using UnityEngine;
using UnityEngine.UI;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Hud.Interfaces;

namespace Valheim.Foresight.Services.Hud;

/// <summary>
/// Unity-specific implementation that adds a small sprite next to the enemy name.
/// </summary>
public sealed class UnityThreatHudIconRenderer : IThreatHudIconRenderer
{
    private const string IconObjectName = "Foresight_ThreatIcon";
    private const float IconOffsetX = 40f; // todo: -> config
    private const float IconSize = 16f; // todo: -> config

    private readonly IThreatIconSpriteProvider _spriteProvider;

    public UnityThreatHudIconRenderer(IThreatIconSpriteProvider spriteProvider)
    {
        _spriteProvider = spriteProvider;
    }

    public void RenderIcon(EnemyHud.HudData hud, ThreatResponseHint hint)
    {
        if (hud?.m_name is null)
            return;

        var enemyName = hud.m_name.text;
        ValheimForesightPlugin.Log?.LogDebug(
            $"[Foresight][{nameof(RenderIcon)}]: enemy='{enemyName}', hint={hint}"
        );

        var iconObject = GetOrCreateIconObject(hud.m_name.transform);
        var image = iconObject.GetComponent<Image>() ?? iconObject.AddComponent<Image>();

        var sprite = _spriteProvider.GetIcon(hint);
        var shouldShow = hint is not ThreatResponseHint.None && sprite is not null;
        ValheimForesightPlugin.Log?.LogDebug(
            $"[Foresight][{nameof(RenderIcon)}]: enemy='{enemyName}', "
                + $"hint={hint}, spriteNull={sprite is null}, shouldShow={shouldShow}"
        );

        iconObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            if (hint is not ThreatResponseHint.None && sprite is null)
            {
                ValheimForesightPlugin.Log?.LogWarning(
                    $"[Foresight] Sprite is null for hint={hint}, hiding icon. "
                        + $"Check ThreatIconSpriteProvider paths."
                );
            }
        }

        image.sprite = sprite;
        image.preserveAspect = true;
    }

    private GameObject GetOrCreateIconObject(Transform nameTransform)
    {
        var parent = nameTransform.parent ?? nameTransform;

        var existing = parent.Find(IconObjectName);
        if (existing != null)
            return existing.gameObject;

        ValheimForesightPlugin.Log?.LogDebug(
            $"[Foresight][{nameof(GetOrCreateIconObject)}] Creating new icon object '{IconObjectName}' "
                + $"under parent '{parent.name}'."
        );

        var go = new GameObject(IconObjectName);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(IconOffsetX, 0f);
        rect.sizeDelta = new Vector2(IconSize, IconSize);

        return go;
    }
}
