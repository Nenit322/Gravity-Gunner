using System;
using UnityEngine;
using UnityEngine.UIElements;

// Passive MVP view: labels, layout and button events only.
public class GameHud : MonoBehaviour
{
    public event Action ResumeRequested;
    public event Action RestartRequested;
    public event Action DisplayRequested;
    public event Action QuitRequested;

    public UIDocument Document { get; private set; }
    Label enemyCount;
    Label context;
    Label menuTitle;
    Label menuMessage;
    VisualElement playing;
    VisualElement menu;
    VisualElement hitOverlay;
    VisualElement crossHorizontal;
    VisualElement crossVertical;
    Button resume;
    Button quit;

    public void Initialize(UIDocument sharedDocument)
    {
        if (Document != null) return;
        Document = sharedDocument;
        var root = Document.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        root.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        root.style.unityTextGenerator = TextGeneratorType.Standard;
        root.style.color = Color.white;

        playing = new VisualElement { name = "PlayingHud", pickingMode = PickingMode.Ignore };
        Stretch(playing);
        root.Add(playing);

        var goal = new VisualElement { pickingMode = PickingMode.Ignore };
        goal.style.position = Position.Absolute;
        goal.style.right = 24; goal.style.top = 24; goal.style.width = 260;
        goal.style.paddingLeft = 16; goal.style.paddingRight = 16;
        goal.style.paddingTop = 10; goal.style.paddingBottom = 10;
        goal.style.backgroundColor = new Color(0.03f, 0.05f, 0.08f, 0.93f);
        goal.Add(Text("ЦЕЛЬ: ОЧИСТИТЬ АРЕНУ", 13));
        enemyCount = Text("", 20); enemyCount.name = "EnemyCounter";
        goal.Add(enemyCount); playing.Add(goal);

        var cross = new VisualElement { pickingMode = PickingMode.Ignore };
        cross.style.position = Position.Absolute;
        cross.style.left = Length.Percent(50); cross.style.top = Length.Percent(50);
        cross.style.marginLeft = -6; cross.style.marginTop = -6;
        cross.style.width = 12; cross.style.height = 12;
        crossHorizontal = Bar(0, 5, 12, 2);
        crossVertical = Bar(5, 0, 2, 12);
        cross.Add(crossHorizontal); cross.Add(crossVertical); playing.Add(cross);

        context = Text("", 18); context.name = "ContextHint";
        context.style.position = Position.Absolute;
        context.style.left = Length.Percent(20); context.style.right = Length.Percent(20);
        context.style.bottom = 106;
        context.style.whiteSpace = WhiteSpace.Normal;
        context.style.unityTextAlign = TextAnchor.MiddleCenter;
        context.style.paddingTop = 10; context.style.paddingBottom = 10;
        context.style.backgroundColor = new Color(0.03f, 0.05f, 0.08f, 0.9f);
        playing.Add(context);

        var controls = Text("WASD — движение    E — захват    ЛКМ — бросок    G — щит    Esc — пауза    R — заново", 14);
        controls.style.position = Position.Absolute;
        controls.style.left = 24; controls.style.right = 24; controls.style.bottom = 22;
        controls.style.unityTextAlign = TextAnchor.MiddleCenter;
        controls.style.whiteSpace = WhiteSpace.Normal;
        controls.style.paddingTop = 10; controls.style.paddingBottom = 10;
        controls.style.backgroundColor = new Color(0.03f, 0.05f, 0.08f, 0.9f);
        playing.Add(controls);

        menu = new VisualElement { name = "RoundMenu" };
        Stretch(menu);
        menu.style.backgroundColor = new Color(0.01f, 0.02f, 0.04f, 0.9f);
        menu.style.alignItems = Align.Center;
        menu.style.justifyContent = Justify.Center;
        root.Add(menu);

        var card = new VisualElement();
        card.style.width = 440;
        card.style.paddingLeft = 28; card.style.paddingRight = 28;
        card.style.paddingTop = 24; card.style.paddingBottom = 24;
        card.style.backgroundColor = new Color(0.06f, 0.09f, 0.13f, 1f);
        menu.Add(card);
        var brand = Text("GRAVITY GUNNER", 13);
        brand.style.color = new Color(0.2f, 0.85f, 0.9f);
        brand.style.unityTextAlign = TextAnchor.MiddleCenter;
        card.Add(brand);
        menuTitle = Text("", 30); menuTitle.name = "MenuTitle";
        menuTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        menuTitle.style.marginTop = 10; card.Add(menuTitle);
        menuMessage = Text("", 16); menuMessage.style.whiteSpace = WhiteSpace.Normal;
        menuMessage.style.unityTextAlign = TextAnchor.MiddleCenter;
        menuMessage.style.marginTop = 8; menuMessage.style.marginBottom = 16;
        card.Add(menuMessage);
        resume = ActionButton("Продолжить", "ResumeButton", () => ResumeRequested?.Invoke());
        card.Add(resume);
        card.Add(ActionButton("Начать заново", "RestartButton", () => RestartRequested?.Invoke()));
        card.Add(ActionButton("Полный экран / окно", "DisplayButton", () => DisplayRequested?.Invoke()));
        quit = ActionButton("Выйти из игры", "QuitButton", () => QuitRequested?.Invoke());
        card.Add(quit);
        hitOverlay = new VisualElement { name = "HitFlash", pickingMode = PickingMode.Ignore };
        Stretch(hitOverlay); root.Add(hitOverlay);
    }

