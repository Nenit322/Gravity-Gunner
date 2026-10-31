// Presenter prepares values; the view never reads gameplay components.
public class HudViewData
{
    public GameState State;
    public int AliveEnemies;
    public int TotalEnemies;
    public bool CanGrab;
    public string Context = "";
    public float HitFlash;
    public bool IsEditor;
}
