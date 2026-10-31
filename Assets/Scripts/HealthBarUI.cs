using UnityEngine;
using UnityEngine.UIElements;

// Passive health view. It receives numbers, never a Health component or model.
public class HealthBarUI : MonoBehaviour
{
    UIDocument document;
    public UIDocument Document => document;
    PanelSettings settings;
    Label label;
    VisualElement fill;
    VisualElement panel;

    public void Initialize()
    {
        if (document != null) return;
        settings = ScriptableObject.CreateInstance<PanelSettings>();
        settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        settings.referenceResolution = new Vector2Int(1280, 720);
        settings.sortingOrder = 100;
        settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("HealthTheme");
        document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = settings;
        var root = document.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        panel = new VisualElement { name = "PlayerHealthBar", pickingMode = PickingMode.Ignore };
        panel.style.position = Position.Absolute;
        panel.style.left = 24; panel.style.top = 24;
        panel.style.width = 266; panel.style.height = 72;
        panel.style.paddingLeft = 14; panel.style.paddingRight = 14;
        panel.style.paddingTop = 8;
        panel.style.backgroundColor = new Color(0.03f, 0.05f, 0.08f, 0.95f);

        label = new Label { pickingMode = PickingMode.Ignore };
        label.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.style.unityTextGenerator = TextGeneratorType.Standard;
        label.style.color = Color.white; label.style.fontSize = 16; label.style.height = 28;
        panel.Add(label);
        var track = new VisualElement { pickingMode = PickingMode.Ignore };
        track.style.height = 18;
        track.style.backgroundColor = new Color(0.25f, 0.04f, 0.06f);
        fill = new VisualElement { pickingMode = PickingMode.Ignore };
        fill.style.height = 18;
        fill.style.backgroundColor = new Color(0.95f, 0.12f, 0.2f);
        track.Add(fill); panel.Add(track); root.Add(panel);
    }

    public void SetHealth(int current, int maximum)
    {
        if (label == null) return;
        label.text = $"ЗДОРОВЬЕ  {current}/{maximum}";
        fill.style.width = Length.Percent(100f * current / Mathf.Max(1, maximum));
    }

    public void SetVisible(bool visible)
    {
        if (panel != null) panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void OnDestroy() { if (settings != null) Destroy(settings); }
}
