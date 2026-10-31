using System;

// Plain game data: no Unity objects or UI dependencies.
public class HealthModel
{
    public int Maximum { get; private set; }
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;

    public event Action Changed;
    public event Action<int> Damaged;
    public event Action Died;

    public HealthModel(int maximum)
    {
        Maximum = Math.Max(1, maximum);
        Current = Maximum;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead) return;

        Current = Math.Max(0, Current - damage);
        Changed?.Invoke();
        Damaged?.Invoke(damage);

        if (IsDead) Died?.Invoke();
    }
}
