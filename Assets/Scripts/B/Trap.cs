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
    private float triggeredAt;
    private static Trap countdownOwner;
    private PlayerState frozenPlayer;
    private float freezeEndsAt;
    private int shownTenths = -1;

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
        if (graphic != null)
        {
            // Gradually return to the armed color so players can judge the cooldown.
            float duration = readyAt - triggeredAt;
            float progress = duration > 0f ? Mathf.Clamp01((Time.time - triggeredAt) / duration) : 1f;
            graphic.color = Color.Lerp(coolingColor, armedColor, progress);
        }
        UpdateCountdown();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var round = RoundManager.Instance;
        if (round == null || round.State != RoundManager.RoundState.Playing || Time.time < readyAt) return;
        var inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null || !inventory.TryGetComponent<PlayerState>(out var state)) return;
        triggeredAt = Time.time;
        readyAt = triggeredAt + Mathf.Max(cooldownSeconds, freezeSeconds);
        state.Freeze(freezeSeconds);
        float previousEnd = countdownOwner != null && countdownOwner.frozenPlayer == state
            ? countdownOwner.freezeEndsAt : Time.time;
        frozenPlayer = state;
        freezeEndsAt = Mathf.Max(previousEnd, Time.time + freezeSeconds);
        countdownOwner = this;
        shownTenths = -1;
        UpdateCountdown();
    }
    private void UpdateCountdown()
    {
        if (countdownOwner != this) return;
        var round = RoundManager.Instance;
        if (frozenPlayer == null || round == null || round.State != RoundManager.RoundState.Playing)
        {
            countdownOwner = null;
            return;
        }

        int tenths = Mathf.CeilToInt(Mathf.Max(0f, freezeEndsAt - Time.time) * 10f);
        if (tenths == 0)
        {
            countdownOwner = null;
            GameHUD.ShowMessage(frozenPlayer.IsFrozen ? "Still frozen" : "0.0s - You can move again!");
            return;
        }
        if (tenths == shownTenths) return;
        shownTenths = tenths;
        GameHUD.ShowMessage($"Frozen: {tenths / 10f:0.0}s");
    }

    private void OnDisable()
    {
        if (countdownOwner == this) countdownOwner = null;
    }
}
