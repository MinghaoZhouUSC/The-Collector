using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Menu buttons request transitions through RoundManager; it owns round state.
public sealed class RoundScreens : MonoBehaviour
{
    [SerializeField, Min(21)] private int sortingOrder = 30;
    [SerializeField] private string title = "THE COLLECTOR";
    [SerializeField] private Color background = new Color(0.035f, 0.055f, 0.085f, 0.98f);
    [SerializeField] private Color accent = new Color(0.45f, 0.9f, 0.7f);
    [SerializeField] private Color failureColor = new Color(1f, 0.5f, 0.4f);
    [SerializeField, Min(16)] private int titleSize = 64;
    [SerializeField, Min(12)] private int bodySize = 30;
    private Canvas canvas;
    private GameObject startPanel;
    private GameObject endPanel;
    private Text resultTitle, reasonText, scoreText, goalText;
    private RoundManager displayedRound;
    private RoundManager.RoundState? displayedState;

    private void Awake()
    {
        canvas = UIBuilder.CreateScreenCanvas("RoundScreensCanvas", transform, Mathf.Max(21, sortingOrder));
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("MenuEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        if (GetComponent<TutorialGuide>() == null) gameObject.AddComponent<TutorialGuide>();
        startPanel = Panel("StartPanel");
        // Keep the menu opaque so the map and gameplay HUD do not compete with it.
        startPanel.GetComponent<Image>().color = new Color(background.r, background.g, background.b, 1f);
        Label(startPanel, "Title", title, titleSize, 280f, 100f, accent).fontStyle = FontStyle.Bold;
        // Filled in LateUpdate from RoundManager, so the goal always matches the round settings.
        goalText = Label(startPanel, "Goal", "", bodySize, 185f, 50f, Color.white);
        Label(startPanel, "MergeTip", "Tip: 3 matching items merge into one lighter, more valuable item.", 24, 135f, 40f, new Color(.55f, .60f, .68f));
        LevelCard("Tutorial", 0f, true);
        Label(startPanel, "StartHint", "Click PLAY or press Space to start", 24, -215f, 40f, new Color(.55f, .60f, .68f));
        endPanel = Panel("EndPanel");
        resultTitle = Label(endPanel, "Result", "", titleSize, 240f, 120f, accent);
        resultTitle.fontStyle = FontStyle.Bold;
        reasonText = Label(endPanel, "Reason", "", bodySize, 70f, 160f, UIBuilder.SoftText);
        scoreText = Label(endPanel, "Score", "", bodySize + 14, -100f, 100f, UIBuilder.Gold);
        scoreText.fontStyle = FontStyle.Bold;
        MenuButton(endPanel.transform, "LevelSelectButton", "BACK TO LEVEL SELECT", new Vector2(0f, -280f), new Vector2(650f, 85f),
            () => { if (RoundManager.Instance != null) RoundManager.Instance.ReturnToLevelSelect(); });
        Label(endPanel, "RestartPrompt", "Or press R to return to level select", 25, -380f, 60f, Color.white);
        startPanel.SetActive(false);
        endPanel.SetActive(false);
    }

    // LateUpdate sees transitions after RoundManager.Update, including final scores.
    private void LateUpdate()
    {
        var round = RoundManager.Instance;
        // These panels contain round snapshots, so refresh only on a transition.
        if (round == displayedRound && (round == null || displayedState == round.State)) return;
        displayedRound = round;
        displayedState = round != null ? round.State : (RoundManager.RoundState?)null;
        bool ready = round != null && round.State == RoundManager.RoundState.Ready;
        bool ended = round != null && round.State == RoundManager.RoundState.Ended;
        startPanel.SetActive(ready);
        endPanel.SetActive(ended);
        if (ready)
            goalText.text = $"Bank ${round.TargetValue} in {FormatDuration(round.TimeRemaining)} and be inside the safe house when time runs out.";
        if (!ended) return;
        resultTitle.text = round.Won ? "EXTRACTION SUCCESS" : "RUN FAILED";
        resultTitle.color = round.Won ? accent : failureColor;
        reasonText.text = round.Won ? "Target reached. You extracted safely." :
            !round.Extracted ? "Time ran out outside the safe house.\nReturn earlier or drop heavy loot to move faster." :
            $"You needed ${Mathf.Max(0, round.TargetValue - round.FinalScore)} more.\nOnly loot deposited in the safe house counts.";
        scoreText.text = $"Final Score ${round.FinalScore} / ${round.TargetValue}";
    }

    // 120 -> "2 minutes", 90 -> "1:30".
    private static string FormatDuration(float seconds)
    {
        int total = Mathf.RoundToInt(seconds);
        if (total % 60 != 0) return $"{total / 60}:{total % 60:00}";
        return total == 60 ? "1 minute" : $"{total / 60} minutes";
    }

    private void LevelCard(string name, float x, bool available)
    {
        // Rounded card with a thin accent border: border panel underneath, card inset by 2px.
        var border = UIBuilder.CreatePanel(startPanel.transform, name + "Card",
            available ? new Color(accent.r, accent.g, accent.b, 0.55f) : new Color(1f, 1f, 1f, 0.1f), 22f);
        UIBuilder.Place(border.rectTransform, new Vector2(.5f, .5f), new Vector2(x, -35f), new Vector2(380f, 260f));
        var image = UIBuilder.CreatePanel(border.transform, "Inner",
            available ? new Color(0.07f, 0.17f, 0.16f) : new Color(0.09f, 0.12f, 0.17f), 20f);
        UIBuilder.Stretch(image.rectTransform);
        image.rectTransform.offsetMin = new Vector2(2f, 2f);
        image.rectTransform.offsetMax = new Vector2(-2f, -2f);
        var color = available ? accent : new Color(.55f, .60f, .68f);
        var heading = UIBuilder.CreateText(image.transform, "Name", name.ToUpperInvariant(), 36, TextAnchor.MiddleCenter, color, false);
        heading.fontStyle = FontStyle.Bold;
        UIBuilder.Place(heading.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 45), new Vector2(350, 60));
        if (available)
            MenuButton(image.transform, "StartTutorial", "PLAY", new Vector2(0, -60), new Vector2(270, 65),
                () => { if (RoundManager.Instance != null) RoundManager.Instance.StartRound(); });
        else
        {
            var locked = UIBuilder.CreateText(image.transform, "Unavailable", "COMING SOON", 21, TextAnchor.MiddleCenter, color, false);
            UIBuilder.Place(locked.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -60), new Vector2(350, 65));
        }
    }

    private void MenuButton(Transform parent, string name, string caption, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        // Pill-shaped button: corner radius is half the height.
        var image = UIBuilder.CreatePanel(parent, name, accent, size.y * 0.5f);
        UIBuilder.Place(image.rectTransform, new Vector2(.5f, .5f), position, size);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        var text = UIBuilder.CreateText(image.transform, "Caption", caption, 27, TextAnchor.MiddleCenter, UIBuilder.DarkText, false);
        text.fontStyle = FontStyle.Bold;
        UIBuilder.Stretch(text.rectTransform);
    }

    private GameObject Panel(string name)
    {
        var image = UIBuilder.CreateImage(canvas.transform, name, background);
        UIBuilder.Stretch(image.rectTransform);
        return image.gameObject;
    }

    private Text Label(GameObject parent, string name, string value, int size, float y, float height, Color color)
    {
        var text = UIBuilder.CreateText(parent.transform, name, value, size, TextAnchor.MiddleCenter, color, false);
        UIBuilder.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1600f, height));
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        return text;
    }

    private void OnDisable()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);
    }
}
