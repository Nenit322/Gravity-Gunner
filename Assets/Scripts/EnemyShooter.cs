using UnityEngine;

[RequireComponent(typeof(Health), typeof(LineRenderer))]
public class EnemyShooter : MonoBehaviour
{

    [SerializeField] GameSession session;

    [SerializeField] Health playerHealth;

    [SerializeField] Transform aimTarget;

    [SerializeField, Min(0.1f)] float fireInterval = 3f;

    [SerializeField, Min(0f)] float initialDelay = 4f;

    [SerializeField, Range(0.1f, 3f)] float aimDuration = 1.5f;

    [SerializeField, Min(1)] int shotDamage = 10;

    [SerializeField, Min(1f)] float range = 30f;

    [SerializeField] LayerMask shotMask = ~0; // Includes player layer 2 (Ignore Raycast).
    [SerializeField] GameObject grenadePrefab;

    [SerializeField, Min(0.2f)] float grenadeFlightTime = 0.9f;

    [SerializeField, Min(0f)] float spawnDelay;

    [SerializeField, Min(0f)] float resumeOffset;

    float spawnTime;

    bool appeared;

    [SerializeField, Range(0f, 100f)] float initialGrenadeChance = 15f;

    [SerializeField, Range(0f, 100f)] float grenadeChanceIncrease = 10f;

    public float CurrentGrenadeChance { get; private set; } = 15f;

    GUIStyle warningStyle;

    public bool HasAppeared => appeared;

    int attackCount;

    Grenade activeGrenade;

    bool waitingForGrenade;

    public int AttackCount => attackCount;

    public bool WaitingForGrenade => waitingForGrenade;

    LineRenderer tracer;

    bool sightWasBlocked;

    float lastAttackTime = float.NegativeInfinity;

    public bool CanRepositionAt(float time) => appeared && !Health.IsDead && !waitingForGrenade && (!HasPlayerSightFrom(transform.position) || time < nextShotTime - aimDuration) && time >= lastAttackTime + 0.5f;

