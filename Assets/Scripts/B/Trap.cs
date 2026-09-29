using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Trap : MonoBehaviour
{
    [SerializeField, Min(0f)] private float freezeSeconds = 2f;
    [SerializeField, Min(0f)] private float cooldownSeconds = 4f;
    [SerializeField] private SpriteRenderer graphic;
    [SerializeField] private Color armedColor = new Color(0.9f, 0.25f, 0.2f);
    [SerializeField] private Color coolingColor = new Color(0.4f, 0.4f, 0.4f);
    private float readyAt;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        graphic = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (graphic == null) graphic = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (graphic != null) graphic.color = Time.time >= readyAt ? armedColor : coolingColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var round = RoundManager.Instance;
        if (round == null || round.State != RoundManager.RoundState.Playing || Time.time < readyAt) return;
        var inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null || !inventory.TryGetComponent<PlayerState>(out var state)) return;
        readyAt = Time.time + Mathf.Max(cooldownSeconds, freezeSeconds);
        state.Freeze(freezeSeconds);
        GameHUD.ShowMessage("Trapped!");
    }
}
