# Libraries live on the player's machine

A Library is a named set of Functions that any Script, in any level, calls by its qualified name: `timing.edge(x)`. Libraries are kept on the player's own machine, not in the level, and the compiler gets each one's text through a lookup by name. This supersedes ADR 0008's "libraries stay local".

A wiring pattern worth naming is often worth reusing in other Scripts and levels without copying it into each. Storing a Library in every level that uses it would copy it anyway, and copies drift apart. Calls only expand into ordinary Logic Blocks, so the level never needs the Library to play.

The cost is the one ADR 0008 named: a Script's meaning now depends on text outside the level. Someone without the Library, or with a different version of it, can't recompile a Script that calls it, and a recompile after editing a Library changes the blocks of every Script that calls it. The blocks already in the level keep working either way.

A Library's errors are reported where the Script calls into it, as `In timing.edge, line 3: …`, since the Script's lines are the only ones the Script window shows. A Library can't use `placed(...)`, because a UID belongs to one level; a Script passes its placed blocks in as inputs.
