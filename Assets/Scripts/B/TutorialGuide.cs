using UnityEngine;
using UnityEngine.UI;

// Contextual coaching observes real actions without pausing or blocking the round.
public sealed class TutorialGuide : MonoBehaviour
{
    private Canvas canvas;
    private Text heading, instruction;
    private PlayerInventory inventory;
    private PlayerState player;
    private Vector3 startPosition;
    private bool initialized, moved, collected, inspected, banked;
    private string shown;

    private void Awake()
    {
        canvas = UIBuilder.CreateScreenCanvas("TutorialGuideCanvas", transform, 25);
        var panel = UIBuilder.CreateImage(canvas.transform, "Guide", new Color(.035f, .075f, .10f, .96f));
        UIBuilder.Place(panel.rectTransform, new Vector2(.5f, 1f), new Vector2(0, -18), new Vector2(850, 115));
        heading = UIBuilder.CreateText(panel.transform, "Heading", "", 22, TextAnchor.UpperLeft, new Color(.45f, .9f, .7f), false);
        UIBuilder.Place(heading.rectTransform, new Vector2(0, 1), new Vector2(22, -10), new Vector2(806, 30));
        instruction = UIBuilder.CreateText(panel.transform, "Instruction", "", 24, TextAnchor.UpperLeft, Color.white, false);
        UIBuilder.Place(instruction.rectTransform, new Vector2(0, 1), new Vector2(22, -44), new Vector2(806, 65));
        instruction.horizontalOverflow = HorizontalWrapMode.Wrap;
        canvas.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        var round = RoundManager.Instance;
        bool playing = round != null && round.State == RoundManager.RoundState.Playing;
        canvas.gameObject.SetActive(playing);
        if (!playing) return;
        if (!initialized)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null) return;
            player = inventory.GetComponent<PlayerState>();
            startPosition = inventory.transform.position;
            initialized = true;
        }
        moved |= Vector3.Distance(startPosition, inventory.transform.position) >= 1f;
        collected |= inventory.Items.Count > 0 || inventory.BankedValue > 0;
        inspected |= BackpackUI.IsOpen;
        banked |= inventory.BankedValue > 0;

        if (BackpackUI.IsOpen)
            Show("BACKPACK / THE TIMER IS STILL RUNNING", "W/S or arrows select an item; Q drops it.\nPress B to close and move again.");
        else if (player != null && player.IsFrozen)
            Show("TRAPS / WAIT FOR THE COUNTDOWN", "Red traps freeze you for 2 seconds.\nAvoid them when planning your return route.");
        else if (round.TimeRemaining <= 30f && inventory.BankedValue < round.TargetValue)
            Show("TIME IS LOW / RETURN TO THE SAFE HOUSE", "Head back now. Drop heavy loot if you need more speed.\nYou must be inside the safe house when time runs out.");
        else if (inventory.BankedValue >= round.TargetValue)
            Show("FINAL STEP / EXTRACT", player != null && player.IsInSafeHouse
                ? "Target reached! Press F here to finish the Tutorial."
                : "Target reached! Return to the safe house and press F.");
        else if (!moved)
            Show("01 / MOVE", "Use WASD or arrow keys to leave the safe house.\nYour goal: collect valuables, then return to bank them.");
        else if (!collected)
            Show("02 / COLLECT", "Walk near a collectible until E appears, then press E.\nLoot has both value and weight.");
        else if (!inspected && !banked)
            Show("03 / MANAGE WEIGHT", "More weight means slower movement. Press B to inspect loot.\nQ drops the selected item; dropping is optional.");
        else if (!banked)
            Show("04 / BANK YOUR LOOT", "Return to the safe house where you started.\nEntering deposits loot automatically and clears its weight.");
        else
            Show("05 / COLLECT, BANK, EXTRACT", $"Bank ${Mathf.Max(0, round.TargetValue - inventory.BankedValue)} more, then press F inside the safe house.\nOptional: find a key and press E at the vault for more loot.");
    }

    private void Show(string title, string body)
    {
        string key = title + body;
        if (shown == key) return;
        shown = key;
        heading.text = title;
        instruction.text = body;
    }

    private void OnDisable()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }
}
