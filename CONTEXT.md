# Zeepkist.LogicScripting

Domain language for the Logic Scripting BepInEx mod: write logic as text in the Zeepkist level editor and compile it into v18 logic blocks.

## Language

**Script**:
The text a player writes in the Logic Scripting window: a list of definitions that describe wiring, read top to bottom. It has no loops, functions or conditionals of its own; those come only from Logic Blocks at runtime. One Script compiles into one Block Group.
_Avoid_: program, code file, macro

**Compile**:
Turning a Script into placed, wired Logic Blocks in the open level, as one undoable editor action.
_Avoid_: build, generate, run, execute

**Logic Block**:
A game block from the v18 "Logic v2" set (block IDs 2413+), e.g. Generator, Trigger, Logic, Math, Delay, Toggler. In game code it is a prefab with a `BlockEdit_v18_C_Logic_*` (editor/data) and a `LogicScript_*` (runtime) component pair.
_Avoid_: node, gate (unless it is the Logic gate block), chip

**Ball Logic**:
The older v16 physics-based logic (balls rolling into gate triggers, block IDs 1727–1745, 2281–2291). Not a Logic Scripting target.
_Avoid_: v1 logic, legacy logic block

**Connector**:
One input (`Logic_In`) or output (`Logic_Out`) point on a Logic Block, addressed by its zero-based index on that block (`ConnectorHub_Logic`). An input accepts at most one Wire; an output can feed many.
_Avoid_: pin, port, socket, hub (except when naming the game class)

**Wire**:
A connection from an output Connector to an input Connector. Stored on both blocks as a `ConnectionStruct { targetUID, connectorID, drawingLeader }`.
_Avoid_: link, edge (except in compiler-graph code), cable

**Signal**:
The `long` value a Wire carries each tick. Most blocks treat it as 0/1 via `Clamp01`.
_Avoid_: power (except when naming game methods like `OutputSignalPower`), value, bit

**Tick**:
One physics step of the game's `LogicTicker`: an Early phase (Source Blocks) then a Late phase (everything else).
_Avoid_: frame, update, step

**Source Block**:
A Logic Block that produces Signals without needing inputs resolved first in the same Tick (`isSourceBlock`): Generator, Trigger, Ticker, Timer, DateTimer, PlayerInput, telemetry, Distance Sensor, Delay.
_Avoid_: input block, emitter

**Delay**:
The Logic Block that breaks feedback: it outputs its stored value in Early and records its input in Late. Every cycle in a Script's graph must pass through a Delay.
_Avoid_: buffer, latch (Toggle Latch is a different block)

**Dependency Loop**:
A cycle of Wires with no Delay in it. The editor refuses to wire one by hand; at runtime it deadlocks all logic for the run. A Script can't express one, because only `delay(...)` may refer to a name defined later or to itself.
_Avoid_: infinite loop, recursion, deadlock (deadlock is the runtime symptom)

**Block Manifest**:
The mod's per-Logic-Block-type description: block IDs, Block Function name, Connector indices, directions and names, Setting keys with their option names. A hand-written table, checked against the game's blocks on entering the editor; a block that doesn't match is turned off. A type whose blocks only differ in look has one Block Function, and `style:` picks the block.
_Avoid_: schema, registry, block definitions

**Block Function**:
The Script function named after a Logic Block type (`logic`, `delay`, `player_input`). Each call places one block; positional arguments are input Wires in Connector order, and a named argument either wires the input of that name (`logic(a: x)`) or gives a Setting. Operators like `&` and `+` are shorthand for a Block Function call.
_Avoid_: builtin, node constructor, command

**Setting**:
A per-block option saved in the block's property dictionary under a short key (e.g. the Logic block's operator under `"xmo"`). Game code names these keys `KEY_*`.
_Avoid_: property, parameter, field

**Block Group**:
The set of Logic Blocks one Compile placed, each tagged with its Script and its own UID. Recompiling the same Script replaces the whole group; a pasted copy of a group block is not a member. A level can hold several Scripts, each with its own Block Group.
_Avoid_: output, generated blocks, selection

**Orphaned Group**:
A Block Group whose Script text no longer exists in the level, usually because the block carrying the text was deleted by hand. It can't be recompiled, only deleted, untagged into plain blocks, or selected.
_Avoid_: lost group, stale blocks

**Draft**:
A Script's latest typed text, kept on the player's own machine. The level only holds the text of the last Compile; a Draft that differs from it is shown as "not compiled".
_Avoid_: unsaved text, buffer, autosave

**Placed Block Reference**:
A Script's pointer to a block the player already placed in the level, written `name = placed("UID")`. It's the only way a Script reaches blocks whose position matters (triggers, sensors, lamps, movers) and Toggler targets; a Script reads their outputs and wires into their inputs by calling the name (`lamp_1(intensity: x)`), but never changes their Settings. Each input still takes one Wire, counting the ones already in the level.
_Avoid_: external block, UID literal
