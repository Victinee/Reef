/// <summary>
/// Contract that any interactable world object must fulfil.
/// Implement this interface on any MonoBehaviour that the player can
/// interact with — litter nodes, planting spots, NPCs, etc.
///
/// <para>
/// <b>PC First-Person interaction flow</b>:
/// <list type="number">
///   <item>
///     <see cref="PlayerController"/> fires a centre-screen raycast up to
///     <c>interactRayLength</c> units every frame.
///   </item>
///   <item>
///     When the ray hits a <see cref="UnityEngine.Collider"/> whose GameObject (or
///     any ancestor) carries an <c>IInteractable</c>, it becomes the active target
///     (<c>CrosshairHighlightActive = true</c>).
///   </item>
///   <item>
///     Pressing the 'E' key or Left Mouse Button immediately triggers <see cref="PerformJob"/>.
///   </item>
/// </list>
/// </para>
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Nominal reach distance (world units) for this interactable.
    /// In FPS mode the effective reach is controlled by
    /// <c>PlayerController.interactRayLength</c> (default 4 units).
    /// Kept for interface consistency and potential proximity checks.
    /// </summary>
    float InteractionRadius { get; }

    /// <summary>
    /// Executes the interaction logic (e.g. clean litter, harvest, talk to NPC).
    /// Called automatically by <see cref="PlayerController"/> when the player presses
    /// 'E' or Left Mouse Button while aiming at this object.
    /// </summary>
    void PerformJob();
}
