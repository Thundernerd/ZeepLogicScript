# Positional blocks are referenced, never created

Compile only creates Logic Blocks whose position doesn't matter. Blocks whose placement matters (Trigger, Distance Sensor, Lamp, Cannon, movers) and Toggler targets are only reached through Placed Block References (`name = placed("UID")`), and a Script never changes those blocks' Settings. Placing them is a level-design decision that text and automatic layout can't express. Creating them from the Script would also make recompiling fight the player's hand edits.
