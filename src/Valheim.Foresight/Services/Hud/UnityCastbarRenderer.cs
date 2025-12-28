using System;
using System.Collections.Generic;
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
    const float OffestLevelFactorY = -11.0f;

    private TMP_FontAsset? _cachedFont;

    private readonly List<Texture2D> _createdTextures = new();
    private readonly List<Sprite> _createdSprites = new();
    private readonly List<GameObject>? _createdCastbars = new();

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
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _parryWindowService =
            parryWindowService ?? throw new ArgumentNullException(nameof(parryWindowService));

        _config.SettingsChanged += OnConfigurationChanged;
    }

    /// <inheritdoc/>
    public void RenderCastbar(
        Transform hudParent,
        ActiveAttackInfo? attackInfo,
        Character? character = null
    )
    {
        var config = ValheimForesightPlugin.ForesightConfig;
        if (config is null || !config.AttackCastbarEnabled.Value)
        {
            var existingCastbar = hudParent.Find(CastbarObjectName);
            if (existingCastbar is not null)
                existingCastbar.gameObject.SetActive(false);
            return;
        }

        bool shouldShow =
            config.DebugEnabled.Value
            || config.AlwaysDisplayCastbar.Value
            || (attackInfo is not null && !attackInfo.IsExpired);

        var isBoss = character != null && character.IsBoss();
        var castbarObject = GetOrCreateCastbarObject(hudParent, isBoss, character);
        castbarObject.SetActive(shouldShow);

        if (shouldShow)
        {
            UpdateCastbarSize(castbarObject, isBoss, character);
            UpdateCastbarProgress(castbarObject, attackInfo);
        }
    }

    private GameObject GetOrCreateCastbarObject(
        Transform hudParent,
        bool isBoss,
        Character? character = null
    )
    {
        var existing = hudParent.Find(CastbarObjectName);
        if (existing is not null)
            return existing.gameObject;

        return CreateCastbarObject(hudParent, isBoss, character);
    }

    private GameObject CreateCastbarObject(
        Transform hudParent,
        bool isBoss,
        Character? character = null
    )
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
            if (isBoss)
            {
                rect.anchoredPosition = new Vector2(
                    config.BossCastbarOffsetX.Value,
                    config.BossCastbarOffsetY.Value
                );
                rect.sizeDelta = new Vector2(
                    config.BossCastbarWidth.Value,
                    config.BossCastbarHeight.Value
                );
            }
            else
            {
                var offsetY = config.AttackCastbarOffsetY.Value;
                if (character != null && character.GetLevel() > 1)
                {
                    offsetY += OffestLevelFactorY;
                }

                rect.anchoredPosition = new Vector2(config.AttackCastbarOffsetX.Value, offsetY);
                rect.sizeDelta = new Vector2(
                    config.AttackCastbarWidth.Value,
                    config.AttackCastbarHeight.Value
                );
            }
        }
        else
        {
            rect.anchoredPosition = new Vector2(0f, -10f);
            rect.sizeDelta = new Vector2(100f, 16f);
        }

        CreateBackground(castbar.transform);
        CreateFill(castbar.transform);
        CreateParryIndicator(castbar.transform);
        CreateAttackNameText(castbar.transform);
        CreateTimerText(castbar.transform);

        _createdCastbars!.Add(castbar);

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

        var fillColor = _config.CastbarFillColor.Value;
        fillImage.sprite = CreateGradientSprite(fillColor);
        fillImage.color = Color.white;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;
        fillImage.raycastTarget = false;
    }

    private Sprite CreateWhiteSprite()
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _createdTextures.Add(texture);

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

        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 16), new Vector2(0.5f, 0.5f), 1f);

        _createdSprites.Add(sprite);
        return sprite;
    }

    private Sprite CreateHollowBorderSprite()
    {
        int size = 16;
        int borderPx = 1;
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
            new Vector4(borderPx, borderPx, borderPx, borderPx)
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
        borderImage.color = Color.white;
        borderImage.raycastTarget = false;
        borderImage.type = Image.Type.Sliced;

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

    // Visual indicator showing the parry window on the castbar
    private void CreateParryIndicator(Transform parent)
    {
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
        indicatorImage.color = Color.white;
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

        var existingFont = GetValheimFont();
        if (existingFont is not null)
        {
            text.font = existingFont;
        }

        text.text = string.Empty;
        text.fontSize = 12;
        text.fontSizeMin = 6;
        text.fontSizeMax = 14;
        text.enableAutoSizing = true;
        var textColor = _config.CastbarTextColor.Value;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(4f, 0f, 2f, 0f);
        text.fontStyle = FontStyles.Normal;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.enabled = _config.AttackCastbarTextEnabled.Value;

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

        var existingFont = GetValheimFont();
        if (existingFont is not null)
        {
            text.font = existingFont;
        }

        text.text = string.Empty;
        text.fontSize = 12;
        text.fontSizeMin = 6;
        text.fontSizeMax = 14;
        text.enableAutoSizing = true;
        var textColor = _config.CastbarTextColor.Value;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.margin = new Vector4(2f, 0f, 4f, 0f);
        text.fontStyle = FontStyles.Normal;
        text.raycastTarget = false;
        text.enabled = _config.AttackCastbarTextEnabled.Value;

        var shadow = textObj.AddComponent<Shadow>();
        var shadowColor = _config.CastbarTextShadowColor.Value;
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private void UpdateCastbarSize(
        GameObject castbarObject,
        bool isBoss,
        Character? character = null
    )
    {
        var width = isBoss ? _config.BossCastbarWidth.Value : _config.AttackCastbarWidth.Value;
        var height = isBoss ? _config.BossCastbarHeight.Value : _config.AttackCastbarHeight.Value;

        var rect = castbarObject.GetComponent<RectTransform>();
        if (rect is not null)
        {
            var offsetX = isBoss
                ? _config.BossCastbarOffsetX.Value
                : _config.AttackCastbarOffsetX.Value;
            var offsetY = isBoss
                ? _config.BossCastbarOffsetY.Value
                : _config.AttackCastbarOffsetY.Value;

            if (!isBoss && character != null && character.GetLevel() > 1)
            {
                offsetY += OffestLevelFactorY;
            }

            rect.anchoredPosition = new Vector2(offsetX, offsetY);
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

        UpdateTextSizes(castbarObject, width, height);
    }

    private void UpdateTextSizes(GameObject castbarObject, float castbarWidth, float castbarHeight)
    {
        var attackNameTransform = castbarObject.transform.Find(AttackNameTextName);
        var timerTransform = castbarObject.transform.Find(TimerTextName);

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

        if (timerTransform is null)
            return;

        var timerText = timerTransform.GetComponent<TextMeshProUGUI>();
        if (timerText is null)
            return;

        timerText.fontSize = baseFontSize;
        timerText.fontSizeMin = minFontSize;
        timerText.fontSizeMax = maxFontSize;

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
        var components = GetCastbarComponents(castbarObject);
        if (components.FillImage is null)
            return;

        if (attackInfo is null || attackInfo.IsExpired)
        {
            ResetCastbarDisplay(components);
            return;
        }

        UpdateActiveAttackDisplay(components, attackInfo);
    }

    private CastbarComponents GetCastbarComponents(GameObject castbarObject)
    {
        var fillTransform = castbarObject.transform.Find(FillName);
        var parryIndicatorTransform = castbarObject.transform.Find(ParryIndicatorName);
        var attackNameTransform = castbarObject.transform.Find(AttackNameTextName);
        var timerTransform = castbarObject.transform.Find(TimerTextName);

        return new CastbarComponents
        {
            FillImage = fillTransform?.GetComponent<Image>(),
            ParryIndicator = parryIndicatorTransform?.gameObject,
            ParryIndicatorRect = parryIndicatorTransform?.GetComponent<RectTransform>(),
            ParryIndicatorImage = parryIndicatorTransform?.GetComponent<Image>(),
            AttackNameText = attackNameTransform?.GetComponent<TextMeshProUGUI>(),
            TimerText = timerTransform?.GetComponent<TextMeshProUGUI>(),
        };
    }

    private void ResetCastbarDisplay(CastbarComponents components)
    {
        if (components.FillImage is not null)
            components.FillImage.fillAmount = 0f;

        if (components.ParryIndicator is not null)
            components.ParryIndicator.SetActive(false);

        if (components.AttackNameText is not null)
            components.AttackNameText.text = string.Empty;

        if (components.TimerText is not null)
            components.TimerText.text = string.Empty;
    }

    private void UpdateActiveAttackDisplay(
        CastbarComponents components,
        ActiveAttackInfo attackInfo
    )
    {
        components.FillImage!.fillAmount = attackInfo.Progress;

        UpdateAttackText(components, attackInfo);

        var parryWindowInfo = _parryWindowService.GetParryWindowInfo(
            attackInfo,
            attackInfo.Duration
        );

        if (!parryWindowInfo.HasValue)
        {
            ApplyNoParryWindowStyle(components);
            return;
        }

        UpdateParryIndicatorPosition(components, parryWindowInfo.Value);
        ApplyParryWindowStyle(components, attackInfo);
    }

    private void UpdateAttackText(CastbarComponents components, ActiveAttackInfo attackInfo)
    {
        var textEnabled = _config.AttackCastbarTextEnabled.Value;

        if (components.AttackNameText is not null)
        {
            components.AttackNameText.text = textEnabled ? attackInfo.AttackName : string.Empty;
            components.AttackNameText.enabled = textEnabled;
        }

        if (components.TimerText is not null)
        {
            components.TimerText.text = textEnabled
                ? $"{attackInfo.TimeRemaining:F1}s"
                : string.Empty;
            components.TimerText.enabled = textEnabled;
        }
    }

    private void ApplyNoParryWindowStyle(CastbarComponents components)
    {
        if (components.ParryIndicator is not null)
            components.ParryIndicator.SetActive(false);

        if (components.FillImage is not null)
            components.FillImage.color = Color.white;

        var textColor = _config.CastbarTextColor.Value;
        if (components.AttackNameText is not null)
            components.AttackNameText.color = textColor;
        if (components.TimerText is not null)
            components.TimerText.color = textColor;
    }

    private void UpdateParryIndicatorPosition(
        CastbarComponents components,
        (float startPos, float windowWidth) parryWindowInfo
    )
    {
        if (components.ParryIndicatorRect is null)
            return;

        var (startPos, windowWidth) = parryWindowInfo;
        components.ParryIndicatorRect.anchorMin = new Vector2(startPos, 0f);
        components.ParryIndicatorRect.anchorMax = new Vector2(startPos + windowWidth, 1f);
        components.ParryIndicatorRect.offsetMin = new Vector2(0f, BorderThickness);
        components.ParryIndicatorRect.offsetMax = new Vector2(0f, -BorderThickness);
    }

    private void ApplyParryWindowStyle(CastbarComponents components, ActiveAttackInfo attackInfo)
    {
        var isInParryWindow = _parryWindowService.IsInParryWindow(attackInfo);

        if (components.FillImage is not null)
            components.FillImage.color = Color.white;

        var textColor = _config.CastbarTextColor.Value;
        if (components.AttackNameText is not null)
            components.AttackNameText.color = textColor;
        if (components.TimerText is not null)
            components.TimerText.color = textColor;

        UpdateParryIndicatorVisual(components, isInParryWindow);
    }

    private void UpdateParryIndicatorVisual(CastbarComponents components, bool isInParryWindow)
    {
        if (components.ParryIndicator is null)
            return;

        components.ParryIndicator.SetActive(true);

        if (components.ParryIndicatorImage is null)
            return;

        if (isInParryWindow)
        {
            if (_cachedParryActiveSprite is null)
            {
                var parryActiveColor = _config.CastbarParryActiveColor.Value;
                _cachedParryActiveSprite = CreateGradientSprite(parryActiveColor);
            }
            components.ParryIndicatorImage.sprite = _cachedParryActiveSprite;
        }
        else
        {
            if (_cachedParryIndicatorSprite is null)
            {
                var parryIndicatorColor = _config.CastbarParryIndicatorColor.Value;
                _cachedParryIndicatorSprite = CreateGradientSprite(parryIndicatorColor);
            }
            components.ParryIndicatorImage.sprite = _cachedParryIndicatorSprite;
        }

        components.ParryIndicatorImage.color = Color.white;
    }

    private struct CastbarComponents
    {
        public Image? FillImage { get; set; }
        public GameObject? ParryIndicator { get; set; }
        public RectTransform? ParryIndicatorRect { get; set; }
        public Image? ParryIndicatorImage { get; set; }
        public TextMeshProUGUI? AttackNameText { get; set; }
        public TextMeshProUGUI? TimerText { get; set; }
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

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        InvalidateColorCache();
        UpdateExistingCastbarColors();
    }

    private void InvalidateColorCache()
    {
        if (_cachedParryActiveSprite is not null)
        {
            _createdSprites.Remove(_cachedParryActiveSprite);
            Object.Destroy(_cachedParryActiveSprite);
            _cachedParryActiveSprite = null;
        }

        if (_cachedParryIndicatorSprite is not null)
        {
            _createdSprites.Remove(_cachedParryIndicatorSprite);
            Object.Destroy(_cachedParryIndicatorSprite);
            _cachedParryIndicatorSprite = null;
        }
    }

    private void UpdateExistingCastbarColors()
    {
        if (_createdCastbars is null)
            return;

        foreach (var castbar in _createdCastbars)
        {
            if (castbar is null || castbar == null)
                continue;

            var fillTransform = castbar.transform.Find(FillName);
            if (fillTransform is not null)
            {
                var fillImage = fillTransform.GetComponent<Image>();
                if (fillImage is not null)
                {
                    if (fillImage.sprite is not null)
                    {
                        _createdSprites.Remove(fillImage.sprite);
                        Object.Destroy(fillImage.sprite);
                    }

                    var fillColor = _config.CastbarFillColor.Value;
                    fillImage.sprite = CreateGradientSprite(fillColor);
                    fillImage.color = Color.white;
                }
            }

            var borderTransform = castbar.transform.Find("Castbar_Border");
            if (borderTransform is not null)
            {
                var borderImage = borderTransform.GetComponent<Image>();
                if (borderImage is not null)
                {
                    if (borderImage.sprite is not null)
                    {
                        _createdSprites.Remove(borderImage.sprite);
                        Object.Destroy(borderImage.sprite);
                    }

                    borderImage.sprite = CreateHollowBorderSprite();
                    borderImage.color = Color.white;
                }
            }

            var backgroundTransform = castbar.transform.Find("Castbar_Background");
            if (backgroundTransform is not null)
            {
                var bgImage = backgroundTransform.GetComponent<Image>();
                if (bgImage is not null)
                {
                    var bgColor = _config.CastbarBackgroundColor.Value;
                    bgImage.color = bgColor;
                }
            }

            var parryIndicatorTransform = castbar.transform.Find(ParryIndicatorName);
            if (parryIndicatorTransform is not null)
            {
                var parryImage = parryIndicatorTransform.GetComponent<Image>();
                if (parryImage is not null)
                {
                    if (parryImage.sprite is not null)
                    {
                        _createdSprites.Remove(parryImage.sprite);
                        Object.Destroy(parryImage.sprite);
                    }
                    var parryIndicatorColor = _config.CastbarParryIndicatorColor.Value;
                    parryImage.sprite = CreateGradientSprite(parryIndicatorColor);
                    parryImage.color = Color.white;
                }
            }

            var attackNameTransform = castbar.transform.Find(AttackNameTextName);
            if (attackNameTransform is not null)
            {
                var attackNameText = attackNameTransform.GetComponent<TextMeshProUGUI>();
                if (attackNameText is not null)
                {
                    var textColor = _config.CastbarTextColor.Value;
                    attackNameText.color = textColor;
                }

                var shadow = attackNameTransform.GetComponent<Shadow>();
                if (shadow is not null)
                {
                    var shadowColor = _config.CastbarTextShadowColor.Value;
                    shadow.effectColor = shadowColor;
                }
            }

            var timerTransform = castbar.transform.Find(TimerTextName);
            if (timerTransform is not null)
            {
                var timerText = timerTransform.GetComponent<TextMeshProUGUI>();
                if (timerText is not null)
                {
                    var textColor = _config.CastbarTextColor.Value;
                    timerText.color = textColor;
                }

                var shadow = timerTransform.GetComponent<Shadow>();
                if (shadow is not null)
                {
                    var shadowColor = _config.CastbarTextShadowColor.Value;
                    shadow.effectColor = shadowColor;
                }
            }
        }
    }

    public void Dispose()
    {
        _config.SettingsChanged -= OnConfigurationChanged;

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
            if (_createdCastbars is null)
                return;

            foreach (var castbar in _createdCastbars)
            {
                if (castbar != null && !castbar.Equals(null))
                {
                    _logger?.LogDebug(
                        $"[{nameof(CleanupCastbars)}] Destroying castbar: {castbar.name}"
                    );
                    Object.Destroy(castbar);
                }
            }

            _logger?.LogInfo($"Cleaned up {_createdCastbars.Count} castbar(s)");

            _createdCastbars?.Clear();
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                $"[{nameof(CleanupCastbars)}] Error cleaning up castbars: {ex.Message}"
            );
        }
    }
}
