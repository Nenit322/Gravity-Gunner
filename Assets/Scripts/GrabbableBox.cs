using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class GrabbableBox : MonoBehaviour
{

    [SerializeField, Min(0f)] float minimumDamageSpeed = 3f;

    [SerializeField, Min(0f)] float damagePerSpeed = 5f;

    [SerializeField, Min(1)] int maximumDamage = 60;

    readonly HashSet<Health> hitTargets = new HashSet<Health>();

    bool canDamage;

    float settleCheckTime;

    public Rigidbody Body { get; private set; }
    public BoxCollider Shape { get; private set; }
    void Awake() { Body = GetComponent<Rigidbody>(); Shape = GetComponent<BoxCollider>(); }

    public void Disarm() { canDamage = false; hitTargets.Clear(); }
    public void Throw(Vector3 direction, float impulse)
    {

        if (GetComponent<PocketShieldItem>() != null) return;

        if (Body.isKinematic || direction.sqrMagnitude < 0.001f || impulse <= 0f) return;

        hitTargets.Clear();

        canDamage = GetComponent<Grenade>() == null;

        settleCheckTime = Time.time + 0.2f;

        // CCD reduces tunnelling when a light crate is thrown quickly.
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Body.AddForce(direction.normalized * impulse, ForceMode.Impulse);

    }

    void OnCollisionEnter(Collision collision)
    {

        TryDamageTarget(collision.collider.GetComponentInParent<Health>(), collision.relativeVelocity.magnitude);

        if (!Body.isKinematic && collision.contactCount > 0 && collision.relativeVelocity.magnitude > 0.5f)
        {

            Vector3 normal = collision.GetContact(0).normal;

            // A small outward speed separates the crate from vertical surfaces without bouncing on the floor forever.
            if (Mathf.Abs(normal.y) < 0.7f)
                Body.linearVelocity = Vector3.ProjectOnPlane(Body.linearVelocity, normal) + normal * Mathf.Clamp(collision.relativeVelocity.magnitude * 0.18f, 0.6f, 2.2f);

        }

    }

    public bool TryDamageTarget(Health target, float impactSpeed)
    {

        if (GetComponent<PocketShieldItem>() != null) return false;

        if (!canDamage || Body.isKinematic || target == null || target.IsDead ||
            impactSpeed <= minimumDamageSpeed || hitTargets.Contains(target)) return false;

        float massMultiplier = Mathf.Clamp(Body.mass / 10f, 1f, 4f);

        int damage = Mathf.Clamp(Mathf.CeilToInt((impactSpeed - minimumDamageSpeed) * damagePerSpeed * massMultiplier), 1, maximumDamage);

        // Remember the health component, not a collider: multiple colliders share one hit per throw.
        hitTargets.Add(target);

        target.TakeDamage(damage);

        return true;

    }

    void FixedUpdate()
    {

        // Once the crate settles, a later accidental bump must not count as a throw.
        if (canDamage && Time.time >= settleCheckTime && (Body.IsSleeping() || Body.linearVelocity.sqrMagnitude < 0.04f)) canDamage = false;

    }

}

