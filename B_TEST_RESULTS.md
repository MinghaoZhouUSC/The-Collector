# B validation

Unity 6000.3.23f1, 2026-09-28.

## Play Mode integration

```text
PASS Round starts Ready
PASS Real physics detects spawn inside safe house
PASS Exactly one RoundManager
PASS Exactly one ItemFactory
PASS 29 loot items and at most one key spawned
PASS Start/end canvas above backpack
PASS Real trigger exit clears safe state (position=(0.00, 3.00, 0.00), body=(0.00, 3.00))
PASS Real trigger deposits loot once and retains key
PASS No key keeps vault locked
PASS Key opens vault and is consumed
PASS All vault graphics hidden
PASS Open door does not consume another key
PASS Real trap trigger freezes player
PASS Short freeze does not shorten existing deadline
PASS Long freeze extends deadline
PASS Freeze expires and standing on trap does not retrigger
PASS F-equivalent early extraction banks and wins correctly
PASS Success screen visible
PASS Timeout outside fails even with enough banked value
PASS Outside failure reason visible
PASS Timeout inside with insufficient money fails
PASS Insufficient money reason visible
PASS Restart 1 resets round, score, freeze and vault
PASS Restart 2 resets round, score, freeze and vault
PASS Restart 3 resets round, score, freeze and vault
```

## WebGL

Build succeeded with zero errors, compression disabled and default template. Local HTTP browser checks passed: start screen, Space starts the round, B opens and closes backpack while countdown continues, natural 120-second timeout shows insufficient-bank failure, R reloads Ready with 2:00 and zero score. Browser console reported no errors during these checks.

The scripted integration checks use temporary test inventory and invoke selected RoundManager methods; they do not constitute a complete manual playthrough to $500. Timing/balance should still be playtested by both teammates.
