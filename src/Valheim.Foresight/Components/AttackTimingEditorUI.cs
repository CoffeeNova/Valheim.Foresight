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

        _windowRect = new Rect(
            (Screen.width - WindowWidth) / 2,
            (Screen.height - WindowHeight) / 2,
            WindowWidth,
            WindowHeight
        );
    }

    public bool IsVisible => _isVisible;

    public void Toggle()
    {
        _isVisible = !_isVisible;
        if (_isVisible)
        {
            RefreshData();
            _logger.LogInfo("[AttackTimingEditorUI] UI opened");
        }
        else
        {
            _logger.LogInfo("[AttackTimingEditorUI] UI closed");
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
            _logger.LogInfo(
                $"[AttackTimingEditorUI] Global learning toggled: {newLearningEnabled}"
            );
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
        GUILayout.Label(entry.Key.CreaturePrefab, _labelStyle, GUILayout.Width(150));
        GUILayout.Label(entry.Key.AttackAnimation, _labelStyle, GUILayout.Width(150));

        if (_editingValues.ContainsKey(key + "_mean"))
        {
            _editingValues[key + "_mean"] = GUILayout.TextField(
                _editingValues[key + "_mean"],
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

        GUILayout.Label(entry.Stats.SampleCount.ToString(), _labelStyle, GUILayout.Width(80));

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
            _logger.LogInfo($"[AttackTimingEditorUI] Toggled learning for {key}: {newLearning}");
        }

        if (_editingValues.ContainsKey(key + "_mean"))
        {
            if (GUILayout.Button("Save", _buttonStyle, GUILayout.Width(70)))
            {
                if (float.TryParse(_editingValues[key + "_mean"], out var newMean))
                {
                    entry.Stats.MeanHitOffsetSeconds = newMean;
                    _dataProvider.UpdateTiming(entry.Key, entry.Stats);
                    _editingValues.Remove(key + "_mean");
                    _logger.LogInfo($"[AttackTimingEditorUI] Updated mean for {key}: {newMean}");
                }
            }
        }
        else
        {
            if (GUILayout.Button("Edit", _buttonStyle, GUILayout.Width(70)))
            {
                _editingValues[key + "_mean"] = entry.Stats.MeanHitOffsetSeconds.ToString("F3");
            }
        }

        if (GUILayout.Button("Reset", _buttonStyle, GUILayout.Width(70)))
        {
            _logger.LogInfo($"[AttackTimingEditorUI] Reset clicked for {key}");

            _editingValues.Remove(key + "_mean");
            _timingService.ResetToPrelearned(entry.Key);
            RefreshData();

            _logger.LogInfo($"[AttackTimingEditorUI] Reset complete for {key}");

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            return true;
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        // Return false to indicate no data change
        return false;
    }

    private void DrawFooter()
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label($"Total Entries: {_filteredEntries.Count}", _labelStyle);
        GUILayout.FlexibleSpace();

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
        _logger.LogInfo($"[AttackTimingEditorUI] Refreshed {_filteredEntries.Count} entries");
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
        _logger.LogInfo("[AttackTimingEditorUI] === Attack Timing Export ===");
        foreach (var entry in _filteredEntries)
        {
            _logger.LogInfo(
                $"{entry.Key}: Mean={entry.Stats.MeanHitOffsetSeconds:F3}s, "
                    + $"Samples={entry.Stats.SampleCount}, Learning={entry.Stats.LearningEnabled}"
            );
        }
        _logger.LogInfo($"[AttackTimingEditorUI] Total entries: {_filteredEntries.Count}");
    }

    private class AttackTimingEntry
    {
        public AttackKey Key { get; set; }
        public AttackTimingStats Stats { get; set; } = null!;
    }
}
