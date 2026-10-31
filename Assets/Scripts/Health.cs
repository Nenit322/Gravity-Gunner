using UnityEngine;

// Unity adapter preserves the combat API; pure health rules live in HealthModel.
public class Health : MonoBehaviour
{
    [SerializeField, Min(1)] int maximumHealth = 100;
    [SerializeField] bool showHealthLabel = true;

    public HealthModel Model { get; private set; }
    public int MaximumHealth => Model != null ? Model.Maximum : maximumHealth;
    public int CurrentHealth => Model != null ? Model.Current : 0;
    public bool IsDead => CurrentHealth <= 0;
    public event System.Action<int> Damaged;
    public event System.Action<Health> Died;

    void Awake()
    {
        Model = new HealthModel(maximumHealth);
        Model.Damaged += OnDamaged;
        Model.Died += OnDied;
    }

    public void TakeDamage(int damage) { Model.TakeDamage(damage); }

    void OnDamaged(int damage)
    {
        Damaged?.Invoke(damage);
        Debug.Log($"{name}: damage {damage}, health {CurrentHealth}/{MaximumHealth}", this);
    }

    void OnDied()
    {
        foreach (Collider shape in GetComponentsInChildren<Collider>()) shape.enabled = false;
        foreach (Renderer visual in GetComponentsInChildren<Renderer>()) visual.enabled = false;
        Died?.Invoke(this);
    }

    void OnGUI()
    {

        if (!showHealthLabel || Camera.main == null || IsDead) return;

        if (TryGetComponent<EnemyShooter>(out var shooter) && !shooter.HasAppeared) return;

        Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);

        if (point.z > 0f) GUI.Label(new Rect(point.x - 65, Screen.height - point.y, 180, 25), $"{name}: {CurrentHealth}/{maximumHealth}");

    }

}

