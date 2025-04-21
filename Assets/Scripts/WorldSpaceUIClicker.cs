using UnityEngine;
using UnityEngine.EventSystems; // Required for event systems and pointer data
using System.Collections.Generic; // Required for List

[RequireComponent(typeof(Camera))] // Ensure this script is attached to a GameObject with a Camera
public class WorldSpaceUIClicker : MonoBehaviour
{
    // No need to store the camera reference explicitly if GetComponent is used in Start
    // private Camera _camera;

    private PointerEventData _pointerEventData;
    private EventSystem _eventSystem;

    void Start()
    {
        // Basic check to ensure the script is on a camera GameObject
        if (GetComponent<Camera>() == null)
        {
            Debug.LogError("WorldSpaceUIClicker requires a Camera component on the same GameObject.", this);
            this.enabled = false; // Disable script if no camera found
            return;
        }

        // Find the scene's EventSystem
        _eventSystem = EventSystem.current; // Gets the currently active EventSystem
        if (_eventSystem == null)
        {
            Debug.LogError("WorldSpaceUIClicker requires an EventSystem in the scene. Please add one (GameObject -> UI -> Event System).", this);
            this.enabled = false;
            return;
        }

        // Create a PointerEventData object that we can reuse
        // Pass the found EventSystem to the constructor
        _pointerEventData = new PointerEventData(_eventSystem);
    }

    void Update()
    {
        // Check if the left mouse button was clicked down this frame
        if (Input.GetMouseButtonDown(0))
        {
            PerformRaycast();
        }
    }

    void PerformRaycast()
    {
        // Ensure EventSystem hasn't been destroyed
        if (_eventSystem == null) return;

        // Set the pointer event data's position to the current mouse position
        _pointerEventData.position = Input.mousePosition;

        // Create a list to receive Raycast results
        List<RaycastResult> results = new List<RaycastResult>();

        // --- Corrected Part: Raycast using the EventSystem ---
        // This is the central function for UI raycasting.
        // It considers all active Raycasters (GraphicRaycaster, PhysicsRaycaster, etc.)
        _eventSystem.RaycastAll(_pointerEventData, results);

        // Check if any UI elements (or potentially physics objects if PhysicsRaycaster is used) were hit
        if (results.Count > 0)
        {
            // The results list is sorted by distance/sorting order. The first element is the topmost one hit.
            RaycastResult topHit = results[0];

            // Check if the hit object has a RectTransform component, which is typical for UI elements.
            // This helps differentiate from potential PhysicsRaycaster hits if one is active.
            if (topHit.gameObject.GetComponent<RectTransform>() != null)
            {
                // We likely hit a UI element!
                Debug.Log($"Clicked on UI Element: {topHit.gameObject.name}", topHit.gameObject);

                // --- Optional: Simulate a Click Event ---
                GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(topHit.gameObject);
                GameObject dragHandler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(topHit.gameObject);

                if (clickHandler != null)
                {
                    Debug.Log($"Executing PointerClick on: {clickHandler.name}", clickHandler);
                    ExecuteEvents.Execute(clickHandler, _pointerEventData, ExecuteEvents.pointerClickHandler);
                }
                else
                {
                     Debug.Log($"UI Element '{topHit.gameObject.name}' was hit, but no IPointerClickHandler found on it or its parents.", topHit.gameObject);
                }
                
                if (dragHandler != null)
                {
                    Debug.Log($"Executing Drag on: {dragHandler.name}", dragHandler);
                    ExecuteEvents.Execute(dragHandler, _pointerEventData, ExecuteEvents.pointerDownHandler);
                }

                // Optional: Execute other events
                // ExecuteEvents.Execute(ExecuteEvents.GetEventHandler<IPointerDownHandler>(topHit.gameObject), _pointerEventData, ExecuteEvents.pointerDownHandler);
                // ExecuteEvents.Execute(ExecuteEvents.GetEventHandler<IPointerUpHandler>(topHit.gameObject), _pointerEventData, ExecuteEvents.pointerUpHandler);

            }
            else
            {
                // The topmost hit wasn't a standard UI element (might be a 3D object hit via PhysicsRaycaster)
                // Debug.Log($"Clicked on non-UI object: {topHit.gameObject.name}", topHit.gameObject);
            }
        }
        else
        {
            // No UI elements (or physics objects) were hit by the EventSystem's raycast
            // Debug.Log("Clicked on empty space.");
        }
    }
}