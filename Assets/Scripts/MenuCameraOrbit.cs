using UnityEngine;

/// <summary>
/// MenuCameraOrbit acts like an aerial drone, smoothly orbiting around a
/// central island target to create a cinematic main menu background.
///
/// Uses transform.RotateAround for the circular pan and transform.LookAt
/// to keep the island centered. Entirely transform-based — no Rigidbody,
/// CharacterController, or physics of any kind.
///
/// Setup:
///   1. Attach this script to the dedicated menu camera GameObject.
///   2. Assign the island (or any central pivot) to the 'target' field.
///   3. Tweak orbitSpeed, orbitRadius, and orbitHeight in the Inspector.
/// </summary>
public class MenuCameraOrbit : MonoBehaviour
{
    [Header("Orbit Target")]
    [Tooltip("The Transform the camera orbits around (e.g. the island root).")]
    [SerializeField] private Transform target;

    [Header("Orbit Settings")]
    [Tooltip("Degrees per second the camera orbits. Negative = counter-clockwise.")]
    [SerializeField] private float orbitSpeed = 8f;

    [Tooltip("Distance from the target along the horizontal plane.")]
    [SerializeField] private float orbitRadius = 20f;

    [Tooltip("Height above the target's Y position.")]
    [SerializeField] private float orbitHeight = 10f;

    [Header("Look Offset")]
    [Tooltip("Vertical offset added to the LookAt point so the camera gazes slightly above/below the target's pivot.")]
    [SerializeField] private float lookAtYOffset = 2f;

    [Header("Smoothing")]
    [Tooltip("How smoothly the camera interpolates its look direction (0 = instant, higher = smoother).")]
    [SerializeField] private float lookSmoothSpeed = 4f;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[MenuCameraOrbit] No target assigned. Attempting to find 'Island' by name...");
            GameObject island = GameObject.Find("Island");
            if (island != null)
            {
                target = island.transform;
            }
            else
            {
                Debug.LogError("[MenuCameraOrbit] No orbit target found! Assign one in the Inspector.");
                enabled = false;
                return;
            }
        }

        // Snap the camera to its initial orbit position
        PositionOnOrbit();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Rotate around the target's Y-axis at orbitSpeed degrees per second
        transform.RotateAround(
            target.position,
            Vector3.up,
            orbitSpeed * Time.deltaTime
        );

        // Maintain the correct orbit radius and height
        PositionOnOrbit();

        // Smoothly look at the target (with optional Y offset)
        Vector3 lookPoint = target.position + Vector3.up * lookAtYOffset;
        Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            lookSmoothSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// Snaps the camera's distance and height relative to the target
    /// without changing its current horizontal angle.
    /// </summary>
    private void PositionOnOrbit()
    {
        Vector3 direction = (transform.position - target.position);
        direction.y = 0f; // flatten to horizontal plane

        if (direction.sqrMagnitude < 0.001f)
        {
            // Avoid zero-length direction on first frame — pick an arbitrary start
            direction = Vector3.forward;
        }

        direction = direction.normalized * orbitRadius;

        transform.position = target.position + direction + Vector3.up * orbitHeight;
    }
}
