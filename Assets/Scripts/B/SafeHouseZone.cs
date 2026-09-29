using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Collider2D))]
public sealed class SafeHouseZone : MonoBehaviour
{
    private readonly Dictionary<Collider2D, PlayerInventory> occupants = new();
    private readonly List<Collider2D> stale = new();
    private readonly HashSet<PlayerInventory> players = new();

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other) => Track(other);
    private void OnTriggerStay2D(Collider2D other) => Track(other);

    private void Track(Collider2D other)
    {
        var inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null) return;
        var state = inventory.GetComponent<PlayerState>();
        if (state == null) return;
        occupants[other] = inventory;
        players.Add(inventory);
        state.IsInSafeHouse = true;
        Deposit(inventory);
    }

    private static void Deposit(PlayerInventory inventory)
    {
        var round = RoundManager.Instance;
        if (round != null && round.State == RoundManager.RoundState.Playing)
            inventory.DepositAll();
    }

    private void Update()
    {
        stale.Clear();
        foreach (var entry in occupants)
            if (entry.Key == null || !entry.Key.enabled || !entry.Key.gameObject.activeInHierarchy)
                stale.Add(entry.Key);
        foreach (var collider in stale) occupants.Remove(collider);
        foreach (var inventory in players)
        {
            if (inventory == null) continue;
            bool inside = occupants.ContainsValue(inventory);
            var state = inventory.GetComponent<PlayerState>();
            if (state != null) state.IsInSafeHouse = inside;
            if (inside) Deposit(inventory);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!occupants.TryGetValue(other, out var inventory)) return;
        occupants.Remove(other);
        if (inventory == null || occupants.ContainsValue(inventory)) return;
        var state = inventory.GetComponent<PlayerState>();
        if (state != null) state.IsInSafeHouse = false;
        players.Remove(inventory);
    }

    private void OnDisable()
    {
        foreach (var inventory in players)
            if (inventory != null && inventory.TryGetComponent<PlayerState>(out var state))
                state.IsInSafeHouse = false;
        occupants.Clear();
        players.Clear();
    }
}
