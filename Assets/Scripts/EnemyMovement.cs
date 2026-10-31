using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(EnemyShooter))]
public class EnemyMovement : MonoBehaviour
{

    [SerializeField] GameSession session;

    [SerializeField] Transform[] positions;

    [SerializeField, Min(0.1f)] float speed = 1.5f;

    [SerializeField, Min(0.1f)] float lostSightDelay = 2f;

    Rigidbody body;

    EnemyShooter shooter;

    readonly Queue<Vector3> route = new Queue<Vector3>();

    float unseenTime;

    float blockedTime;

    int patrolIndex;

    public bool IsRelocating { get; private set; }
    public int RemainingWaypoints => route.Count;

    void Awake()
    {

        body = GetComponent<Rigidbody>(); shooter = GetComponent<EnemyShooter>();

        body.isKinematic = true; body.useGravity = false;

        body.interpolation = RigidbodyInterpolation.Interpolate;

        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

    }

    void FixedUpdate() { if (session.CanPlay) TickMovement(Time.fixedDeltaTime, Time.time); }
    public void TickMovement(float step, float time)
    {

        IsRelocating = false;

        if (session.State != GameState.Playing || shooter.Health.IsDead || !shooter.HasAppeared) return;

        bool visible = shooter.HasPlayerSightFrom(body.position);

        unseenTime = visible ? 0f : unseenTime + step;

        if (!shooter.CanRepositionAt(time)) return;

        if (unseenTime >= lostSightDelay)
        {

            PlanRoute(true);

            unseenTime = 0f;

        }

        if (route.Count == 0) PlanRoute(false);

        if (route.Count == 0) return;

        Vector3 target = route.Peek();

        if (Vector3.Distance(body.position, target) < 0.12f) { route.Dequeue(); return; }
        Vector3 delta = Vector3.ClampMagnitude(target - body.position, speed * step);

        float travel = AllowedTravel(body.position, delta);

        if (travel > 0.001f)
        {

            body.MovePosition(body.position + delta.normalized * travel);

            IsRelocating = true; blockedTime = 0f;

        }

        else
        {

            blockedTime += step;

            if (blockedTime > 0.6f) { route.Clear(); blockedTime = 0f; patrolIndex++; }
        }

    }

    float AllowedTravel(Vector3 from, Vector3 delta)
    {

        float length = delta.magnitude;

        if (length < 0.0001f) return 0f;

        // A small skin above the floor avoids treating ground contact as an obstacle.
        foreach (RaycastHit hit in Physics.CapsuleCastAll(from + Vector3.down * 0.5f, from + Vector3.up * 0.5f,
                     0.48f, delta.normalized, length + 0.04f, ~0, QueryTriggerInteraction.Ignore))
        {

            if (hit.collider.transform.IsChildOf(transform)) continue;

            length = Mathf.Min(length, Mathf.Max(0f, hit.distance - 0.04f));

        }

        return length;

    }

    public bool CanTravel(Vector3 from, Vector3 to)
    {

        Vector3 delta = to - from;

        if (AllowedTravel(from, delta) < delta.magnitude - 0.01f) return false;

        foreach (Collider shape in Physics.OverlapCapsule(to + Vector3.down * 0.5f, to + Vector3.up * 0.5f,
                     0.48f, ~0, QueryTriggerInteraction.Ignore))
            if (!shape.transform.IsChildOf(transform)) return false;

        return true;

    }

    public bool PlanRoute(bool seekSight)
    {

        if (positions == null || positions.Length == 0) return false;

        int n = positions.Length;

        int[] parent = new int[n];

        for (int i = 0; i < n; i++) parent[i] = -2;

        var search = new Queue<int>();

        for (int i = 0; i < n; i++)
            if (positions[i] != null && CanTravel(body.position, positions[i].position))
            { parent[i] = -1; search.Enqueue(i); }
        // Prepared points form a tiny graph. Test every edge against the actual scene geometry.
        while (search.Count > 0)
        {

            int current = search.Dequeue();

            for (int i = 0; i < n; i++)
                if (parent[i] == -2 && positions[i] != null && CanTravel(positions[current].position, positions[i].position))
                { parent[i] = current; search.Enqueue(i); }
        }

        int goal = -1;

        float nearest = float.PositiveInfinity;

        for (int offset = 0; offset < n; offset++)
        {

            int i = (patrolIndex + offset) % n;

            if (parent[i] == -2 || positions[i] == null) continue;

            float distance = Vector3.Distance(body.position, positions[i].position);

            if (distance < 0.8f) continue;

            if (seekSight)
            {

                if (!shooter.HasPlayerSightFrom(positions[i].position) || distance >= nearest) continue;

                goal = i; nearest = distance;

            }

            else if (distance <= 3f) { goal = i; break; }
        }

        if (goal < 0) return false;

        var path = new Stack<Vector3>();

        for (int node = goal; node >= 0; node = parent[node]) path.Push(positions[node].position);

        route.Clear(); while (path.Count > 0) route.Enqueue(path.Pop());

        patrolIndex = (goal + 1) % n;

        return true;

    }

}

