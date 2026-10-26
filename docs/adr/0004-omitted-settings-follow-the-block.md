# Omitted Settings follow the block, except required ones

When a Script leaves out a Setting, the placed block gets exactly what a block placed by hand has, which is 0 / false. The Script never invents friendlier defaults, so `random(t)` has max 0 and `generator()` has power 0, as in the editor. Players who know the inspector aren't surprised, and there's no second set of defaults to remember or keep in sync.

The one exception is a **required** Setting: one whose omitted value is known to break the level. `delay` needs `ticks:`, because a Delay with 0 ticks (the game's value for an untouched Delay) throws every tick in play mode and can stop all logic. Making it required keeps the rule intact: the Script still doesn't fill in a value, it just refuses to build something known to be broken. If the game fixes the bug, the requirement can be dropped without changing what existing Scripts mean.
