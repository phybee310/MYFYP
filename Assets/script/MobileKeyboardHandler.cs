using UnityEngine;

public class MobileKeyboardHandler : MonoBehaviour
{
    [Header("References")]
    public RectTransform panelToMove;  
    public Canvas canvas;

    [Header("Settings")]
    public float animationSpeed = 10f;
    public float keyboardHeightRatio = 0.4f; 

    private float _defaultY;
    private float _targetY;
    private bool _keyboardVisible;
    private TouchScreenKeyboard _keyboard;

    void Start()
    {
        if (panelToMove == null || canvas == null)
        {
            Debug.LogError("KeyboardHandler: Missing references.");
            enabled = false;
            return;
        }

        _defaultY = panelToMove.anchoredPosition.y;
        _targetY = _defaultY;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        UpdateKeyboardState();
        UpdatePanelPosition();
#endif
    }

    private void UpdateKeyboardState()
    {
        bool visible = false;

        if (TouchScreenKeyboard.visible)
        {
            visible = true;
        }

    
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (selected != null &&
                (selected.GetComponent<TMPro.TMP_InputField>() != null ||
                 selected.GetComponent<UnityEngine.UI.InputField>() != null))
            {
                visible = true;
            }
        }

        _keyboardVisible = visible;
    }

    private float GetKeyboardHeight()
    {
        float height = TouchScreenKeyboard.area.height;

        
        if (height <= 0)
        {
            height = Screen.height * keyboardHeightRatio;
        }

        return height / canvas.scaleFactor;
    }

    private void UpdatePanelPosition()
    {
        if (_keyboardVisible)
        {
            float keyboardHeight = GetKeyboardHeight();
            _targetY = _defaultY + keyboardHeight;
        }
        else
        {
            _targetY = _defaultY;
        }

        Vector2 pos = panelToMove.anchoredPosition;
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * animationSpeed);
        panelToMove.anchoredPosition = pos;
    }
}