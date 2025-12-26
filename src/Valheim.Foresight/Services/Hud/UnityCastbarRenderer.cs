using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.Foresight.Configuration;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Castbar.Interfaces;
using Valheim.Foresight.Services.Hud.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;
using Object = UnityEngine.Object;

namespace Valheim.Foresight.Services.Hud;

/// <summary>
/// Unity-based castbar renderer for displaying enemy attack progress
/// </summary>
public sealed class UnityCastbarRenderer : IUnityCastbarRenderer
{
    private const string CastbarObjectName = "Foresight_Castbar";
    private const string FillName = "Castbar_Fill";
    private const string ParryIndicatorName = "Castbar_ParryIndicator";
    private const string AttackNameTextName = "Castbar_AttackName";
    private const string TimerTextName = "Castbar_Timer";

    private const float BorderThickness = 3f;

    private TMP_FontAsset? _cachedFont;

    private readonly List<Texture2D> _createdTextures = new();
    private readonly List<Sprite> _createdSprites = new();
    private readonly List<GameObject> _createdCastbars = new();

    private Sprite? _cachedParryActiveSprite;
    private Sprite? _cachedParryIndicatorSprite;

    private readonly ILogger _logger;
    private readonly IForesightConfiguration _config;
    private readonly IParryWindowService _parryWindowService;

    /// <summary>
    /// Creates a new castbar renderer
    /// </summary>
    public UnityCastbarRenderer(
        ILogger logger,
        IForesightConfiguration config,
        IParryWindowService parryWindowService
    )
    {
        _logger = logger;
        _config = config;
        _parryWindowService =
            parryWindowService ?? throw new ArgumentNullException(nameof(parryWindowService));
    }

    /// <inheritdoc/>
    public void RenderCastbar(Transform hudParent, ActiveAttackInfo? attackInfo)
    {
        var config = ValheimForesightPlugin.ForesightConfig;
        if (config is null || !config.AttackCastbarEnabled.Value)
        {
            var existingCastbar = hudParent.Find(CastbarObjectName);
            if (existingCastbar is not null)
                existingCastbar.gameObject.SetActive(false);
            return;
        }

        var castbarObject = GetOrCreateCastbarObject(hudParent);
        castbarObject.SetActive(true);

        UpdateCastbarSize(castbarObject);
        UpdateCastbarProgress(castbarObject, attackInfo);
    }

    private GameObject GetOrCreateCastbarObject(Transform hudParent)
    {
        var existing = hudParent.Find(CastbarObjectName);
        if (existing is not null)
            return existing.gameObject;

        return CreateCastbarObject(hudParent);
    }

    private GameObject CreateCastbarObject(Transform hudParent)
    {
        var config = ValheimForesightPlugin.ForesightConfig;

        // Main castbar container (frame only, no background)
        var castbar = new GameObject(CastbarObjectName);
        castbar.transform.SetParent(hudParent, false);

        var rect = castbar.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);

        if (config is not null)
        {
            rect.anchoredPosition = new Vector2(
                config.AttackCastbarOffsetX.Value,
                config.AttackCastbarOffsetY.Value
            );
            rect.sizeDelta = new Vector2(
                config.AttackCastbarWidth.Value,
                config.AttackCastbarHeight.Value
            );
        }
        else
        {
            rect.anchoredPosition = new Vector2(0f, -10f);
            rect.sizeDelta = new Vector2(100f, 16f);
        }

        // Create background with border (black bar with outline)
        CreateBackground(castbar.transform);

        // Create fill (separately, inside the frame)
        CreateFill(castbar.transform);

        // Parry indicator (thin line on the right)
        CreateParryIndicator(castbar.transform);

        // Text overlay
        CreateAttackNameText(castbar.transform);
        CreateTimerText(castbar.transform);

        // Track created GameObject for proper cleanup
        _createdCastbars.Add(castbar);

