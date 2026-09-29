using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class VaultDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private Collider2D doorCollider;
    [SerializeField] private SpriteRenderer[] doorGraphics;
    [SerializeField] private string lockedMessage = "Locked - need a key";
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
    }

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
            return;
        }
        inventory.ConsumeKey();
        opened = true;
        doorCollider.enabled = false;
        foreach (var graphic in doorGraphics)
            if (graphic != null) graphic.enabled = false;
    }
}
