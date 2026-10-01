using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class VaultDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private Collider2D doorCollider;
    [SerializeField] private SpriteRenderer[] doorGraphics;
    [SerializeField] private string lockedMessage = "Vault locked - find a key to open it";
    [SerializeField, Min(0.05f)] private float feedbackSeconds = 0.35f;
    private Color[] originalColors;
    private float feedbackUntil;
    private bool showingFeedback;
    private bool opened;
    public bool CanInteract => !opened && isActiveAndEnabled;

    private void Reset()
    {
        doorCollider = GetComponent<Collider2D>();
        doorCollider.isTrigger = false;
        doorGraphics = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Awake()
    {
        if (doorCollider == null) doorCollider = GetComponent<Collider2D>();
        if (doorGraphics == null || doorGraphics.Length == 0)
            doorGraphics = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[doorGraphics.Length];
        for (int i = 0; i < doorGraphics.Length; i++)
            if (doorGraphics[i] != null) originalColors[i] = doorGraphics[i].color;
    }

    private void Update()
    {
        if (showingFeedback && Time.time >= feedbackUntil) FinishFeedback();
    }

    private void Flash(Color color)
    {
        showingFeedback = true;
        feedbackUntil = Time.time + Mathf.Max(0.05f, feedbackSeconds);
        foreach (var graphic in doorGraphics)
            if (graphic != null) graphic.color = color;
    }

    private void FinishFeedback()
    {
        showingFeedback = false;
        if (originalColors == null) return;
        for (int i = 0; i < doorGraphics.Length; i++)
        {
            var graphic = doorGraphics[i];
            if (graphic == null) continue;
            graphic.color = originalColors[i];
            if (opened) graphic.enabled = false;
        }
    }

    private void OnDisable() => FinishFeedback();

    public void Interact(GameObject player)
    {
        if (!CanInteract || player == null) return;
        var round = RoundManager.Instance;
        if (round == null || round.State != RoundManager.RoundState.Playing) return;
        var inventory = player.GetComponentInParent<PlayerInventory>();
        if (inventory == null) return;
        var state = inventory.GetComponent<PlayerState>();
        if (state != null && state.IsFrozen) return;
        if (!inventory.HasKey)
        {
            GameHUD.ShowMessage(lockedMessage);
            Flash(new Color(1f, 0.2f, 0.2f));
            return;
        }
        inventory.ConsumeKey();
        opened = true;
        doorCollider.enabled = false;
        // Unlock immediately; the short green flash is visual feedback only.
        Flash(new Color(0.25f, 1f, 0.45f));
        GameHUD.ShowMessage("Vault unlocked - key used");
    }
}
