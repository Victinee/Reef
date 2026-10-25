using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MenuCameraManager uses the player's existing game camera as the main menu background.
/// On scene load, it disables the CharacterController and PlayerController on the Player object
/// to create an 'anti-gravity' effect where the player does not fall or accept movement/look inputs,
/// while keeping the game camera active to showcase the 3D environment behind the UI.
/// Provides a public function for the UI Play button to hide the menu Canvas, re-enable the controls,
/// and seamlessly transition into active gameplay.
/// </summary>
public class MenuCameraManager : MonoBehaviour
{
    [Header("Menu & UI References")]
    [Tooltip("The Canvas holding the main menu UI elements.")]
    [SerializeField] private Canvas menuCanvas;

    [Tooltip("Optional reference to the Play button for auto-wiring onClick.")]
    [SerializeField] private Button playButton;

    [Header("Player References")]
    [Tooltip("Reference to the player GameObject. If null, will search for PlayerController or tag 'Player'.")]
    [SerializeField] private GameObject playerObject;

    [Tooltip("Reference to the CharacterController component on the player.")]
    [SerializeField] private CharacterController characterController;

    [Tooltip("Reference to the PlayerController component on the player.")]
    [SerializeField] private PlayerController playerController;

    [Header("Camera Settings")]
    [Tooltip("The main game camera used as the menu background. Kept active.")]
    [SerializeField] private Camera gameCamera;

    [Header("Cursor Settings")]
    [Tooltip("Whether to show and unlock the cursor during menu mode.")]
    [SerializeField] private bool showCursorInMenu = true;

    [Header("Anti-Gravity Position Stabilization")]
    [Tooltip("Lock the player position during menu mode so external physics or forces cannot move them.")]
    [SerializeField] private bool freezePositionInMenu = true;

    private Vector3 initialPlayerPosition;
    private bool isInMenuMode = true;

    private void Awake()
    {
        ResolveReferences();
        EnterMenuMode();
    }

    private void Start()
    {
        // Re-enforce menu mode in Start in case another component modifies cursor or components on Awake/Start
        if (isInMenuMode)
        {
            ApplyMenuState();
        }

        // Auto-wire the Play button if assigned
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
    }

    private void Update()
    {
        // Keep the player pinned in place if anti-gravity freeze is enabled during menu mode
        if (isInMenuMode && freezePositionInMenu && playerObject != null)
        {
            playerObject.transform.position = initialPlayerPosition;
        }
    }

    /// <summary>
    /// Finds and auto-resolves any missing references in the scene.
    /// </summary>
    private void ResolveReferences()
    {
        // 1. Resolve Menu Canvas
        if (menuCanvas == null)
        {
            menuCanvas = GetComponentInParent<Canvas>();
            if (menuCanvas == null)
            {
                menuCanvas = FindFirstObjectByType<Canvas>();
            }
        }

        // 2. Resolve Player & Components
        if (playerObject == null)
        {
            PlayerController foundController = FindFirstObjectByType<PlayerController>();
            if (foundController != null)
            {
                playerObject = foundController.gameObject;
                playerController = foundController;
            }
            else
            {
                GameObject playerTagObj = GameObject.FindWithTag("Player");
                if (playerTagObj != null)
                {
                    playerObject = playerTagObj;
                }
            }
        }

        if (playerObject != null)
        {
            if (characterController == null)
            {
                characterController = playerObject.GetComponent<CharacterController>();
            }

            if (playerController == null)
            {
                playerController = playerObject.GetComponent<PlayerController>();
            }

            initialPlayerPosition = playerObject.transform.position;
        }

        // 3. Resolve Game Camera
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            if (gameCamera == null && playerObject != null)
            {
                gameCamera = playerObject.GetComponentInChildren<Camera>();
            }
            if (gameCamera == null)
            {
                gameCamera = FindFirstObjectByType<Camera>();
            }
            // If still no camera exists in the scene, create one to render the environment behind the UI
            if (gameCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                gameCamera = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
                camObj.transform.position = new Vector3(0f, 2f, -10f);
                camObj.transform.LookAt(Vector3.zero);
                Debug.Log("[MenuCameraManager] No camera found in scene. Automatically spawned 'Main Camera'.");
            }
        }

        // 4. Resolve Play Button if missing
        if (playButton == null)
        {
            GameObject playBtnObj = GameObject.Find("Button_Play");
            if (playBtnObj != null)
            {
                playButton = playBtnObj.GetComponent<Button>();
            }
        }
    }

    /// <summary>
    /// Disables player input and movement to create the anti-gravity menu state,
    /// while keeping the game camera active.
    /// </summary>
    public void EnterMenuMode()
    {
        isInMenuMode = true;

        if (playerObject != null)
        {
            initialPlayerPosition = playerObject.transform.position;
        }

        ApplyMenuState();
    }

    private void ApplyMenuState()
    {
        // Show the menu canvas
        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(true);
        }

        // Ensure the game camera is active so it renders the environment in the background
        if (gameCamera != null)
        {
            gameCamera.gameObject.SetActive(true);
            gameCamera.enabled = true;
        }

        // Disable CharacterController so no falling/gravity/collision resolution happens
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Disable PlayerController so player receives no mouse look or WASD input
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Display cursor for UI interaction
        if (showCursorInMenu)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Public function for the UI Play button.
    /// Hides the menu Canvas, re-enables CharacterController and PlayerController,
    /// and restores normal gameplay gravity and input seamlessly.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        StartGameplay();
    }

    /// <summary>
    /// Seamlessly starts gameplay by hiding the menu and enabling player controllers.
    /// </summary>
    public void StartGameplay()
    {
        isInMenuMode = false;

        // 1. Hide the menu Canvas
        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(false);
        }

        // 2. Re-enable the CharacterController (gravity and physical movement restored)
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 3. Re-enable the PlayerController (input and camera control restored)
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // 4. Lock cursor for gameplay (PlayerController uses locked cursor)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[MenuCameraManager] Play button clicked: Menu hidden, gravity and player controls enabled.");
    }
}
