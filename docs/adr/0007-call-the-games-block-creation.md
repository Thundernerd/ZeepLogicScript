# Create blocks through the game's own routine, guarded by a startup check

Compile creates blocks by calling the game's private block-creation routine, the one its own undo/redo uses, through the publicized game libraries. The mod adds only the steps that routine leaves out for new blocks. The spike did it differently, copying the routine's steps into the mod.

A copy keeps working when the private signature changes, but it silently drifts when the game adds a step, and a missing step shows up as subtly broken blocks (the spike's untextured blocks were exactly that kind of bug). Calling the routine picks up the game's changes for free. A signature change then fails loudly instead of quietly, and the startup check catches it before any Compile runs.

So the check that compares the Block Manifest with the game's prefabs when the editor opens also confirms that the routine exists with the expected signature. On a mismatch, Compile is disabled with a "needs an update for this game version" message, while editing and checking Scripts keep working.

Publicized libraries only get a private call past the compiler; the runtime still rejects it as inaccessible. The project therefore enables unsafe code, which makes the runtime skip access checks for the mod's assembly, as the author's other mods already do. The alternative, binding each private method to a delegate through reflection, also worked, but costs a lookup per member and differs from those mods for no gain, since the startup check already guards the signature.