        return castbar;
    }

    private void CreateFill(Transform parent)
    {
        var fill = new GameObject(FillName);
        fill.transform.SetParent(parent, false);

        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(BorderThickness, 0f);

        var config = ValheimForesightPlugin.ForesightConfig;
        var width = (config?.AttackCastbarWidth.Value ?? 100f) - BorderThickness * 2;
        var height = -BorderThickness * 2;
        fillRect.sizeDelta = new Vector2(width, height);

        var fillImage = fill.AddComponent<Image>();

        // IMPORTANT: Create gradient sprite for fillAmount to work
        var fillColor = _config.CastbarFillColor.Value;
        fillImage.sprite = CreateGradientSprite(fillColor);
        fillImage.color = Color.white; // Sprite already has the color
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;
        fillImage.raycastTarget = false;
    }

    private Sprite CreateWhiteSprite()
    {
        // Create 1x1 white texture
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _createdTextures.Add(texture);

        // Create sprite from texture
        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        _createdSprites.Add(sprite);
        return sprite;
    }

    private Sprite CreateGradientSprite(Color baseColor)
    {
        // Create gradient texture (lighter at top, darker at bottom)
        var texture = new Texture2D(1, 16, TextureFormat.RGBA32, false);

        for (int y = 0; y < 16; y++)
        {
            // Gradient from darker (bottom) to brighter (top)
            float t = y / 15f; // 0 at bottom, 1 at top
            float brightness = Mathf.Lerp(0.65f, 1f, t); // Lighter at top

            var color = new Color(
                baseColor.r * brightness,
                baseColor.g * brightness,
                baseColor.b * brightness,
                baseColor.a
            );
            texture.SetPixel(0, y, color);
        }

        texture.Apply();
        _createdTextures.Add(texture);

        // Create sprite from texture
        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 16), new Vector2(0.5f, 0.5f), 1f);

        _createdSprites.Add(sprite);
        return sprite;
    }

    private Sprite CreateHollowBorderSprite()
    {
        // Create hollow border texture
        int size = 16;
        int borderPx = 1; // 1px border in texture
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        var borderColor = _config.CastbarBorderColor.Value;
        var transparent = new Color(0f, 0f, 0f, 0f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // Border only on edges
                bool isEdge =
                    x < borderPx || x >= size - borderPx || y < borderPx || y >= size - borderPx;
                texture.SetPixel(x, y, isEdge ? borderColor : transparent);
            }
        }

        texture.Apply();
        _createdTextures.Add(texture);

        // Create 9-sliced sprite for proper scaling
        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            1f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(borderPx, borderPx, borderPx, borderPx) // Border for 9-slice
        );

        _createdSprites.Add(sprite);
        return sprite;
    }

    private void CreateBackground(Transform parent)
    {
        // Border frame (single hollow rectangle with transparent border)
        var border = new GameObject("Castbar_Border");
        border.transform.SetParent(parent, false);

        var borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;

        var borderImage = border.AddComponent<Image>();
        borderImage.sprite = CreateHollowBorderSprite();
        borderImage.color = Color.white; // Color is in the sprite
        borderImage.raycastTarget = false;
        borderImage.type = Image.Type.Sliced; // Important for proper border scaling

        // Black background (inside the border)
        var background = new GameObject("Castbar_Background");
        background.transform.SetParent(parent, false);

        var bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(BorderThickness, BorderThickness);
        bgRect.offsetMax = new Vector2(-BorderThickness, -BorderThickness);

        var bgImage = background.AddComponent<Image>();
        bgImage.sprite = CreateWhiteSprite();
        var bgColor = _config.CastbarBackgroundColor.Value;
        bgImage.color = bgColor;
        bgImage.raycastTarget = false;
    }

    private void CreateParryIndicator(Transform parent)
    {
        // Visual indicator showing the parry window on the castbar
        var indicator = new GameObject(ParryIndicatorName);
        indicator.transform.SetParent(parent, false);

        var indicatorRect = indicator.AddComponent<RectTransform>();
        indicatorRect.anchorMin = Vector2.zero;
        indicatorRect.anchorMax = Vector2.one;
        indicatorRect.offsetMin = Vector2.zero;
        indicatorRect.offsetMax = Vector2.zero;
        indicatorRect.sizeDelta = Vector2.zero;

        var indicatorImage = indicator.AddComponent<Image>();
        var parryIndicatorColor = _config.CastbarParryIndicatorColor.Value;
        indicatorImage.sprite = CreateGradientSprite(parryIndicatorColor);
        indicatorImage.color = Color.white; // Sprite already has the color
        indicatorImage.raycastTarget = false;

        indicator.SetActive(false);
    }

    private void CreateAttackNameText(Transform parent)
    {
        var textObj = new GameObject(AttackNameTextName);
        textObj.transform.SetParent(parent, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(0.6f, 1f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();

        // Get font from existing Valheim UI
        var existingFont = GetValheimFont();
        if (existingFont is not null)
        {
            text.font = existingFont;
        }

        text.text = string.Empty;
        text.fontSize = 12; // Base size
        text.fontSizeMin = 6; // Minimum size
        text.fontSizeMax = 14; // Maximum size
        text.enableAutoSizing = true; // Automatic scaling
        var textColor = _config.CastbarTextColor.Value;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(4f, 0f, 2f, 0f);
        text.fontStyle = FontStyles.Normal;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis; // Truncate with ...
        text.raycastTarget = false;
        text.enabled = true;

        var shadow = textObj.AddComponent<Shadow>();
        var shadowColor = _config.CastbarTextShadowColor.Value;
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private void CreateTimerText(Transform parent)
    {
        var textObj = new GameObject(TimerTextName);
        textObj.transform.SetParent(parent, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.6f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();

        // Get font from existing Valheim UI
        var existingFont = GetValheimFont();
        if (existingFont is not null)
        {
            text.font = existingFont;
        }

        text.text = string.Empty;
        text.fontSize = 12; // Base size
        text.fontSizeMin = 6; // Minimum size
        text.fontSizeMax = 14; // Maximum size
        text.enableAutoSizing = true; // Automatic scaling
        var textColor = _config.CastbarTextColor.Value;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.margin = new Vector4(2f, 0f, 4f, 0f);
        text.fontStyle = FontStyles.Normal;
        text.raycastTarget = false;
        text.enabled = true;

        var shadow = textObj.AddComponent<Shadow>();
        var shadowColor = _config.CastbarTextShadowColor.Value;
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private void UpdateCastbarSize(GameObject castbarObject)
    {
        var width = _config.AttackCastbarWidth.Value;
        var height = _config.AttackCastbarHeight.Value;

        var rect = castbarObject.GetComponent<RectTransform>();
        if (rect is not null)
        {
            rect.anchoredPosition = new Vector2(
                _config.AttackCastbarOffsetX.Value,
                _config.AttackCastbarOffsetY.Value
            );
            rect.sizeDelta = new Vector2(width, height);
        }

        // Update Fill width
        var fillTransform = castbarObject.transform.Find(FillName);
        if (fillTransform is not null)
        {
            var fillRect = fillTransform.GetComponent<RectTransform>();
            if (fillRect is not null)
            {
                var fillWidth = width - BorderThickness * 2;
                fillRect.sizeDelta = new Vector2(fillWidth, -BorderThickness * 2);
            }
        }

        // Adaptive text size based on castbar width
        UpdateTextSizes(castbarObject, width, height);
    }

    private void UpdateTextSizes(GameObject castbarObject, float castbarWidth, float castbarHeight)
    {
        var attackNameTransform = castbarObject.transform.Find(AttackNameTextName);
        var timerTransform = castbarObject.transform.Find(TimerTextName);

        // Calculate optimal font size based on width and height
        float baseFontSize = Mathf.Clamp(castbarHeight * 0.7f, 8f, 16f);
        float minFontSize = Mathf.Max(6f, baseFontSize * 0.5f);
        float maxFontSize = Mathf.Min(18f, baseFontSize * 1.2f);

        // If castbar is very narrow, reduce text size
        if (castbarWidth < 80f)
        {
            baseFontSize = Mathf.Clamp(castbarHeight * 0.5f, 6f, 10f);
            minFontSize = 6f;
            maxFontSize = 10f;
        }

        // Update sizes for attackNameText
        if (attackNameTransform is not null)
        {
            var attackNameText = attackNameTransform.GetComponent<TextMeshProUGUI>();
            if (attackNameText is not null)
            {
                attackNameText.fontSize = baseFontSize;
                attackNameText.fontSizeMin = minFontSize;
                attackNameText.fontSizeMax = maxFontSize;

                // For very narrow castbars - hide attack name text
                if (castbarWidth < 50f)
                {
                    attackNameText.enabled = false;
                }
                else
                {
                    attackNameText.enabled = true;
                }
            }
        }

        // Update sizes for timerText
        if (timerTransform is null)
            return;

        var timerText = timerTransform.GetComponent<TextMeshProUGUI>();
        if (timerText is null)
            return;

        timerText.fontSize = baseFontSize;
        timerText.fontSizeMin = minFontSize;
        timerText.fontSizeMax = maxFontSize;

        // For very narrow castbars - hide timer
        if (castbarWidth < 30f)
        {
            timerText.enabled = false;
        }
        else
        {
            timerText.enabled = true;
        }
    }

    private void UpdateCastbarProgress(GameObject castbarObject, ActiveAttackInfo? attackInfo)
    {
        var fillTransform = castbarObject.transform.Find(FillName);
        var parryIndicatorTransform = castbarObject.transform.Find(ParryIndicatorName);
        var attackNameTransform = castbarObject.transform.Find(AttackNameTextName);
        var timerTransform = castbarObject.transform.Find(TimerTextName);

        if (fillTransform is null)
            return;

        var fillImage = fillTransform.GetComponent<Image>();
        var parryIndicator = parryIndicatorTransform?.gameObject;
        var parryIndicatorRect = parryIndicatorTransform?.GetComponent<RectTransform>();
        var parryIndicatorImage = parryIndicatorTransform?.GetComponent<Image>();
        var attackNameText = attackNameTransform?.GetComponent<TextMeshProUGUI>();
        var timerText = timerTransform?.GetComponent<TextMeshProUGUI>();

        if (fillImage is null)
            return;

        // If no active attack - everything is empty
        if (attackInfo is null || attackInfo.IsExpired)
        {
            fillImage.fillAmount = 0f;

            if (parryIndicator is not null)
                parryIndicator.SetActive(false);

            if (attackNameText is not null)
                attackNameText.text = string.Empty;

            if (timerText is not null)
                timerText.text = string.Empty;

            return;
        }

        // Set fill progress
        fillImage.fillAmount = attackInfo.Progress;

        // Update attack text
        if (attackNameText is not null)
            attackNameText.text = attackInfo.AttackName;

        // Update timer
        if (timerText is not null)
            timerText.text = $"{attackInfo.TimeRemaining:F1}s";

        var parryWindowInfo = _parryWindowService.GetParryWindowInfo(
            attackInfo,
            attackInfo.Duration
        );

        if (!parryWindowInfo.HasValue)
        {
            if (parryIndicator is not null)
            {
                parryIndicator.SetActive(false);
            }

            // Normal fill - keep yellow (sprite color)
            fillImage.color = Color.white;

            var textColor = _config.CastbarTextColor.Value;
            if (attackNameText is not null)
                attackNameText.color = textColor;
            if (timerText is not null)
                timerText.color = textColor;

            return;
        }

        // If parry window info exists - update indicator size and position
        if (parryIndicatorRect is not null)
        {
            var (startPos, windowWidth) = parryWindowInfo.Value;
            parryIndicatorRect.anchorMin = new Vector2(startPos, 0f);
            parryIndicatorRect.anchorMax = new Vector2(startPos + windowWidth, 1f);
            parryIndicatorRect.offsetMin = new Vector2(0f, BorderThickness);
            parryIndicatorRect.offsetMax = new Vector2(0f, -BorderThickness);
        }

        // Visual effects for parry window
        if (_parryWindowService.IsInParryWindow(attackInfo))
        {
            fillImage.color = Color.white;
            var textColor = _config.CastbarTextColor.Value;
            if (attackNameText is not null)
                attackNameText.color = textColor;
            if (timerText is not null)
                timerText.color = textColor;

            // During parry window show indicator with red gradient
            if (parryIndicator is not null)
            {
                parryIndicator.SetActive(true);
                if (parryIndicatorImage is not null)
                {
                    if (_cachedParryActiveSprite is null)
                    {
                        var parryActiveColor = _config.CastbarParryActiveColor.Value;
                        _cachedParryActiveSprite = CreateGradientSprite(parryActiveColor);
                    }
                    parryIndicatorImage.sprite = _cachedParryActiveSprite;
                    parryIndicatorImage.color = Color.white;
                }
            }
        }
        else
        {
            // Show parry indicator with orange gradient
            if (parryIndicator is not null)
            {
                parryIndicator.SetActive(true);
                if (parryIndicatorImage is not null)
                {
                    if (_cachedParryIndicatorSprite is null)
                    {
                        var parryIndicatorColor = _config.CastbarParryIndicatorColor.Value;
                        _cachedParryIndicatorSprite = CreateGradientSprite(parryIndicatorColor);
                    }
                    parryIndicatorImage.sprite = _cachedParryIndicatorSprite;
                    parryIndicatorImage.color = Color.white;
                }
            }

            fillImage.color = Color.white;

            var textColor = _config.CastbarTextColor.Value;
            if (attackNameText is not null)
                attackNameText.color = textColor;
            if (timerText is not null)
                timerText.color = textColor;
        }
    }

    private TMP_FontAsset? GetValheimFont()
    {
        if (_cachedFont is not null)
            return _cachedFont;

        try
        {
            // Try to find font from EnemyHud
            var enemyHud = EnemyHud.instance;
            if (enemyHud is not null)
            {
                var hudElements = enemyHud.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (hudElements.Length > 0)
                {
                    _cachedFont = hudElements[0].font;
                    ValheimForesightPlugin.Log.LogInfo(
                        $"Found and cached Valheim font: {_cachedFont?.name}"
                    );
                    return _cachedFont;
                }
            }

            // Alternative: search for any TextMeshProUGUI in the scene
            var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            if (allTexts.Length > 0)
            {
                _cachedFont = allTexts[0].font;
                ValheimForesightPlugin.Log.LogInfo(
                    $"Found and cached font from scene: {_cachedFont?.name}"
                );
                return _cachedFont;
            }

            ValheimForesightPlugin.Log.LogWarning("Could not find Valheim font!");
        }
        catch (Exception ex)
        {
            ValheimForesightPlugin.Log.LogError($"Error getting Valheim font: {ex.Message}");
        }

        return null;
    }

    public void Dispose()
    {
        CleanupCastbars();

        foreach (var sprite in _createdSprites)
        {
            if (sprite is not null)
                Object.Destroy(sprite);
        }
        _createdSprites.Clear();

        foreach (var texture in _createdTextures)
        {
            if (texture is not null)
                Object.Destroy(texture);
        }
        _createdTextures.Clear();

        _cachedFont = null;
        _cachedParryActiveSprite = null;
        _cachedParryIndicatorSprite = null;
    }

    private void CleanupCastbars()
    {
        try
        {
            // Use tracked list instead of FindObjectsByType
            foreach (var castbar in _createdCastbars)
            {
                if (castbar is not null)
                {
                    _logger?.LogDebug(
                        $"[{nameof(CleanupCastbars)}] Destroying castbar: {castbar.name}"
                    );
                    Object.Destroy(castbar);
                }
            }

            _logger?.LogInfo(
                $"[{nameof(CleanupCastbars)}] Cleaned up {_createdCastbars.Count} castbar(s)"
            );

            _createdCastbars.Clear();
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                $"[{nameof(CleanupCastbars)}] Error cleaning up castbars: {ex.Message}"
            );
        }
    }
}
