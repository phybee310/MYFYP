using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class ARPlaceAndDragCube : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("UI Elements")]
    [Tooltip("Drag the Confirm button from your Model Selection Panel here.")]
    public GameObject confirmButton; // 1. ADDED: Reference to the confirm button

    [Header("Model Placement")]
    [Tooltip("The model prefab you currently want to place.")]
    [SerializeField] private GameObject selectedPrefab;

    private GameObject spawnedObject;
    private bool isDragging = false;

    private static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    // 2. ADDED: Hide the confirm button right when the app opens
    private void Start()
    {
        if (confirmButton != null)
        {
            confirmButton.SetActive(false);
        }
    }

    void Update()
    {
        if (!raycastManager) return;

        bool isPressed = false;
        bool wasPressedThisFrame = false;
        Vector2 screenPosition = default;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            isPressed = touch.press.isPressed;
            wasPressedThisFrame = touch.press.wasPressedThisFrame;
            screenPosition = touch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            isPressed = Mouse.current.leftButton.isPressed;
            wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            screenPosition = Mouse.current.position.ReadValue();
        }

        if (wasPressedThisFrame)
        {
            if (IsPointerOverUI(screenPosition))
            {
                isDragging = false;
                return;
            }

            isDragging = true;
        }

        if (isPressed && isDragging && spawnedObject != null)
        {
            DragObject(screenPosition);
        }

        if (!isPressed)
        {
            isDragging = false;
        }
    }

    private void DragObject(Vector2 touchPosition)
    {
        raycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon);

        if (s_Hits.Count > 0)
        {
            spawnedObject.transform.position = s_Hits[0].pose.position;
            spawnedObject.transform.rotation = s_Hits[0].pose.rotation;
        }
    }

    public void SpawnModelFromButton(GameObject newPrefab)
    {
        selectedPrefab = newPrefab;

        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
        }

        Vector2 centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
        raycastManager.Raycast(centerScreen, s_Hits, TrackableType.PlaneWithinPolygon);

        if (s_Hits.Count > 0)
        {
            spawnedObject = Instantiate(selectedPrefab, s_Hits[0].pose.position, s_Hits[0].pose.rotation);
        }
        else
        {
            Transform camTransform = Camera.main.transform;
            Vector3 spawnPos = camTransform.position + camTransform.forward * 1.0f;
            spawnPos.y -= 0.25f;
            spawnedObject = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
        }

        // 3. ADDED: Now that the model is actually spawned, show the confirm button!
        if (confirmButton != null)
        {
            confirmButton.SetActive(true);
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = screenPosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0;
    }
}