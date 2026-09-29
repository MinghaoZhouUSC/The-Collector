using UnityEngine;
using UnityEngine.UI;

// Display only: RoundManager owns all input and round transitions.
public sealed class RoundScreens : MonoBehaviour
{
    [SerializeField, Min(21)] private int sortingOrder = 30;
    [SerializeField] private string title = "THE COLLECTOR";
    [SerializeField] private string startPrompt = "Press Space to Start";
    [SerializeField] private string restartPrompt = "Press R to Restart";
    [SerializeField, TextArea(4, 12)] private string controls =
        "WASD / Arrow Keys  -  Move\nE  -  Pick up / Open vault\nB  -  Open / Close inventory\nUp / Down or W / S  -  Select item\nQ  -  Drop selected item\nF  -  Extract in safe house after reaching target";
    [SerializeField] private Color background = new Color(0.035f, 0.055f, 0.085f, 0.98f);
    [SerializeField] private Color accent = new Color(0.45f, 0.9f, 0.7f);
    [SerializeField] private Color failureColor = new Color(1f, 0.5f, 0.4f);
    [SerializeField, Min(16)] private int titleSize = 64;
    [SerializeField, Min(12)] private int bodySize = 30;
    private Canvas canvas;
    private GameObject startPanel;
    private GameObject endPanel;
    private Text goalText, resultTitle, reasonText, scoreText;

    private void Awake()
    {
        canvas = UIBuilder.CreateScreenCanvas("RoundScreensCanvas", transform, Mathf.Max(21, sortingOrder));
        startPanel = Panel("StartPanel");
        Label(startPanel, "Title", title, titleSize, 350f, 120f, accent);
        goalText = Label(startPanel, "Goal", "", bodySize, 210f, 120f, Color.white);
        Label(startPanel, "Controls", controls, bodySize, -30f, 330f, Color.white);
        Label(startPanel, "StartPrompt", startPrompt, bodySize + 6, -340f, 80f, accent);
        endPanel = Panel("EndPanel");
        resultTitle = Label(endPanel, "Result", "", titleSize, 240f, 120f, accent);
        reasonText = Label(endPanel, "Reason", "", bodySize, 70f, 160f, Color.white);
        scoreText = Label(endPanel, "Score", "", bodySize + 14, -100f, 100f, Color.white);
        Label(endPanel, "RestartPrompt", restartPrompt, bodySize + 6, -310f, 100f, accent);
        startPanel.SetActive(false);
        endPanel.SetActive(false);
    }

    // LateUpdate sees transitions after RoundManager.Update, including final scores.
    private void LateUpdate()
    {
        var round = RoundManager.Instance;
        bool ready = round != null && round.State == RoundManager.RoundState.Ready;
        bool ended = round != null && round.State == RoundManager.RoundState.Ended;
        startPanel.SetActive(ready);
        endPanel.SetActive(ended);
        if (ready)
            goalText.text = $"Bank ${round.TargetValue} and be in the safe house when time runs out.\nCarry more, move slower. Return to bank your loot.";
        if (!ended) return;
        resultTitle.text = round.Won ? "EXTRACTION SUCCESS" : "RUN FAILED";
        resultTitle.color = round.Won ? accent : failureColor;
        reasonText.text = round.Won ? "Target reached. You extracted safely." :
            !round.Extracted ? "You were outside the safe house when time ran out." :
            "You did not bank enough value.";
        scoreText.text = $"Final Score ${round.FinalScore} / ${round.TargetValue}";
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
