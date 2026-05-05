using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARPlaceAndDragCube : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("UI Elements")]
    public GameObject confirmButton;
    public GameObject scanningPromptPanel;

    [Header("Model Placement")]
    [SerializeField] private GameObject selectedPrefab;

    [Header("Scaling Settings")]
    public float pinchScaleSpeed = 0.005f;
    public float scrollScaleSpeed = 0.001f;
    public float minScale = 0.01f;
    public float maxScale = 1.0f;

    private GameObject spawnedObject;
    private bool isDragging = false;
    private bool isPreviewing = false;
    private bool isPlacementLocked = false;
    private bool isWaitingForSurface = false;

    private int stableHitCount = 0;
    private const int requiredStableFrames = 5;

    private Vector2 initialTouchPosition;
    private float dragThreshold = 10f;

    private static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    private float _touchStartTime;
    private float _holdToDragDuration = 0.25f;
    private bool _isPatting = false;

    private void OnEnable() => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Start()
    {
        if (confirmButton != null) confirmButton.SetActive(false);
        if (scanningPromptPanel != null) scanningPromptPanel.SetActive(false);
    }

    void Update()
    {
        if (!raycastManager) return;
        if (ARSession.state != ARSessionState.SessionTracking) return;

        if (isWaitingForSurface)
        {
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (raycastManager.Raycast(screenCenter, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                ARPlane plane = planeManager.GetPlane(s_Hits[0].trackableId);

                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    stableHitCount++;

                    if (stableHitCount >= requiredStableFrames)
                    {
                        isWaitingForSurface = false;
                        stableHitCount = 0;

                        if (scanningPromptPanel != null) scanningPromptPanel.SetActive(false);

                        spawnedObject = Instantiate(selectedPrefab, s_Hits[0].pose.position, s_Hits[0].pose.rotation);
                        RotateToFaceCamera();

                        isPreviewing = true;
                        isDragging = false;

                        if (confirmButton != null) confirmButton.SetActive(true);
                    }
                }
                else
                {
                    stableHitCount = Mathf.Max(0, stableHitCount - 1);
                }
            }
            else
            {
                stableHitCount = Mathf.Max(0, stableHitCount - 1);
            }

            return;
        }

        if (isPreviewing && !isDragging && spawnedObject != null)
        {
            LockToCenterScreen();
        }

        if (Touch.activeTouches.Count == 2 && spawnedObject != null)
        {
            isDragging = false;

            Touch touch0 = Touch.activeTouches[0];
            Touch touch1 = Touch.activeTouches[1];

            Vector2 touch0PrevPos = touch0.screenPosition - touch0.delta;
            Vector2 touch1PrevPos = touch1.screenPosition - touch1.delta;

            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.screenPosition - touch1.screenPosition).magnitude;

            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            ScaleModel(deltaMagnitudeDiff * pinchScaleSpeed);
            return;
        }

        if (Mouse.current != null && spawnedObject != null)
        {
            float scrollValue = Mouse.current.scroll.ReadValue().y;
            if (scrollValue != 0)
            {
                ScaleModel(scrollValue * scrollScaleSpeed);
            }
        }

        bool isPressed = false;
        bool wasPressedThisFrame = false;
        Vector2 screenPosition = default;

        if (Pointer.current != null && Touch.activeTouches.Count < 2)
        {
            isPressed = Pointer.current.press.isPressed;
            wasPressedThisFrame = Pointer.current.press.wasPressedThisFrame;
            screenPosition = Pointer.current.position.ReadValue();
        }

        if (wasPressedThisFrame)
        {
            if (IsPointerOverUI(screenPosition))
            {
                isDragging = false;
                _isPatting = false;
                return;
            }

            _touchStartTime = Time.time;
            initialTouchPosition = screenPosition;
            isDragging = false;
            _isPatting = false;

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent<TapToAnimate>(out TapToAnimate animScript))
                {
                    animScript.TriggerAnimation();
                }
                else if (hit.transform.TryGetComponent<TapToBounce>(out TapToBounce bounceScript))
                {
                    bounceScript.TriggerAnimation();
                }
                else if (hit.transform.TryGetComponent<CatInteraction>(out CatInteraction catScript))
                {
                    catScript.NormalTap();
                }
                // --- NEW: Check if the user tapped a Flower ---
                else if (hit.transform.TryGetComponent<FlowerInteraction>(out FlowerInteraction flowerScript))
                {
                    flowerScript.TapFlower();
                }
            }
        }

        if (isPressed && spawnedObject != null)
        {
            float currentHoldTime = Time.time - _touchStartTime;

            if (!isDragging && !_isPatting)
            {
                if (Vector2.Distance(screenPosition, initialTouchPosition) > dragThreshold)
                {
                    if (currentHoldTime >= _holdToDragDuration)
                    {
                        isDragging = true;
                        isPreviewing = false;

                        if (spawnedObject.TryGetComponent<CatWander>(out CatWander catWanderScript))
                        {
                            catWanderScript.PauseWander();
                        }
                    }
                    else
                    {
                        _isPatting = true;

                        if (spawnedObject.TryGetComponent<CatInteraction>(out CatInteraction catScript))
                        {
                            catScript.PatCat();
                        }
                    }
                }
            }

            if (isDragging)
            {
                DragObject(screenPosition);
            }
        }

        if (!isPressed)
        {
            if (isDragging)
            {
                if (spawnedObject != null && spawnedObject.TryGetComponent<CatWander>(out CatWander catWanderScript))
                {
                    catWanderScript.ResumeWander(spawnedObject.transform.position);
                }
            }

            isDragging = false;
            _isPatting = false;
        }
    }

    private void RotateToFaceCamera()
    {
        if (spawnedObject == null || Camera.main == null) return;

        Vector3 directionToCamera = Camera.main.transform.position - spawnedObject.transform.position;
        directionToCamera.y = 0;

        if (directionToCamera != Vector3.zero)
        {
            spawnedObject.transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }

    private void ScaleModel(float scaleAdjustment)
    {
        Vector3 newScale = spawnedObject.transform.localScale + Vector3.one * scaleAdjustment;
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
        spawnedObject.transform.localScale = newScale;
    }

    private void LockToCenterScreen()
    {
        Vector2 centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (raycastManager.Raycast(centerScreen, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            ARPlane plane = planeManager.GetPlane(s_Hits[0].trackableId);
            if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
            {
                spawnedObject.transform.position = s_Hits[0].pose.position;
            }
        }
    }

    private void DragObject(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 newPosition = s_Hits[0].pose.position;
            newPosition.y = spawnedObject.transform.position.y;
            spawnedObject.transform.position = newPosition;
        }
    }

    public void SpawnModelFromButton(GameObject newPrefab)
    {
        if (isPlacementLocked) return;

        if (spawnedObject != null) Destroy(spawnedObject);

        selectedPrefab = newPrefab;
        isWaitingForSurface = true;
        stableHitCount = 0;

        if (scanningPromptPanel != null) scanningPromptPanel.SetActive(true);
        if (confirmButton != null) confirmButton.SetActive(false);
    }

    public void LockPlacement()
    {
        isPlacementLocked = true;

        if (confirmButton != null) confirmButton.SetActive(false);

        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                if (plane.TryGetComponent<MeshRenderer>(out MeshRenderer mesh))
                {
                    mesh.enabled = false;
                }
            }
            planeManager.enabled = false;
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0;
    }
}