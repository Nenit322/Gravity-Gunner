using System;

public enum GameState { Playing, Paused, Won, Lost }

public class RoundModel
{
    public GameState State { get; private set; } = GameState.Playing;
    public event Action Changed;

    public bool SetState(GameState state)
    {
        if ((State == GameState.Won || State == GameState.Lost) && state != State)
            return false;

        if (State == state) return true;

        State = state;
        Changed?.Invoke();
        return true;
    }
}
