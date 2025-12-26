using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Hud.Interfaces;

namespace Valheim.Foresight.Services.Hud;

/// <summary>
/// Unity-specific implementation that adds a small sprite next to the enemy name.
/// </summary>
public sealed class UnityThreatHudIconRenderer : IThreatHudIconRenderer, IDisposable
{
    private const string IconObjectName = "Foresight_ThreatIcon";

    private readonly IThreatIconSpriteProvider _spriteProvider;
    private readonly List<GameObject> _createdIcons = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new threat HUD icon renderer
    /// </summary>
    public UnityThreatHudIconRenderer(IThreatIconSpriteProvider spriteProvider)
    {
        _spriteProvider = spriteProvider;
    }

    /// <inheritdoc/>
    public void RenderIcon(TextMeshProUGUI? nameLabel, ThreatResponseHint hint)
    {
        if (nameLabel is null)
            return;

        var config = ValheimForesightPlugin.ForesightConfig;
        if (config == null || !config.ThreatIconEnabled.Value)
        {
            // Hide icon if disabled
            var existingIcon = GetExistingIconObject(nameLabel.transform);
            if (existingIcon != null)
                existingIcon.SetActive(false);
            return;
        }

        var enemyName = nameLabel.text;
        var iconObject = GetOrCreateIconObject(nameLabel.transform);
        var image = iconObject.GetComponent<Image>() ?? iconObject.AddComponent<Image>();
        var sprite = _spriteProvider.GetIcon(hint);
        var shouldShow = hint is not ThreatResponseHint.None && sprite is not null;

        iconObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            if (hint is not ThreatResponseHint.None && sprite is null)
            {
                ValheimForesightPlugin.Log?.LogWarning(
                    $"Sprite is null for hint={hint}, hiding icon. "
                        + $"Check ThreatIconSpriteProvider paths."
                );
            }
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;

        var rect = iconObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(
                config.ThreatIconOffsetX.Value,
                config.ThreatIconOffsetY.Value
            );
            rect.sizeDelta = new Vector2(config.ThreatIconSize.Value, config.ThreatIconSize.Value);
        }
    }

    private GameObject? GetExistingIconObject(Transform nameTransform)
    {
        var parent = nameTransform.parent ?? nameTransform;
        var existing = parent.Find(IconObjectName);
        return existing?.gameObject;
    }

    private GameObject GetOrCreateIconObject(Transform nameTransform)
    {
        var existing = GetExistingIconObject(nameTransform);
        if (existing != null)
            return existing;

        var parent = nameTransform.parent ?? nameTransform;

        ValheimForesightPlugin.Log?.LogDebug(
            $"[{nameof(GetOrCreateIconObject)}] Creating new icon object '{IconObjectName}' "
                + $"under parent '{parent.name}'."
        );

        var go = new GameObject(IconObjectName);
        go.transform.SetParent(parent, false);
        _createdIcons.Add(go);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);

        var config = ValheimForesightPlugin.ForesightConfig;
        if (config != null)
        {
            rect.anchoredPosition = new Vector2(
                config.ThreatIconOffsetX.Value,
                config.ThreatIconOffsetY.Value
            );
            rect.sizeDelta = new Vector2(config.ThreatIconSize.Value, config.ThreatIconSize.Value);
        }

        return go;
    }

    /// <summary>
    /// Cleans up all created threat icons
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _spriteProvider?.Dispose();
        }

        foreach (var icon in _createdIcons)
        {
            if (icon != null)
                UnityEngine.Object.Destroy(icon);
        }
        _createdIcons.Clear();

        _disposed = true;
    }

    ~UnityThreatHudIconRenderer()
    {
        Dispose(false);
    }
}
