using UnityEngine;

[RequireComponent(typeof(GravityGrabber))]
public class PocketShield : MonoBehaviour
{

    [SerializeField] GameSession session;

    [SerializeField] Camera viewCamera;

    [SerializeField] GameObject shieldPrefab;

    [SerializeField] float deployDistance = 1.4f;

    GravityGrabber grabber;

    public bool IsDeployed => grabber != null && grabber.HeldBox != null && grabber.HeldBox.GetComponent<PocketShieldItem>() != null;

    void Awake() { grabber = GetComponent<GravityGrabber>(); }
    void Update() { if (session.CanPlay && Input.GetKeyDown(KeyCode.G)) Toggle(); }
    public bool Toggle()
    {

        if (session.State != GameState.Playing) return false;

        if (IsDeployed) { grabber.Release(); return true; }
        if (grabber.HeldBox != null) return false;

        if (shieldPrefab == null) return false;

        Vector3 origin = viewCamera.transform.position;

        Vector3 extents = Vector3.Scale(shieldPrefab.GetComponent<BoxCollider>().size, shieldPrefab.transform.localScale) * 0.5f;

        Vector3 center = origin + viewCamera.transform.forward * deployDistance - viewCamera.transform.up * 0.2f;

        // Refuse deployment when there is no room; never spawn through a wall.
        if (Physics.CheckBox(origin, extents, Quaternion.identity, ~4, QueryTriggerInteraction.Ignore) ||
            Physics.BoxCast(origin, extents, (center - origin).normalized, Quaternion.identity, (center - origin).magnitude, ~4, QueryTriggerInteraction.Ignore) ||
            Physics.CheckBox(center, extents, Quaternion.identity, ~4, QueryTriggerInteraction.Ignore)) return false;

        var item = Instantiate(shieldPrefab, center, Quaternion.identity).GetComponent<GrabbableBox>();

        grabber.Hold(item); return true;

    }

}

