using UnityEngine;
using Valheim.Foresight.Configuration;
using Valheim.Foresight.Services.Castbar.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Components;

/// <summary>
/// Manages the attack timing editor UI lifecycle and input handling
/// </summary>
public class AttackTimingEditorManager : MonoBehaviour
{
    private AttackTimingEditorUI? _editorUI;
    private ILogger? _logger;
    private IAttackTimingService? _timingService;
    private IAttackTimingDataProvider? _dataProvider;
    private IForesightConfiguration? _config;
    private KeyCode _toggleKey = KeyCode.F7;

    public static AttackTimingEditorManager? Instance { get; private set; }

    public void Initialize(
        ILogger logger,
        IAttackTimingService timingService,
        IAttackTimingDataProvider dataProvider,
        IForesightConfiguration config,
        KeyCode toggleKey = KeyCode.F7
    )
    {
        _logger = logger;
        _timingService = timingService;
        _dataProvider = dataProvider;
        _config = config;
        _toggleKey = toggleKey;

        _editorUI = new AttackTimingEditorUI(_logger, _timingService, _dataProvider, _config);

        _logger.LogInfo($"[AttackTimingEditorManager] Initialized with toggle key: {_toggleKey}");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (_editorUI == null)
            return;

        // Toggle UI with configured key
        if (Input.GetKeyDown(_toggleKey))
        {
            _editorUI.Toggle();
        }

        // Close UI with Escape key
        if (_editorUI.IsVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            _editorUI.Hide();
        }
    }

    private void OnGUI()
    {
        _editorUI?.RenderGUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowUI()
    {
        _editorUI?.Show();
    }

    public void HideUI()
    {
        _editorUI?.Hide();
    }

    public void ToggleUI()
    {
        _editorUI?.Toggle();
    }
}
