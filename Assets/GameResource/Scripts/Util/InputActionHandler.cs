using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputAction 이벤트 구독을 관리하는 핸들러 클래스
/// 여러 액션 맵과 액션을 등록하고 관리할 수 있습니다.
/// </summary>
public class InputActionHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    
    private readonly Dictionary<string, InputAction> _registeredActions = new Dictionary<string, InputAction>();
    private readonly Dictionary<string, Action<InputAction.CallbackContext>> _callbacks = new Dictionary<string, Action<InputAction.CallbackContext>>();

    private void OnEnable()
    {
        EnableAllActions();
    }

    private void OnDisable()
    {
        DisableAllActions();
    }

    /// <summary>
    /// 액션을 등록하고 콜백을 연결합니다.
    /// </summary>
    /// <param name="actionMapName">액션 맵 이름</param>
    /// <param name="actionName">액션 이름</param>
    /// <param name="onPerformed">액션 수행 시 호출될 콜백</param>
    /// <param name="onStarted">액션 시작 시 호출될 콜백 (선택사항)</param>
    /// <param name="onCanceled">액션 취소 시 호출될 콜백 (선택사항)</param>
    public void RegisterAction(
        string actionMapName, 
        string actionName, 
        Action<InputAction.CallbackContext> onPerformed,
        Action<InputAction.CallbackContext> onStarted = null,
        Action<InputAction.CallbackContext> onCanceled = null)
    {
        if (_inputActions == null)
        {
            Debug.LogError("InputActionAsset이 할당되지 않았습니다.");
            return;
        }

        string key = GetActionKey(actionMapName, actionName);

        // 이미 등록된 액션이면 해제
        if (_registeredActions.ContainsKey(key))
        {
            UnregisterAction(actionMapName, actionName);
        }

        // 액션 찾기
        var actionMap = _inputActions.FindActionMap(actionMapName);
        if (actionMap == null)
        {
            Debug.LogError($"액션 맵 '{actionMapName}'을 찾을 수 없습니다.");
            return;
        }

        var action = actionMap.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"액션 '{actionName}'을 '{actionMapName}' 맵에서 찾을 수 없습니다.");
            return;
        }

        // 액션 등록
        _registeredActions[key] = action;

        // 콜백 연결
        if (onPerformed != null)
        {
            action.performed += onPerformed;
            _callbacks[$"{key}_performed"] = onPerformed;
        }

        if (onStarted != null)
        {
            action.started += onStarted;
            _callbacks[$"{key}_started"] = onStarted;
        }

        if (onCanceled != null)
        {
            action.canceled += onCanceled;
            _callbacks[$"{key}_canceled"] = onCanceled;
        }

        // 활성화
        action.Enable();
    }

    /// <summary>
    /// 등록된 액션을 해제합니다.
    /// </summary>
    public void UnregisterAction(string actionMapName, string actionName)
    {
        string key = GetActionKey(actionMapName, actionName);

        if (!_registeredActions.ContainsKey(key))
        {
            return;
        }

        var action = _registeredActions[key];

        // 콜백 해제
        if (_callbacks.TryGetValue($"{key}_performed", out var performedCallback))
        {
            action.performed -= performedCallback;
            _callbacks.Remove($"{key}_performed");
        }

        if (_callbacks.TryGetValue($"{key}_started", out var startedCallback))
        {
            action.started -= startedCallback;
            _callbacks.Remove($"{key}_started");
        }

        if (_callbacks.TryGetValue($"{key}_canceled", out var canceledCallback))
        {
            action.canceled -= canceledCallback;
            _callbacks.Remove($"{key}_canceled");
        }

        // 비활성화
        action.Disable();

        // 등록 해제
        _registeredActions.Remove(key);
    }

    /// <summary>
    /// 모든 등록된 액션을 활성화합니다.
    /// </summary>
    private void EnableAllActions()
    {
        foreach (var action in _registeredActions.Values)
        {
            action.Enable();
        }
    }

    /// <summary>
    /// 모든 등록된 액션을 비활성화합니다.
    /// </summary>
    private void DisableAllActions()
    {
        foreach (var action in _registeredActions.Values)
        {
            action.Disable();
        }
    }

    /// <summary>
    /// 액션 맵과 액션 이름으로 고유 키를 생성합니다.
    /// </summary>
    private string GetActionKey(string actionMapName, string actionName)
    {
        return $"{actionMapName}/{actionName}";
    }

    /// <summary>
    /// 특정 액션이 현재 눌려있는지 확인합니다.
    /// </summary>
    public bool IsActionPressed(string actionMapName, string actionName)
    {
        string key = GetActionKey(actionMapName, actionName);
        if (_registeredActions.TryGetValue(key, out var action))
        {
            return action.IsPressed();
        }
        return false;
    }

    /// <summary>
    /// 특정 액션의 값을 읽습니다 (Vector2, float 등).
    /// </summary>
    public T ReadActionValue<T>(string actionMapName, string actionName) where T : struct
    {
        string key = GetActionKey(actionMapName, actionName);
        if (_registeredActions.TryGetValue(key, out var action))
        {
            return action.ReadValue<T>();
        }
        return default;
    }
}
