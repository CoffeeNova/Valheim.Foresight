using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valheim.Foresight.Configuration;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Castbar.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Components;

/// <summary>
/// In-game UI for viewing and editing attack timing data
/// </summary>
public class AttackTimingEditorUI
{
    private const float WindowWidth = 800f;
    private const float WindowHeight = 600f;
    private const float ScrollViewHeight = 450f;

    private readonly ILogger _logger;
    private readonly IAttackTimingService _timingService;
    private readonly IAttackTimingDataProvider _dataProvider;
    private readonly IForesightConfiguration _config;

    private bool _isVisible;
    private Rect _windowRect;
    private Vector2 _scrollPosition;
    private string _searchFilter = "";
    private List<AttackTimingEntry> _filteredEntries = new();
    private readonly Dictionary<string, string> _editingValues = new();

    private GUIStyle? _windowStyle;
    private GUIStyle? _headerStyle;
    private GUIStyle? _labelStyle;
    private GUIStyle? _buttonStyle;
    private GUIStyle? _textFieldStyle;
    private GUIStyle? _toggleStyle;
    private bool _hasInitializedPosition;

    public AttackTimingEditorUI(
        ILogger logger,
        IAttackTimingService timingService,
        IAttackTimingDataProvider dataProvider,
        IForesightConfiguration config
    )
    {
        _logger = logger;
        _timingService = timingService;
        _dataProvider = dataProvider;
        _config = config;

        // Initialize with default position, will be recentered on first show
        _windowRect = new Rect(0, 0, WindowWidth, WindowHeight);
        _hasInitializedPosition = false;
    }

    public bool IsVisible => _isVisible;

    public void Toggle()
    {
        _isVisible = !_isVisible;
        if (_isVisible)
        {
            CenterWindowOnScreen();
            RefreshData();
            _logger.LogInfo("UI opened");
        }
        else
        {
            _logger.LogInfo("UI closed");
        }
    }

    public void Show()
    {
        if (!_isVisible)
        {
            Toggle();
        }
    }

    public void Hide()
    {
        if (_isVisible)
        {
            Toggle();
        }
    }

    private void CenterWindowOnScreen()
    {
        if (!_hasInitializedPosition && Screen.width > 0 && Screen.height > 0)
        {
            _windowRect = new Rect(
                (Screen.width - WindowWidth) / 2,
                (Screen.height - WindowHeight) / 2,
                WindowWidth,
                WindowHeight
            );
            _hasInitializedPosition = true;
        }
    }

    private void OnGUI()
    {
        if (!_isVisible)
            return;

        InitializeStyles();

        _windowRect = GUILayout.Window(
            30316,
            _windowRect,
            DrawWindow,
            "Attack Timing Editor",
            _windowStyle
        );
    }

    public void RenderGUI()
    {
        OnGUI();
    }

    private void InitializeStyles()
    {
        if (_windowStyle != null)
            return;

        _windowStyle = new GUIStyle(GUI.skin.window) { padding = new RectOffset(10, 10, 20, 10) };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
        };