    public bool HasPlayerSightFrom(Vector3 position)
    {

        if (aimTarget == null) return false;

        Vector3 origin = position + Vector3.up * 0.15f + transform.right * 0.35f;

        Vector3 line = aimTarget.position - Vector3.up * 0.25f - origin;

        if (line.magnitude > range) return false;

        foreach (RaycastHit hit in Physics.RaycastAll(origin, line.normalized, line.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {

            if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<GrabbableBox>() != null || hit.collider.GetComponentInParent<EnemyShooter>() != null) continue;

            if (hit.collider.GetComponentInParent<Health>() != playerHealth) return false;

        }

        return true;

    }

    float nextShotTime;

    float tracerEndTime;

    public Health Health => GetComponent<Health>();

    void Awake()
    {

        tracer = GetComponent<LineRenderer>();

        tracer.useWorldSpace = true; tracer.positionCount = 2; tracer.alignment = LineAlignment.View;

        tracer.enabled = false;

        CurrentGrenadeChance = initialGrenadeChance;

        spawnTime = Time.time + spawnDelay;

        appeared = spawnDelay <= 0f;

        SetAppearance(appeared);

        nextShotTime = spawnTime + initialDelay;

    }

    public bool IsAimingAt(float time) => !waitingForGrenade && HasPlayerSightFrom(transform.position) && time >= spawnTime && time >= nextShotTime - aimDuration && time < nextShotTime;

    void Update()
    {

        if (Health.IsDead || playerHealth.IsDead || session.State == GameState.Won || session.State == GameState.Lost)
        { tracer.enabled = false; return; }
        if (!session.CanPlay) return;

        TickAttack(Time.time);

    }

    public void TickAttack(float time)
    {

        if (session.State != GameState.Playing || Health.IsDead || playerHealth.IsDead) return;

        if (!appeared)
        {

            if (time < spawnTime) return;

            appeared = true; SetAppearance(true);

        }

        if (waitingForGrenade)
        {

            tracer.enabled = false;

            if (activeGrenade != null && !activeGrenade.IsDetonated) return;

            // No attacks or counter changes while the grenade exists, even when it is held.
            activeGrenade = null;

            waitingForGrenade = false;

            attackCount = 0;

            float interval = Mathf.Max(fireInterval, aimDuration + 0.1f);

            float anchor = spawnTime - spawnDelay + initialDelay + resumeOffset;

            nextShotTime = anchor + Mathf.Ceil((time + interval - anchor) / interval) * interval;

            return;

        }

        if (!HasPlayerSightFrom(transform.position))
        { tracer.enabled = false; sightWasBlocked = true; return; }
        if (sightWasBlocked)
        { nextShotTime = Mathf.Max(nextShotTime, time + aimDuration); sightWasBlocked = false; }
        Vector3 origin = transform.position + Vector3.up * 0.15f + transform.right * 0.35f;

        Vector3 direction = (aimTarget.position - Vector3.up * 0.25f - origin).normalized;

        Vector3 end = origin + direction * range;

        bool hasHit = TraceShot(origin, direction, out RaycastHit hit);

        if (hasHit) end = hit.point;

        if (time >= nextShotTime)
        {

            nextShotTime = time + Mathf.Max(fireInterval, aimDuration + 0.1f);

            ShowTrace(origin, end, new Color(1f, 0.8f, 0.2f), 0.055f);

            tracerEndTime = time + 0.12f;

            lastAttackTime = time;

            attackCount++;

            if (RollGrenade(Random.Range(0f, 100f)))
            {

                tracer.enabled = false;

                LaunchGrenade(origin, direction);

            }

            else
            {

                GameAudio.PlayAt("shot", origin);

                if (hasHit && hit.collider.GetComponentInParent<Health>() == playerHealth) playerHealth.TakeDamage(shotDamage);

            }

        }

        else if (IsAimingAt(time)) ShowTrace(origin, end, Color.red, 0.065f);

        else if (time >= tracerEndTime) tracer.enabled = false;

    }

    void LaunchGrenade(Vector3 origin, Vector3 direction)
    {

        // Resource fallback also lets older saved arenas use the updated attack pattern.
        GameObject prefab = grenadePrefab != null ? grenadePrefab : Resources.Load<GameObject>("Prefabs/Grenade");

        if (prefab == null) { Debug.LogError("Grenade prefab is missing", this); return; }
        var projectile = Instantiate(prefab, origin + direction * 0.9f, Quaternion.identity);

        var grenade = projectile.GetComponent<Grenade>();

        grenade.Initialize(session, playerHealth, transform);

        activeGrenade = grenade;

        waitingForGrenade = true;

        Vector3 destination = aimTarget.position - Vector3.up * 0.8f;

        Vector3 velocity = (destination - projectile.transform.position) / grenadeFlightTime - 0.5f * Physics.gravity * grenadeFlightTime;

        projectile.GetComponent<Rigidbody>().linearVelocity = velocity;

        GameAudio.PlayAt("throw", origin);

    }

    public bool RollGrenade(float rollPercent)
    {

        if (CurrentGrenadeChance >= 100f || rollPercent < CurrentGrenadeChance)
        { CurrentGrenadeChance = initialGrenadeChance; return true; }
        CurrentGrenadeChance = Mathf.Min(100f, CurrentGrenadeChance + grenadeChanceIncrease);

        return false;

    }

    void OnGUI()
    {

        if (Camera.main == null || !appeared || Health.IsDead || session == null || session.State != GameState.Playing || !IsAimingAt(Time.time)) return;

        Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.35f);

        if (point.z <= 0f) return;

        if (warningStyle == null) warningStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        Color old = GUI.color;

        GUI.color = new Color(0.05f, 0f, 0f, 0.85f);

        GUI.DrawTexture(new Rect(point.x - 90, Screen.height - point.y - 32, 180, 30), Texture2D.whiteTexture);

        GUI.color = Color.red;

        GUI.Label(new Rect(point.x - 90, Screen.height - point.y - 32, 180, 30), $"АТАКА  {Mathf.Max(0f, nextShotTime - Time.time):0.0} с", warningStyle);

        GUI.color = old;

    }

    void SetAppearance(bool visible)
    {

        foreach (Renderer visual in GetComponentsInChildren<Renderer>())
            if (visual != tracer) visual.enabled = visible;

        foreach (Collider shape in GetComponentsInChildren<Collider>()) shape.enabled = visible;

        tracer.enabled = false;

    }

    void ShowTrace(Vector3 origin, Vector3 end, Color color, float width)
    {

        tracer.startColor = tracer.endColor = color;

        tracer.widthMultiplier = width;

        tracer.SetPosition(0, origin); tracer.SetPosition(1, end); tracer.enabled = true;

    }

    public bool TraceShot(Vector3 origin, Vector3 direction, out RaycastHit closest)
    {

        closest = default;

        bool found = false;

        float nearest = range;

        // Filter self explicitly: the muzzle can be inside the enemy's collider.
        foreach (RaycastHit hit in Physics.RaycastAll(origin, direction, range, shotMask, QueryTriggerInteraction.Ignore))
        {

            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;

            nearest = hit.distance; closest = hit; found = true;

        }

        return found;

    }

}

