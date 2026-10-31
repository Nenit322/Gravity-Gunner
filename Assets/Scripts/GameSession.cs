using UnityEngine;

using UnityEngine.SceneManagement;



public class GameSession : MonoBehaviour
{

    [SerializeField] Health playerHealth;

    [SerializeField] EnemyShooter[] enemies = new EnemyShooter[0];

    readonly RoundModel round = new RoundModel();

    public RoundModel Model => round;

    public GameState State => round.State;

    public bool CanPlay => State == GameState.Playing && Cursor.lockState == CursorLockMode.Locked;

    void Awake()
    {

        SetState(GameState.Playing);

    }

    void Start()
    {

        if (Application.isPlaying && playerHealth != null)
        {
            var presenter = GetComponent<GameUiPresenter>();
            if (presenter == null) presenter = gameObject.AddComponent<GameUiPresenter>();
            presenter.Bind(this, playerHealth, enemies);
        }

        if (playerHealth != null) playerHealth.Died += OnPlayerDied;

        foreach (EnemyShooter enemy in enemies)
            if (enemy != null) enemy.Health.Died += OnEnemyDied;

    }

    void OnPlayerDied(Health health)
    {

        SetState(GameState.Lost);

    }

    void OnEnemyDied(Health health)
    {

        if (State == GameState.Lost || enemies.Length == 0) return;

        foreach (EnemyShooter enemy in enemies)
            if (enemy != null && !enemy.Health.IsDead) return;

        SetState(GameState.Won);

    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (State == GameState.Playing)
            {

                SetState(GameState.Paused);

            }

            else if (State == GameState.Paused)
            {

                SetState(GameState.Playing);

            }

        }

        if (Input.GetKeyDown(KeyCode.R))
        {

            Restart();

        }

    }

    public void SetState(GameState state)
    {

        if ((State == GameState.Won || State == GameState.Lost) && state != State) return;

        if (state != State && (state == GameState.Won || state == GameState.Lost))
            GameAudio.PlayAt(state == GameState.Won ? "won" : "lost", Camera.main != null ? Camera.main.transform.position : transform.position);

        round.SetState(state);

        if ((state == GameState.Won || state == GameState.Lost) && playerHealth != null)
            playerHealth.GetComponent<GravityGrabber>()?.Release();

        Time.timeScale = state == GameState.Playing ? 1f : 0f;

        Cursor.lockState = state == GameState.Playing ? CursorLockMode.Locked : CursorLockMode.None;

        Cursor.visible = state != GameState.Playing;

    }

    public void Restart()
    {

        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    void OnApplicationFocus(bool focused)
    {

        if (!focused && State == GameState.Playing)
        {

            SetState(GameState.Paused);

        }

    }

    void OnDestroy()
    {

        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;

        foreach (EnemyShooter enemy in enemies)
            if (enemy != null) enemy.Health.Died -= OnEnemyDied;

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;

        Cursor.visible = true;

    }

}

