using UnityEngine;

// Only the decorative rig moves: the collider and aiming origin stay on the root.
[RequireComponent(typeof(EnemyShooter))]
public class EnemyVisualFeedback : MonoBehaviour
{

    [SerializeField] GameSession session;

    [SerializeField] Transform viewTarget;

    [SerializeField] Transform visualRig;

    [SerializeField] Renderer visor;

    EnemyShooter shooter;

    MaterialPropertyBlock colors;

    EnemyMovement movement;

    float step;

    void Awake()
    {

        shooter = GetComponent<EnemyShooter>();

        colors = new MaterialPropertyBlock();

        movement = GetComponent<EnemyMovement>();

    }

    void LateUpdate()
    {

        if (session == null || !session.CanPlay || !shooter.HasAppeared || shooter.Health.IsDead) return;

        if (visualRig == null || viewTarget == null) return;

        bool moving = movement != null && movement.IsRelocating;

        Vector3 direction = viewTarget.position - transform.position;

        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f) visualRig.rotation = Quaternion.LookRotation(direction);

        if (moving) step += Time.deltaTime * 9f;

        visualRig.localPosition = Vector3.up * (moving ? Mathf.Sin(step) * 0.025f : 0f);

        if (visor == null) return;

        bool aiming = shooter.IsAimingAt(Time.time);

        Color color = aiming ? new Color(1f, 0.12f, 0.04f) : new Color(1f, 0.55f, 0.15f);

        colors.SetColor("_Color", color);

        colors.SetColor("_EmissionColor", color * (aiming ? 1.5f : 0.5f));

        visor.SetPropertyBlock(colors);

    }

}