    public void Render(HudViewData data)
    {
        if (Document == null) return;
        bool isPlaying = data.State == GameState.Playing;
        playing.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
        menu.style.display = isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
        enemyCount.text = $"Осталось врагов: {data.AliveEnemies}/{data.TotalEnemies}";
        context.text = data.Context;
        context.style.display = string.IsNullOrEmpty(data.Context) ? DisplayStyle.None : DisplayStyle.Flex;
        Color aimColor = data.CanGrab ? Color.cyan : Color.white;
        crossHorizontal.style.backgroundColor = aimColor;
        crossVertical.style.backgroundColor = aimColor;
        resume.style.display = data.State == GameState.Paused ? DisplayStyle.Flex : DisplayStyle.None;
        quit.SetEnabled(!data.IsEditor);
        quit.tooltip = data.IsEditor ? "Для остановки в редакторе нажмите Play ещё раз" : "Закрыть игру";
        if (data.State == GameState.Won)
        {
            menuTitle.text = "АРЕНА ЗАЧИЩЕНА";
            menuMessage.text = "Все враги уничтожены. Можно начать новый бой.";
        }
        else if (data.State == GameState.Lost)
        {
            menuTitle.text = "ПОРАЖЕНИЕ";
            menuMessage.text = "Здоровье закончилось. Попробуйте снова.";
        }
        else
        {
            menuTitle.text = "ПАУЗА";
            menuMessage.text = "Бой остановлен. Esc — продолжить.";
        }
        hitOverlay.style.backgroundColor = new Color(0.8f, 0f, 0f, data.HitFlash * 0.65f);
    }

    static Label Text(string value, int size)
    {
        var label = new Label(value) { pickingMode = PickingMode.Ignore };
        label.style.fontSize = size; label.style.color = Color.white;
        return label;
    }

    static VisualElement Bar(float left, float top, float width, float height)
    {
        var bar = new VisualElement { pickingMode = PickingMode.Ignore };
        bar.style.position = Position.Absolute;
        bar.style.left = left; bar.style.top = top; bar.style.width = width; bar.style.height = height;
        return bar;
    }

    static Button ActionButton(string text, string name, Action action)
    {
        var button = new Button(action) { text = text, name = name };
        button.style.height = 42; button.style.marginTop = 8;
        button.style.fontSize = 18;
        button.style.backgroundColor = new Color(0.12f, 0.2f, 0.27f);
        button.style.color = Color.white;
        return button;
    }

    static void Stretch(VisualElement element)
    {
        element.style.position = Position.Absolute;
        element.style.left = 0; element.style.right = 0; element.style.top = 0; element.style.bottom = 0;
    }

}
