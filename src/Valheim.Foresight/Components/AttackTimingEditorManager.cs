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

    private bool _cursorStateSaved;
    private bool _prevCursorVisible;
    private CursorLockMode _prevCursorLock;

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

        _logger.LogInfo($"Initialized with toggle key: {_toggleKey}");
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

        if (Input.GetKeyDown(_toggleKey))
            _editorUI.Toggle();

        if (_editorUI.IsVisible && Input.GetKeyDown(KeyCode.Escape))
            HideUI();
    }

    private void OnGUI()
    {
        if (_editorUI?.IsVisible != true)
        {
            EnablePlayerInput();
            return;
        }

        DisablePlayerInput();

        if (!_cursorStateSaved)
        {
            _cursorStateSaved = true;
            _prevCursorVisible = Cursor.visible;
            _prevCursorLock = Cursor.lockState;
            _logger?.LogInfo("Cursor state saved");
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        GUI.depth = -10000;
        _editorUI.RenderGUI();
    }

    private void LateUpdate()
    {
        if (_editorUI?.IsVisible == true)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void DisablePlayerInput()
    {
        if (Player.m_localPlayer != null)
        {
            Player.m_localPlayer.enabled = false;
        }

        if (GameCamera.instance != null)
        {
            GameCamera.instance.enabled = false;
        }
    }

    private void EnablePlayerInput()
    {
        if (Player.m_localPlayer != null)
        {
            Player.m_localPlayer.enabled = true;
        }

        if (GameCamera.instance != null)
        {
            GameCamera.instance.enabled = true;
        }
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

        EnablePlayerInput();

        if (_cursorStateSaved)
        {
            Cursor.visible = _prevCursorVisible;
            Cursor.lockState = _prevCursorLock;
            _cursorStateSaved = false;
            _logger?.LogInfo("Cursor state restored");
        }
    }

    public void ToggleUI()
    {
        _editorUI?.Toggle();
    }
}