        _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        _textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 12 };
        _toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 12 };
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.BeginVertical();
        DrawHeader();
        GUILayout.Space(10);
        DrawScrollableContent();
        GUILayout.Space(10);
        DrawFooter();
        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, WindowWidth, 20));
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label("Search:", _labelStyle, GUILayout.Width(60));
        var newFilter = GUILayout.TextField(_searchFilter, _textFieldStyle, GUILayout.Width(300));
        if (newFilter != _searchFilter)
        {
            _searchFilter = newFilter;
            FilterEntries();
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("Global Learning:", _labelStyle, GUILayout.Width(110));
        var newLearningEnabled = GUILayout.Toggle(
            _config.AttackTimingLearningEnabled.Value,
            "",
            _toggleStyle,
            GUILayout.Width(30)
        );
        if (newLearningEnabled != _config.AttackTimingLearningEnabled.Value)
        {
            _config.AttackTimingLearningEnabled.Value = newLearningEnabled;
            _logger.LogInfo($"Global learning toggled: {newLearningEnabled}");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Refresh", _buttonStyle, GUILayout.Width(80)))
        {
            RefreshData();
        }

        if (GUILayout.Button("Close", _buttonStyle, GUILayout.Width(80)))
        {
            Hide();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawScrollableContent()
    {
        _scrollPosition = GUILayout.BeginScrollView(
            _scrollPosition,
            GUILayout.Width(WindowWidth - 20),
            GUILayout.Height(ScrollViewHeight)
        );

        if (_filteredEntries.Count == 0)
        {
            GUILayout.Label("No attack timings available", _labelStyle);
        }
        else
        {
            DrawTableHeader();

            foreach (var t in _filteredEntries)
            {
                if (DrawTimingRow(t))
                {
                    // Data was modified (e.g., reset clicked), exit early and let Unity re-render
                    break;
                }
            }
        }

        GUILayout.EndScrollView();
    }

    private void DrawTableHeader()
    {
        GUILayout.BeginHorizontal("box");

        GUILayout.Label("Creature", _headerStyle, GUILayout.Width(150));
        GUILayout.Label("Animation", _headerStyle, GUILayout.Width(150));
        GUILayout.Label("Mean Hit Time (s)", _headerStyle, GUILayout.Width(120));
        GUILayout.Label("Samples", _headerStyle, GUILayout.Width(80));
        GUILayout.Label("Learning", _headerStyle, GUILayout.Width(80));
        GUILayout.Label("Actions", _headerStyle, GUILayout.Width(150));

        GUILayout.EndHorizontal();
    }

    private bool DrawTimingRow(AttackTimingEntry entry)
    {
        var key = entry.Key.ToString();

        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();

        DrawEntryLabels(entry);
        DrawMeanHitTimeField(entry, key);
        DrawSampleCountLabel(entry);
        DrawLearningToggle(entry, key);

        var shouldBreak = DrawActionButtons(entry, key);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        return shouldBreak;
    }

    private void DrawEntryLabels(AttackTimingEntry entry)
    {
        GUILayout.Label(entry.Key.CreaturePrefab, _labelStyle, GUILayout.Width(150));
        GUILayout.Label(entry.Key.AttackAnimation, _labelStyle, GUILayout.Width(150));
    }

    private void DrawMeanHitTimeField(AttackTimingEntry entry, string key)
    {
        var editKey = key + "_mean";

        if (_editingValues.ContainsKey(editKey))
        {
            _editingValues[editKey] = GUILayout.TextField(
                _editingValues[editKey],
                _textFieldStyle,
                GUILayout.Width(120)
            );
        }
        else
        {
            GUILayout.Label(
                entry.Stats.MeanHitOffsetSeconds.ToString("F3"),
                _labelStyle,
                GUILayout.Width(120)
            );
        }
    }

    private void DrawSampleCountLabel(AttackTimingEntry entry)
    {
        GUILayout.Label(entry.Stats.SampleCount.ToString(), _labelStyle, GUILayout.Width(80));
    }

    private void DrawLearningToggle(AttackTimingEntry entry, string key)
    {
        var newLearning = GUILayout.Toggle(
            entry.Stats.LearningEnabled,
            "",
            _toggleStyle,
            GUILayout.Width(80)
        );

        if (newLearning != entry.Stats.LearningEnabled)
        {
            entry.Stats.LearningEnabled = newLearning;
            _dataProvider.UpdateTiming(entry.Key, entry.Stats);
            _logger.LogInfo($"Toggled learning for {key}: {newLearning}");
        }
    }

    private bool DrawActionButtons(AttackTimingEntry entry, string key)
    {
        var editKey = key + "_mean";

        if (_editingValues.ContainsKey(editKey))
        {
            return DrawSaveButton(entry, key, editKey);
        }

        DrawEditButton(entry, editKey);
        return DrawResetButton(entry, key, editKey);
    }

    private bool DrawSaveButton(AttackTimingEntry entry, string key, string editKey)
    {
        if (!GUILayout.Button("Save", _buttonStyle, GUILayout.Width(70)))
            return false;

        if (float.TryParse(_editingValues[editKey], out var newMean))
        {
            entry.Stats.MeanHitOffsetSeconds = newMean;
            _dataProvider.UpdateTiming(entry.Key, entry.Stats);
            _editingValues.Remove(editKey);
            _logger.LogInfo($"Updated mean for {key}: {newMean}");
        }

        return false;
    }

    private void DrawEditButton(AttackTimingEntry entry, string editKey)
    {
        if (GUILayout.Button("Edit", _buttonStyle, GUILayout.Width(70)))
        {
            _editingValues[editKey] = entry.Stats.MeanHitOffsetSeconds.ToString("F3");
        }
    }

    private bool DrawResetButton(AttackTimingEntry entry, string key, string editKey)
    {
        if (!GUILayout.Button("Reset", _buttonStyle, GUILayout.Width(70)))
            return false;

        _logger.LogInfo($"Reset clicked for {key}");
        _editingValues.Remove(editKey);
        _timingService.ResetToPrelearned(entry.Key);
        RefreshData();
        _logger.LogInfo($"Reset complete for {key}");

        return true;
    }

    private void DrawFooter()
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label($"Total Entries: {_filteredEntries.Count}", _labelStyle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Reset All to Prelearned", _buttonStyle, GUILayout.Width(180)))
        {
            ResetAllToPrelearned();
        }

        if (GUILayout.Button("Export to Log", _buttonStyle, GUILayout.Width(120)))
        {
            ExportToLog();
        }

        GUILayout.EndHorizontal();
    }

    private void RefreshData()
    {
        var allTimings = _dataProvider.GetAllTimings();
        _filteredEntries = allTimings
            .Select(kvp => new AttackTimingEntry { Key = kvp.Key, Stats = kvp.Value })
            .OrderBy(e => e.Key.CreaturePrefab)
            .ThenBy(e => e.Key.AttackAnimation)
            .ToList();

        FilterEntries();
        _logger.LogInfo($"Refreshed {_filteredEntries.Count} entries");
    }

    private void FilterEntries()
    {
        if (string.IsNullOrWhiteSpace(_searchFilter))
        {
            return;
        }

        var allTimings = _dataProvider.GetAllTimings();
        _filteredEntries = allTimings
            .Where(kvp =>
                kvp.Key.CreaturePrefab.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase)
                    >= 0
                || kvp.Key.AttackAnimation.IndexOf(
                    _searchFilter,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            .Select(kvp => new AttackTimingEntry { Key = kvp.Key, Stats = kvp.Value })
            .OrderBy(e => e.Key.CreaturePrefab)
            .ThenBy(e => e.Key.AttackAnimation)
            .ToList();
    }

    private void ExportToLog()
    {
        _logger.LogInfo("=== Attack Timing Export ===");
        foreach (var entry in _filteredEntries)
        {
            _logger.LogInfo(
                $"{entry.Key}: Mean={entry.Stats.MeanHitOffsetSeconds:F3}s, "
                    + $"Samples={entry.Stats.SampleCount}, Learning={entry.Stats.LearningEnabled}"
            );
        }
        _logger.LogInfo($"Total entries: {_filteredEntries.Count}");
    }

    private void ResetAllToPrelearned()
    {
        _logger.LogInfo("Resetting all timings to prelearned values via UI");
        _dataProvider.ResetAllToPrelearned();
        RefreshData();
        _logger.LogInfo("UI refreshed after reset");
    }

    private class AttackTimingEntry
    {
        public AttackKey Key { get; set; }
        public AttackTimingStats Stats { get; set; } = null!;
    }
}
