using UnityEngine;

public class GravityGrabber : MonoBehaviour
{

    [SerializeField] GameSession session;

    [SerializeField] Camera viewCamera;

    [SerializeField] float grabDistance = 4f;

    [SerializeField] float holdDistance = 2.2f;

    [SerializeField] float maxMass = 50f;

    [SerializeField] float followSpeed = 10f;

    [SerializeField, Min(0f)] float throwImpulse = 120f;

    [SerializeField, Min(0f)] float aimAssistRadius = 0.35f;

    public GrabbableBox HeldBox => held;

    public GrabbableBox AvailableBox { get; private set; }
    [SerializeField] LayerMask obstacleMask = ~4; // Player uses Ignore Raycast (layer 2).
    GrabbableBox held;

    bool savedGravity;

    CollisionDetectionMode savedDetection;

    RigidbodyInterpolation savedInterpolation;

    void Update()
    {

        AvailableBox = held == null ? FindGrabbable() : null;

        if (!session.CanPlay) return;

        if (Input.GetMouseButtonDown(0) && held != null) { ThrowHeld(); return; }
        if (Input.GetKeyDown(KeyCode.E)) Interact();

    }

    public void Interact()
    {

        if (session.State != GameState.Playing) return;

        if (held != null && held.GetComponent<PocketShieldItem>() != null) { Release(); TryGrab(); }
        else if (held != null) Release();

        else TryGrab();

    }    public GrabbableBox FindGrabbable()
    {

        Vector3 origin = viewCamera.transform.position;

        GrabbableBox closest = null;

        float nearest = grabDistance + 1f;

        foreach (RaycastHit hit in Physics.SphereCastAll(origin, aimAssistRadius, viewCamera.transform.forward,
                     grabDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {

            var box = hit.collider.GetComponentInParent<GrabbableBox>();

            if (box == null || box.Body == null || box.Body.isKinematic || box.Body.mass > maxMass) continue;

            Vector3 point = box.Shape.ClosestPoint(origin);

            Vector3 sight = point - origin;

            if (sight.magnitude > grabDistance || sight.magnitude >= nearest) continue;

            // A wide selection beam still needs a clear line to the box: no grabbing through walls.
            if (Physics.Raycast(origin, sight.normalized, out RaycastHit blocker, sight.magnitude + 0.02f,
                    obstacleMask, QueryTriggerInteraction.Ignore) && blocker.collider.attachedRigidbody != box.Body) continue;

            closest = box; nearest = sight.magnitude;

        }

        return closest;

    }

    public float GetThrowImpulse(float mass) => throwImpulse * Mathf.Sqrt(Mathf.Max(1f, mass / 10f));

    void TryGrab()
    {

        GrabbableBox box = FindGrabbable();

        if (box == null) return;

        Hold(box);

    }

    public void Hold(GrabbableBox box)
    {

        GameAudio.PlayAt("grab", viewCamera.transform.position);

        held = box;

        held.Disarm();

        savedGravity = held.Body.useGravity;

        savedDetection = held.Body.collisionDetectionMode;

        savedInterpolation = held.Body.interpolation;

        held.Body.linearVelocity = Vector3.zero;

        held.Body.angularVelocity = Vector3.zero;

        held.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        held.Body.isKinematic = true;

        held.Body.useGravity = false;

        held.Body.interpolation = RigidbodyInterpolation.Interpolate;

    }

    void FixedUpdate()
    {

        if (held == null || !session.CanPlay) return;

        Vector3 origin = viewCamera.transform.position;

        if (Vector3.Distance(origin, held.Body.position) > grabDistance + 1f) { Release(); return; }
        Vector3 desired = origin + viewCamera.transform.forward * (held.GetComponent<PocketShieldItem>() != null ? 1.4f : holdDistance);

        if (held.GetComponent<PocketShieldItem>() != null) desired -= viewCamera.transform.up * 0.2f;

        // A world-aligned bounding box is conservative even if the crate fell at an angle.
        Vector3 extents = held.Shape.bounds.extents * 0.98f;

        Vector3 center = held.Shape.bounds.center;

        Vector3 offset = center - held.Body.position;

        Vector3 direction = desired + offset - origin;

        float distance = direction.magnitude;

        float allowed = distance;

        foreach (RaycastHit hit in Physics.BoxCastAll(origin, extents, direction.normalized, Quaternion.identity,
                     distance, obstacleMask, QueryTriggerInteraction.Ignore))
            if (hit.collider.attachedRigidbody != held.Body) allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.07f));

        // Too little room ahead: keep the last safe position instead of pulling into the player.
        if (allowed < 1.2f) return;

        Vector3 target = origin + direction.normalized * allowed - offset;

        Vector3 step = Vector3.MoveTowards(held.Body.position, target, followSpeed * Time.fixedDeltaTime);

        Vector3 travel = step - held.Body.position;

        float safeTravel = travel.magnitude;

        if (safeTravel > 0.0001f)
            foreach (RaycastHit hit in Physics.BoxCastAll(center, extents, travel.normalized, Quaternion.identity,
                         safeTravel, obstacleMask, QueryTriggerInteraction.Ignore))
                if (hit.collider.attachedRigidbody != held.Body) safeTravel = Mathf.Min(safeTravel, Mathf.Max(0f, hit.distance - 0.07f));

        step = held.Body.position + travel.normalized * safeTravel;

        foreach (Collider other in Physics.OverlapBox(step + offset, extents, Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore))
            if (other.attachedRigidbody != held.Body) return;

        held.Body.MovePosition(step);

    }

    void ThrowHeld()
    {

        if (held.GetComponent<PocketShieldItem>() != null) return;

        GrabbableBox box = held;

        Vector3 direction = viewCamera.transform.forward;

        Release();

        var grenade = box.GetComponent<Grenade>();

        if (grenade != null) grenade.MarkReturned();

        box.Throw(direction, grenade != null ? box.Body.mass * 12f : GetThrowImpulse(box.Body.mass));

        GameAudio.PlayAt("throw", viewCamera.transform.position);

    }

    public void Release()
    {

        if (held == null) return;

        if (held.GetComponent<PocketShieldItem>() != null)
        {

            var item = held.gameObject; held = null;

            item.SetActive(false);

            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);

            return;

        }

        held.Disarm();

        held.Body.isKinematic = false;

        held.Body.useGravity = savedGravity;

        held.Body.collisionDetectionMode = savedDetection;

        held.Body.interpolation = savedInterpolation;

        held.Body.linearVelocity = Vector3.zero;

        held.Body.angularVelocity = Vector3.zero;

        held = null;

    }

    void OnDisable() { Release(); }
}

