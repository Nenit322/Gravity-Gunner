using UnityEngine;

// MVP mediator: gameplay -> plain models -> view; view events -> gameplay commands.
public class GameUiPresenter : MonoBehaviour
{
    GameSession session;
    Health player;
    HealthModel healthModel;
    RoundModel roundModel;
    GravityGrabber grabber;
    EnemyShooter[] enemies;
    GameHud hud;
    HealthBarUI healthBar;
    HudViewData data = new HudViewData();
    float hitFlash;
    bool bound;

    public void Bind(GameSession game, Health playerHealth, EnemyShooter[] actors)
    {
        Unbind();
        session = game;
        player = playerHealth;
        healthModel = player.Model;
        roundModel = session.Model;
        grabber = player.GetComponent<GravityGrabber>();
        enemies = actors;

        hud = GetComponent<GameHud>();
        if (hud == null) hud = gameObject.AddComponent<GameHud>();
        healthBar = GetComponent<HealthBarUI>();
        if (healthBar == null) healthBar = gameObject.AddComponent<HealthBarUI>();

        healthBar.Initialize();
        hud.Initialize(healthBar.Document);

        hud.ResumeRequested += Resume;
        hud.RestartRequested += Restart;
        hud.DisplayRequested += ToggleDisplay;
        hud.QuitRequested += Quit;
        healthModel.Changed += RefreshHealth;
        healthModel.Damaged += OnDamaged;
        roundModel.Changed += OnRoundChanged;
        bound = true;
        RenderNow();
    }

    void Update()
    {
        if (!bound) return;
        hitFlash = Mathf.Max(0f, hitFlash - Time.unscaledDeltaTime);
        RenderNow();
    }

    public void RenderNow()
    {
        if (!bound) return;
        data.State = roundModel.State;
        data.TotalEnemies = enemies.Length;
        data.AliveEnemies = 0;
        foreach (EnemyShooter enemy in enemies)
            if (enemy != null && !enemy.Health.IsDead) data.AliveEnemies++;

        data.CanGrab = grabber != null && grabber.AvailableBox != null;
        data.Context = "";
        if (grabber != null && grabber.HeldBox != null)
        {
            GrabbableBox held = grabber.HeldBox;
            if (held.GetComponent<PocketShieldItem>() != null)
                data.Context = "КАРМАННЫЙ ЩИТ\nG — убрать   •   E — перехватить предмет";
            else if (held.TryGetComponent<Grenade>(out var grenade))
                data.Context = $"ГРАНАТА: {grenade.RemainingTime:0.0} с\nЛКМ — вернуть   •   Запал продолжает идти!";
            else
                data.Context = $"ЯЩИК: {held.Body.mass:0} кг\nЛКМ — бросить   •   E — отпустить";
        }
        else if (grabber != null && grabber.AvailableBox != null)
        {
            data.Context = grabber.AvailableBox.GetComponent<Grenade>() != null
                ? "E — ПЕРЕХВАТИТЬ ГРАНАТУ"
                : $"E — взять ящик {grabber.AvailableBox.Body.mass:0} кг";
        }

        data.HitFlash = hitFlash;
        data.IsEditor = Application.isEditor;
        hud.Render(data);
        RefreshHealth();
    }

    void RefreshHealth()
    {
        if (healthModel == null || healthBar == null) return;
        healthBar.SetHealth(healthModel.Current, healthModel.Maximum);
        healthBar.SetVisible(roundModel.State == GameState.Playing);
    }

    void OnRoundChanged() { RenderNow(); }

    void OnDamaged(int damage)
    {
        hitFlash = 0.2f;
        if (Camera.main != null) GameAudio.PlayAt("hit", Camera.main.transform.position);
    }

    void Resume()
    {
        if (roundModel.State == GameState.Paused) session.SetState(GameState.Playing);
    }

    void Restart() { session.Restart(); }
    void ToggleDisplay() { session.GetComponent<DisplaySettings>()?.Toggle(); }
    void Quit() { if (!Application.isEditor) Application.Quit(); }

    void Unbind()
    {
        if (!bound) return;
        if (hud != null)
        {
            hud.ResumeRequested -= Resume;
            hud.RestartRequested -= Restart;
            hud.DisplayRequested -= ToggleDisplay;
            hud.QuitRequested -= Quit;
        }
        healthModel.Changed -= RefreshHealth;
        healthModel.Damaged -= OnDamaged;
        roundModel.Changed -= OnRoundChanged;
        bound = false;
    }

    void OnDestroy() { Unbind(); }
}
