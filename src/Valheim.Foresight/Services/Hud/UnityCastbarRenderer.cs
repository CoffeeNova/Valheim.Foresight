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
    private const string BorderTopName = "Castbar_Border_Top";
    private const string BorderBottomName = "Castbar_Border_Bottom";
    private const string BorderLeftName = "Castbar_Border_Left";
    private const string BorderRightName = "Castbar_Border_Right";
    private const string FillName = "Castbar_Fill";
    private const string ParryIndicatorName = "Castbar_ParryIndicator";
    private const string AttackNameTextName = "Castbar_AttackName";
    private const string TimerTextName = "Castbar_Timer";

    private const float BorderThickness = 1f;

    private TMP_FontAsset? _cachedFont;

    private readonly List<Texture2D> _createdTextures = new();
    private readonly List<Sprite> _createdSprites = new();

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
        if (config == null || !config.AttackCastbarEnabled.Value)
        {
            var existingCastbar = hudParent.Find(CastbarObjectName);
            if (existingCastbar != null)
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
        if (existing != null)
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

        if (config != null)
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

        // Create border frame
        CreateBorderLines(castbar.transform);

        // Create fill (separately, inside the frame)
        CreateFill(castbar.transform);

        // Parry indicator (thin line on the right)
        CreateParryIndicator(castbar.transform);

        // Text overlay
        CreateAttackNameText(castbar.transform);
        CreateTimerText(castbar.transform);

        return castbar;
    }

    private void CreateBorderLines(Transform parent)
    {
        var borderColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

        // Top border
        var top = new GameObject(BorderTopName);
        top.transform.SetParent(parent, false);
        var topRect = top.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(0f, BorderThickness);
        var topImg = top.AddComponent<Image>();
        topImg.color = borderColor;
        topImg.raycastTarget = false;

        // Bottom border
        var bottom = new GameObject(BorderBottomName);
        bottom.transform.SetParent(parent, false);
        var bottomRect = bottom.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.sizeDelta = new Vector2(0f, BorderThickness);
        var bottomImg = bottom.AddComponent<Image>();
        bottomImg.color = borderColor;
        bottomImg.raycastTarget = false;

        // Left border
        var left = new GameObject(BorderLeftName);
        left.transform.SetParent(parent, false);
        var leftRect = left.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0f, 1f);
        leftRect.pivot = new Vector2(0f, 0.5f);
        leftRect.anchoredPosition = Vector2.zero;
        leftRect.sizeDelta = new Vector2(BorderThickness, 0f);
        var leftImg = left.AddComponent<Image>();
        leftImg.color = borderColor;
        leftImg.raycastTarget = false;

        // Right border
        var right = new GameObject(BorderRightName);
        right.transform.SetParent(parent, false);
        var rightRect = right.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.pivot = new Vector2(1f, 0.5f);
        rightRect.anchoredPosition = Vector2.zero;
        rightRect.sizeDelta = new Vector2(BorderThickness, 0f);
        var rightImg = right.AddComponent<Image>();
        rightImg.color = borderColor;
        rightImg.raycastTarget = false;
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

        // IMPORTANT: Create white sprite for fillAmount to work
        fillImage.sprite = CreateWhiteSprite();
        fillImage.color = new Color(1f, 0.5f, 0.2f, 0.85f);
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
        indicatorImage.color = new Color(0.3f, 1f, 0.3f, 0.4f);
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
        if (existingFont != null)
        {
            text.font = existingFont;
        }

        text.text = string.Empty;
        text.fontSize = 12; // Base size
        text.fontSizeMin = 6; // Minimum size
        text.fontSizeMax = 14; // Maximum size
        text.enableAutoSizing = true; // Automatic scaling
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(4f, 0f, 2f, 0f);
        text.fontStyle = FontStyles.Normal;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis; // Truncate with ...
        text.raycastTarget = false;
        text.enabled = true;

        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
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
        if (existingFont != null)
        {
            text.font = existingFont;
        }

        text.text = string.Empty;
        text.fontSize = 12; // Base size
        text.fontSizeMin = 6; // Minimum size
        text.fontSizeMax = 14; // Maximum size
        text.enableAutoSizing = true; // Automatic scaling
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.margin = new Vector4(2f, 0f, 4f, 0f);
        text.fontStyle = FontStyles.Normal;
        text.raycastTarget = false;
        text.enabled = true;

        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private void UpdateCastbarSize(GameObject castbarObject)
    {
        var width = _config.AttackCastbarWidth.Value;
        var height = _config.AttackCastbarHeight.Value;

        var rect = castbarObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(
                _config.AttackCastbarOffsetX.Value,
                _config.AttackCastbarOffsetY.Value
            );
            rect.sizeDelta = new Vector2(width, height);
        }

        // Update Fill width
        var fillTransform = castbarObject.transform.Find(FillName);
        if (fillTransform != null)
        {
            var fillRect = fillTransform.GetComponent<RectTransform>();
            if (fillRect != null)
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
        if (attackNameTransform != null)
        {
            var attackNameText = attackNameTransform.GetComponent<TextMeshProUGUI>();
            if (attackNameText != null)
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
        if (timerTransform != null)
        {
            var timerText = timerTransform.GetComponent<TextMeshProUGUI>();
            if (timerText != null)
            {
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
        }
    }

    private void UpdateCastbarProgress(GameObject castbarObject, ActiveAttackInfo? attackInfo)
    {
        var fillTransform = castbarObject.transform.Find(FillName);
        var parryIndicatorTransform = castbarObject.transform.Find(ParryIndicatorName);
        var attackNameTransform = castbarObject.transform.Find(AttackNameTextName);
        var timerTransform = castbarObject.transform.Find(TimerTextName);

        if (fillTransform == null)
            return;

        var fillImage = fillTransform.GetComponent<Image>();
        var parryIndicator = parryIndicatorTransform?.gameObject;
        var parryIndicatorRect = parryIndicatorTransform?.GetComponent<RectTransform>();
        var parryIndicatorImage = parryIndicatorTransform?.GetComponent<Image>();
        var attackNameText = attackNameTransform?.GetComponent<TextMeshProUGUI>();
        var timerText = timerTransform?.GetComponent<TextMeshProUGUI>();

        if (fillImage == null)
            return;

        // If no active attack - everything is empty
        if (attackInfo == null || attackInfo.IsExpired)
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
        if (attackNameText != null)
            attackNameText.text = attackInfo.AttackName;

        // Update timer
        if (timerText != null)
            timerText.text = $"{attackInfo.TimeRemaining:F1}s";

        var parryWindowInfo = _parryWindowService.GetParryWindowInfo(
            attackInfo,
            attackInfo.Duration
        );

        if (!parryWindowInfo.HasValue)
        {
            if (parryIndicator != null)
            {
                parryIndicator.SetActive(false);
            }

            // Normal fill color (no effects)
            fillImage.color = new Color(1f, 0.5f, 0.2f, 0.85f);

            if (attackNameText != null)
                attackNameText.color = Color.white;
            if (timerText != null)
                timerText.color = Color.white;

            return;
        }

        // If parry window info exists - update indicator size and position
        if (parryIndicatorRect != null)
        {
            var (startPos, windowWidth) = parryWindowInfo.Value;
            parryIndicatorRect.anchorMin = new Vector2(startPos, 0f);
            parryIndicatorRect.anchorMax = new Vector2(startPos + windowWidth, 1f);
            parryIndicatorRect.offsetMin = Vector2.zero;
            parryIndicatorRect.offsetMax = Vector2.zero;
        }

        // Visual effects for parry window
        if (_parryWindowService.IsInParryWindow(attackInfo))
        {
            float pulse = Mathf.PingPong(Time.time * 6f, 1f);
            fillImage.color = Color.Lerp(
                new Color(1f, 0.8f, 0.2f, 0.85f),
                new Color(0.3f, 1f, 0.3f, 0.9f),
                pulse
            );

            if (attackNameText != null)
                attackNameText.color = Color.Lerp(Color.white, Color.green, pulse);
            if (timerText != null)
                timerText.color = Color.Lerp(Color.white, Color.green, pulse);

            // During parry window show indicator with highlight
            if (parryIndicator != null)
                parryIndicator.SetActive(true);
        }
        else
        {
            // Show parry indicator
            if (parryIndicator != null)
            {
                parryIndicator.SetActive(true);
                if (parryIndicatorImage != null)
                {
                    float blink = Mathf.PingPong(Time.time * 10f, 1f);
                    parryIndicatorImage.color = Color.Lerp(
                        new Color(0.3f, 1f, 0.3f, 0.5f),
                        new Color(0.3f, 1f, 0.3f, 1f),
                        blink
                    );
                }
            }

            fillImage.color = new Color(1f, 0.5f, 0.2f, 0.85f);

            if (attackNameText != null)
                attackNameText.color = Color.white;
            if (timerText != null)
                timerText.color = Color.white;
        }
    }

    private TMP_FontAsset? GetValheimFont()
    {
        if (_cachedFont != null)
            return _cachedFont;

        try
        {
            // Try to find font from EnemyHud
            var enemyHud = EnemyHud.instance;
            if (enemyHud != null)
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

        _cachedFont = null;
    }

    private void CleanupCastbars()
    {
        try
        {
            var allCastbars = Object
                .FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name == "Foresight_Castbar")
                .ToArray();

            foreach (var castbar in allCastbars)
            {
                if (castbar != null)
                {
                    _logger?.LogDebug(
                        $"[{nameof(CleanupCastbars)}] Destroying castbar: {castbar.name}"
                    );
                    Object.Destroy(castbar);
                }
            }

            _logger?.LogInfo(
                $"[{nameof(CleanupCastbars)}] Cleaned up {allCastbars.Length} castbar(s)"
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                $"[{nameof(CleanupCastbars)}] Error cleaning up castbars: {ex.Message}"
            );
        }
    }
}
