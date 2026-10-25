using UnityEngine;

/// <summary>
/// Lightweight menu controller that transitions from the landing screen
/// into gameplay by toggling entire GameObjects on or off.
/// 
/// Setup:
///   1. In the scene, deactivate the playerObject by default (uncheck its checkbox in the Inspector).
///   2. Assign menuCamera, menuCanvas, and playerObject in the Inspector.
///   3. Wire a UI Button's OnClick event to MenuController.StartGame().
/// </summary>
public class MenuController : MonoBehaviour
{
    [Tooltip("The dedicated menu camera that renders the landing screen.")]
    public GameObject menuCamera;

    [Tooltip("The Canvas holding the main menu UI elements.")]
    public GameObject menuCanvas;

    [Tooltip("The player object (deactivated by default). Activated when the game starts.")]
    public GameObject playerObject;

    /// <summary>
    /// Called by the Play button's OnClick event.
    /// Hides the landing screen and spawns the player.
    /// </summary>
    public void StartGame()
    {
        // Hide the menu camera and UI canvas
        if (menuCamera != null)
            menuCamera.SetActive(false);

        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        // Activate the player object to hand over control
        if (playerObject != null)
            playerObject.SetActive(true);
    }

    /// <summary>
    /// Called by the Quit button's OnClick event.
    /// Exits play mode in the editor or terminates the application in a build.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
