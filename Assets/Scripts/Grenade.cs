using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(GrabbableBox))]
public class Grenade : MonoBehaviour
{

    [SerializeField, Min(0.5f)] float fuseTime = 4f;

    [SerializeField, Min(0.1f)] float blastRadius = 5f;

    [SerializeField, Min(1)] int damage = 20;

    [SerializeField, Min(1)] int shieldedDamage = 10;

    GameSession session;

    Health player;

    Collider[] ownerShapes;

    float detonateTime;

    bool returned;

    bool detonated;

    public bool IsDetonated => detonated;

    public float RemainingTime => Mathf.Max(0f, detonateTime - Time.time);

    public void Initialize(GameSession game, Health playerHealth, Transform owner)
    {

        session = game; player = playerHealth; detonateTime = Time.time + fuseTime;

        ownerShapes = owner != null ? owner.GetComponentsInChildren<Collider>() : new Collider[0];

        foreach (Collider shape in ownerShapes) Physics.IgnoreCollision(GetComponent<BoxCollider>(), shape, true);

    }

    public void MarkReturned()
    {

        returned = true;

        if (ownerShapes != null)
            foreach (Collider shape in ownerShapes)
                if (shape != null) Physics.IgnoreCollision(GetComponent<BoxCollider>(), shape, false);

    }

    void Update()
    {

        if (session != null && session.State == GameState.Playing && Time.time >= detonateTime) Detonate();

    }

    void FixedUpdate()
    {

        var box = GetComponent<GrabbableBox>();

        if (detonated || player == null || player.IsDead || box.Body.isKinematic || (session != null && session.State != GameState.Playing)) return;

        var shape = box.Shape;

        Vector3 center = shape.transform.TransformPoint(shape.center);

        Vector3 extents = Vector3.Scale(shape.size, shape.transform.lossyScale) * 0.5f;

        foreach (Collider other in Physics.OverlapBox(center, extents, shape.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            if (other.GetComponentInParent<Health>() == player) { Detonate(); return; }
        // Sweep because CharacterController contacts may not generate OnCollisionEnter.
        Vector3 delta = box.Body.linearVelocity * Time.fixedDeltaTime;

        RaycastHit closest = default; float nearest = float.PositiveInfinity;

        if (delta.sqrMagnitude < 0.000001f) return;

        foreach (RaycastHit hit in Physics.BoxCastAll(center, extents, delta.normalized, shape.transform.rotation, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {

            if (hit.collider.transform.IsChildOf(transform)) continue;

            bool owner = false;

            if (ownerShapes != null) foreach (Collider ownerShape in ownerShapes) if (ownerShape == hit.collider) owner = true;

            if (owner || hit.distance >= nearest) continue;

            closest = hit; nearest = hit.distance;

        }

        if (closest.collider != null && closest.collider.GetComponentInParent<Health>() == player) Detonate();

    }

    public int DamageFor(Health target)
    {

        if (target == null || target.IsDead || (target != player && !returned)) return 0;

        Collider targetShape = target.GetComponent<Collider>();

        Vector3 center = targetShape != null ? targetShape.bounds.center : target.transform.position;

        if (Vector3.Distance(transform.position, targetShape != null ? targetShape.ClosestPoint(transform.position) : center) > blastRadius) return 0;

        var grabber = target.GetComponent<GravityGrabber>();

        GrabbableBox shield = grabber != null ? grabber.HeldBox : null;

        Vector3 line = center - transform.position;

        bool protectedByBox = false;

        foreach (RaycastHit hit in Physics.RaycastAll(transform.position, line.normalized, line.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {

            if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<Health>() == target) continue;

            var box = hit.collider.GetComponentInParent<GrabbableBox>();

            if (box != null) { if (box == shield && box.GetComponent<Grenade>() == null) protectedByBox = true; continue; }
            // Solid arena walls block a blast; crates only soften it for a shielded player.
            if (hit.collider.GetComponentInParent<Health>() == null) return 0;

        }

        return target == player && protectedByBox ? shieldedDamage : damage;

    }

    void OnCollisionEnter(Collision collision)
    {

        var target = collision.collider.GetComponentInParent<Health>();

        if (target != null && (target == player || returned)) Detonate();

    }

    public void HitPlayer(Health target)
    {

        if (target != null && target == player && !GetComponent<GrabbableBox>().Body.isKinematic) Detonate();

    }

    public void Detonate()
    {

        if (detonated) return;

        detonated = true;

        var targets = new HashSet<Health>();

        foreach (Collider shape in Physics.OverlapSphere(transform.position, blastRadius, ~0, QueryTriggerInteraction.Ignore))
        {

            var health = shape.GetComponentInParent<Health>();

            if (health != null) targets.Add(health);

        }

        // Calculate all damage before callbacks can change the game state or release the shield.
        var amounts = new Dictionary<Health, int>();

        foreach (Health target in targets) amounts[target] = DamageFor(target);

        // Loss takes precedence if one explosion kills both the player and the last enemy.
        if (player != null && amounts.TryGetValue(player, out int playerDamage)) player.TakeDamage(playerDamage);

        foreach (var pair in amounts) if (pair.Key != player) pair.Key.TakeDamage(pair.Value);

        if (Application.isPlaying)
        {

            GameAudio.PlayAt("shot", transform.position);

            ExplosionFlash.Show(transform.position, blastRadius);

            Destroy(gameObject);

        }

    }

}

