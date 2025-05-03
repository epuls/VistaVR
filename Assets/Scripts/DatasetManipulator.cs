using UnityEngine;

public class DatasetManipulator : MonoBehaviour
{
    [Header("Target Object")]
    [Tooltip("The GameObject that will be rotated and translated.")]
    public GameObject targetObject; // Assign the GameObject you want to control in the Inspector

    [Header("Camera")]
    [Tooltip("The camera used for determining view directions and zoom. If empty, will try to find Camera.main.")]
    [SerializeField] private Camera mainCamera; // Assign the Camera in the Inspector or leave empty to use Camera.main

    [Header("Control Speeds")]
    [Tooltip("How fast the object rotates with the left mouse button.")]
    public float rotationSpeed = 150f;
    [Tooltip("How fast the object translates with the right mouse button.")]
    public float translationSpeed = 5f;
    [Tooltip("How fast the object moves towards/away from the camera with the mouse wheel.")]
    public float zoomSpeed = 500f; // Added speed control for zoom

    // Private variables
    private Transform targetTransform; // Cache the target's transform for efficiency

    void Start()
    {
        // --- Validation Checks ---
        if (targetObject == null)
        {
            Debug.LogError($"Error in {GetType().Name}: Target Object is not assigned in the Inspector!", this);
            this.enabled = false; // Disable this script to prevent errors
            return;
        }

        // Cache the target's transform
        targetTransform = targetObject.transform;

        // Find and cache the main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError($"Error in {GetType().Name}: Camera is not assigned in the Inspector and no camera tagged 'MainCamera' was found.", this);
                this.enabled = false; // Disable this script
                return;
            }
            else
            {
                 Debug.LogWarning($"Warning in {GetType().Name}: Camera was not assigned in the Inspector. Found and assigned the 'MainCamera'.", this);
            }
        }

        var sens = Options.Instance.GetDataManipSensitivity();
        rotationSpeed = sens.x;
        translationSpeed = sens.y;
    }

    void Update()
    {
        // Ensure target and camera haven't been destroyed or become invalid
        if (targetTransform == null || mainCamera == null) return;

        // --- Rotation Handling (Left Mouse Button) ---
        if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt))
        {
            HandleRotation();
        }

        // --- Translation Handling (Right Mouse Button) ---
        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftAlt))
        {
            HandleTranslation();
        }

        // --- Zoom Handling (Mouse Scroll Wheel) ---
        HandleZoom(); // Call the zoom handler every frame to check for input
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        targetTransform.Rotate(mainCamera.transform.up, -mouseX, Space.World);
        targetTransform.Rotate(mainCamera.transform.right, mouseY, Space.World);
    }

    void HandleTranslation()
    {
        float mouseX = Input.GetAxis("Mouse X") * translationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * translationSpeed * Time.deltaTime;

        Vector3 right = mainCamera.transform.right;
        Vector3 up = mainCamera.transform.up;
        Vector3 movement = (right * mouseX) + (up * mouseY);

        targetTransform.Translate(movement, Space.World);
    }

    // --- New Method for Zoom Handling ---
    void HandleZoom()
    {
        // Get scroll wheel input (positive for scroll up/forward, negative for scroll down/backward)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        // Check if there was any scroll input (use a small threshold to avoid tiny values)
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Determine the direction to move: along the camera's forward vector
            // Moving along camera.forward moves the object *away* from the camera on positive scroll
            Vector3 zoomDirection = mainCamera.transform.forward;

            // Calculate the movement amount based on scroll input, speed, and frame time
            Vector3 movement = zoomDirection * scrollInput * zoomSpeed * Time.deltaTime;

            // Apply the translation to the target object's position in world space
            targetTransform.Translate(movement, Space.World);
        }
    }
}