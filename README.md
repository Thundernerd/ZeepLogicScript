# Zeepkist.LogicScripting

A mod that lets you write logic as text in the level editor.
The mod compiles the text into logic blocks.
The mod places the blocks and connects the wires for you.

The result is normal logic blocks.
Other players do not need the mod to play your level.

```logic
default hide: true

p = player_input(player: 1)
open = toggle_latch(p.horn)
wall_1 = placed("wall-uid")
toggler(open, targets: [wall_1])
```

This Script makes a wall disappear when player 1 honks, and come back on the next honk.

## Status

Logic Scripting is a pre-release.
It works, but expect rough edges, and keep a copy of levels you care about.
Report problems on the [issue tracker](https://github.com/Thundernerd/Zeepkist.LogicScripting/issues).

## Installation

Install Logic Scripting with [Modkist](https://github.com/Thundernerd/ModkistMKII).
Modkist installs BepInEx and ZeepSDK.

## Getting started

1. Open a level in the level editor.
2. Press **F9** to open the Logic Script window.
   You can also use the **Logic Script** menu in ZeepSDK's menu bar.
   You can change the key in the mod's settings.
3. Click **New Script** and give it a name.
4. Type your Script.
   The window checks it while you type and lists the errors under the text.
5. Click **Compile**, or press **Ctrl+Enter**.

Compile places the blocks where the editor places a new block, in front of the camera.
Signals flow toward the right of the screen.
Compile is one undo step in the level editor:
click outside the text, then **Ctrl+Z** takes the whole thing back.
Inside the text, Ctrl+Z undoes your typing.

The blocks one Compile places are the Script's Block Group.
Compile the Script again after you change it.
The new group replaces the old one, at the same spot.

### Things to know

- **Use `default hide: true`.**
  Visible logic blocks and wires are solid in play mode, so cars bump into them.
  Put `default hide: true` at the top of a Script to hide all its blocks in play mode.
- **Don't edit compiled blocks by hand.**
  The next Compile replaces them, so hand edits are lost.
  Change the Script instead.
  A copy you paste of a compiled block is a normal block, and Compile leaves it alone.
- **Compile to save.**
  The level stores a Script's text when you compile it.
  Text you type after that is a Draft, kept on your own machine.
  A Script you never compiled is lost when you load another level or go back to the main menu.
- **After a game update**, the mod checks its block list against the game.
  If a block changed, the mod turns that block off, or turns Compile off, until the mod is updated.
  You can still edit and check your Scripts.

## The window

- **Scripts**: the Scripts in this level.
  Right-click a Script to rename it, select its blocks, or delete it.
- **Orphaned Groups**: compiled blocks whose Script text the level no longer holds,
  for example because you deleted the block that holds the text.
  You can delete them, untag them into plain blocks, or select them.
- **Blocks**: two tabs.
  - **Placed blocks**: the blocks you placed by hand that the Script refers to.
    Select blocks in the level and click **Insert selected** to add them to the Script.
  - **Reference**: every block function, with its inputs, outputs and settings.
    Type in the filter to search.

The text editor knows the usual shortcuts:
Ctrl+Z / Ctrl+Y to undo and redo, Ctrl+D to duplicate a line, Ctrl+Shift+K to delete a line,
Ctrl+/ to comment a line out, Alt+Up / Alt+Down to move lines, and Tab / Shift+Tab to indent.

While you type a name, the editor lists the block functions and defined names that match.
It only lists names defined on an earlier line, except inside `delay(...)`, which can refer to any of them.
At the start of an argument it lists the call's inputs and settings by name first, leaving out those the call already has;
a call to a placed block lists only its inputs. After `default` it lists `hide:`.
Starting an argument opens the list with the call's named arguments:
typing the `(` of a call, or a comma or space before its next argument, or choosing a block function from the list.
After a setting's `:` it lists the values the setting takes, such as the operators after `op:` or `true` and `false` after `hide:`;
typing the `:` or a space after it, or choosing the setting from the list, opens it. Number settings list nothing.
Up / Down choose one, Enter or Tab writes it, and Escape closes the list.
Ctrl+Space opens the list at any time.
To stop it opening by itself, turn off **Completion while typing** in the mod's settings.

Inside a call's parentheses, a box above the line shows the call's inputs, in the order positional arguments fill them,
and its settings, with the one you're writing highlighted and, for a setting, the values it takes.
Inside a call nested in another, it shows the inner one.
Escape hides it until the cursor leaves the call.

## Writing Scripts

A Script is a list of lines, read from top to bottom.
Each line places blocks and wires them together.
A Script has no loops or `if`; its functions only name wiring you use more than once.
For that kind of logic, use blocks that do it while the level plays, like `multiplexer`, `transistor` or `counter`.

### Lines, names and comments

```logic
# A comment starts with # and runs to the end of the line
trigger_1 = placed("trigger-1-uid")
trigger_2 = placed("trigger-2-uid")
both = trigger_1 & trigger_2
power_display(both)
```

- A line like `name = ...` gives a name to a block's output.
  Use the name in later lines to wire that output somewhere else.
- A line without a name is for blocks that have no output, like `toggler`, `death` or `power_display`.
- Names use letters, digits and `_`, and don't start with a digit.
  Each name is defined once, and only used below the line that defines it.
  The one exception is `delay`; see [Feedback with delay](#feedback-with-delay).
- One line is one statement.
  To spread a long statement over several lines, put it in parentheses:

  ```logic
  together = (
      trigger_1
      & trigger_2
  )
  ```

### Numbers

A whole number like `5` or `-3` is a Generator with that power.
Each number gets one Generator, shared by every place that uses it.
There are no decimals.
Blocks that work with fractions, like `math` with `sin`, use whole numbers scaled by 10000.

### Operators

Operators are short for a `logic` or `math` block.
They bind like in C: from loose to tight, top to bottom.

| Operator | Block | Example |
|---|---|---|
| `\|` | `logic(op: or)` | `a \| b` |
| `^` | `logic(op: xor)` | `a ^ b` |
| `&` | `logic(op: and)` | `a & b` |
| `==` `!=` | `math(op: equal)`, `math(op: not_equal)` | `count == 3` |
| `<` `>` `<=` `>=` | `math(op: less)`, … | `speed > 100` |
| `+` `-` | `math(op: add)`, `math(op: subtract)` | `a + 1` |
| `*` `/` `%` | `math(op: multiply)`, `math(op: divide)`, `math(op: modulo)` | `a % 4` |
| `!x` | `digitizer(x, invert: true)` | `!pressed` |
| `-x` | `0 - x` | `-speed` |

Use parentheses to group: `(a | b) & c`.
Logic operators count any Signal of 1 or more as on.
Compare operators give 1 for true and 0 for false.

### Calling blocks

Each block function places one block.
The function is named after the block, like `delay` or `player_input`;
see [Blocks](#blocks) for the full list.

```logic
p = player_input(player: 1)
x = math(p.analog_left, p.analog_right, op: multiply)
y = math(b: x, a: 10, op: subtract)
roll = random(p.horn, min: 1, max: 6)
```

- Arguments without a name wire the block's inputs, in order.
- An argument with an input's name wires that input, like `b: x`.
  You can leave inputs out.
- Other named arguments are the block's settings, like `op: multiply`.
  A setting takes a whole number, `true` or `false`, or one of the words its list shows in the editor.

A setting you leave out gets the value a block placed by hand has, which is usually 0 or off.
So `random(press)` rolls between 0 and 0, and `generator()` has power 0.
The one exception is `delay`, which needs `ticks:`: a Delay with 0 ticks breaks the level's logic.

Some settings exist on many blocks:

- `hide:` hides the block in play mode.
  `default hide: true` at the top sets it for every block in the Script.
- `player:` picks a player, `1` to `4`, like the editor does.
  Blocks that act on players also take `all`.
- `style:` picks the shape of blocks that come in several shapes, like `passthrough(x, style: skinny_2h)`.

### Outputs

A block with one output is used as a value right away: `on = toggle_latch(press)`.

A block with several outputs is given a name, and you pick an output with a dot:

```logic
p = player_input(player: 2)
steer = p.left | p.right
```

`simple_telemetry`'s outputs depend on its `mode:`.
The Reference tab lists them per mode.

### Feedback with delay

Inside `delay(...)` you may use a name that is defined later, or the name being defined.
This is how a Script feeds a Signal back into itself:

```logic
p = player_input(player: 1)
press = pulser(p.horn)
on = delay(on ^ press, ticks: 1)
power_display(on)
```

Each horn press flips `on`.
Every loop of wires has to pass through a Delay; the game's logic stops when one doesn't.
That's why a name can only refer ahead inside `delay`.

### Blocks you placed by hand

Compile only places blocks whose position doesn't matter.
For triggers, sensors, lights, cannons and movers, place the block yourself and refer to it with `placed(...)`.
A logic block you placed by hand works the same way, with all its inputs and outputs.
Any other block, like a wall, can be a Toggler target.

Select the blocks in the level and click **Insert selected** in the Placed blocks tab.
It writes a line like this for each of them:

```logic
trigger_1 = placed("trigger-uid")
```

The text in quotes is the block's UID.
The examples in this README use made-up UIDs; Insert selected writes the real ones.
It skips blocks that belong to a Script's Block Group.

Insert selected names a block after its type, like `trigger_1` or `lamp_1`,
and a block that isn't a logic block `block_1`.
You can rename them; the examples call a wall `wall_1`.

Use the name to read the block's outputs, and call it to wire its inputs:

```logic
trigger_1 = placed("trigger-uid")
lamp_1 = placed("lamp-uid")
lamp_1(red: 255, green: (trigger_1 > 0) * 255)
```

A Script never changes the settings of a placed block; set those in the editor.
Each input takes one wire, counting the wires that are already in the level.

### Your own functions

A function gives a name to wiring you use more than once.
Write it once, then call it like a block function:

```logic
function edge(x) -> rising
    prev = delay(x, ticks: 1)
    rising = x & !prev
end

p = player_input(player: 1)
pressed = edge(p.horn)
power_display(pressed)
```

Each call places the blocks of the function's body, wired to what you passed in, as if you had written them out yourself.
The level doesn't need the mod to play them.

- The first line names the function, its inputs in parentheses, and after `->` its outputs.
  Everything up to `end` is the body.
  Indenting the body is only for reading.
- Pass every input, in order or by name: `edge(p.horn)` or `edge(x: p.horn)`.
- The body has to define each output.
  A function with one output is used as a value.
  With several, pick one with a dot, like a block with several outputs:

  ```logic
  function sr_latch(set, reset) -> q, not_q
      q = delay(!reset & (set | q), ticks: 1)
      not_q = !q
  end

  trigger_1 = placed("trigger-1-uid")
  trigger_2 = placed("trigger-2-uid")
  l = sr_latch(trigger_1, trigger_2)
  power_display(l.q)
  ```

- A function without `->` has no outputs; call it on its own line.
- The body only sees its inputs and its own names, not the names outside it.
  It can call other functions, but not itself.
  `delay` works inside it the same way: only inside `delay` can a name refer ahead.
- You can define a function above or below the lines that call it.
  Its name can't be a block function's or a name the Script already uses.
- An input is wired once, however often the body uses it, so `edge(a & b)` places one `logic` block for `a & b`.
- Pass a placed block in, and the body can call it to wire its inputs:

  ```logic
  function light(lamp, on)
      lamp(red: on * 255)
  end

  trigger_1 = placed("trigger-uid")
  lamp_1 = placed("lamp-uid")
  light(lamp_1, trigger_1)
  ```

- `default hide:` goes outside functions, and applies to the blocks they place too.

### Tips

- Counter, Toggle Latch, Random, Timer, Resettable Ticker, Death, State Changer, Physics Cannon and Dynamic Mover
  react when their input goes from 0 to exactly 1.
  A value that jumps from 0 to 5 does nothing.
  Simple Memory's and Cheese Memory's `write`, Parking, Force Giver's `enable`
  and Property Multiplier's `apply` and `reset` need exactly 1 too.
  Turn a value into 0 or 1 first, with `x > 0` or `digitizer(x)`.
- `power_display(x)` shows a Signal's value in the editor's test mode.
  Use it to see what a sensor or telemetry block puts out.

## Examples

### A door that opens while someone stands in a trigger

```logic
default hide: true

trigger_1 = placed("trigger-uid")
wall_1 = placed("wall-uid")
toggler(trigger_1 > 0, targets: [wall_1])
```

A Toggler hides its targets while its input is on.
The trigger counts the bodies inside it, so `> 0` turns it into on or off.

### Open a wall after three laps

```logic
default hide: true

finish_1 = placed("trigger-finish-uid")
wall_1 = placed("wall-uid")

laps = counter(finish_1 > 0)
toggler(laps >= 3, targets: [wall_1])
power_display(laps)
```

The display shows the lap count while you test the level.

### Two triggers, in order

The wall disappears once a player has passed trigger 1 and then trigger 2.

```logic
default hide: true

trigger_1 = placed("trigger-1-uid")
trigger_2 = placed("trigger-2-uid")
wall_1 = placed("wall-uid")

armed = simple_memory(1, write: trigger_1 > 0)
done = simple_memory(1, write: armed & trigger_2 > 0)
toggler(done, targets: [wall_1])
```

`simple_memory` stores its `data` input while `write` is 1, and keeps it after that.

## Blocks

Each block function, with the name the block has in the editor's block list.
The editor's names end in "Logic Block", left out here.

### Functions

| Function | In-game block | Inputs | Outputs |
|---|---|---|---|
| `generator` | Generator | – | one |
| `logic` | Logic | `a`, `b`, `op_in` | one |
| `math` | Math | `a`, `b`, `op_in` | one |
| `math_3_pins`, `lerp` | Math (3 Inputs) | `start`, `end`, `t`, `op_in` | one |
| `digitizer` | Digitizer | `x` | one |
| `transistor` | Transistor | `data`, `gate` | one |
| `multiplexer`, `mux` | Multiplexer | `in1`, `in2`, `in3`, `in4`, `select` | one |
| `demultiplexer`, `demux` | Demultiplexer | `select`, `data` | `ch1`, `ch2`, `ch3`, `ch4` |
| `delay` | Delay | `x` | one |
| `pulser` | Pulse Generator | `x` | one |
| `random` | Random | `trigger`, `max_in` | one |
| `ticker` | Ticker | – | one |
| `resettable_ticker` | Resettable Ticker | `reset` | one |
| `timer` | Timer | `duration`, `start` | one |
| `simple_memory` | Simple Memory | `data`, `write` | one |
| `cheese_memory` | Cheese Memory | `data`, `write` | one |
| `counter` | Counter | `increment`, `decrement`, `reset` | one |
| `toggle_latch` | Toggle Latch | `x` | one |
| `date_timer` | DateTime | – | `year`, `month`, `day`, `hour`, `minute`, `second` |
| `player_input` | Player Input | – | `arms_up`, `brake`, `left`, `right`, `analog_left`, `analog_right`, `horn` |
| `simple_telemetry` | Simple Telemetry | – | by `mode:`; see the Reference tab |
| `wheel_telemetry` | Wheel Telemetry (Advanced) | – | `all`, `any`, `lf`, `rf`, `lr`, `rr` |
| `passthrough` | Passthrough (all four shapes) | `x` | one |
| `power_display` | Signal Power Display (both sizes) | `x` | one |
| `toggler`, `door` | Toggler | `x` | – |
| `death` | Death | `trigger` | – |
| `parking` | Parking | `x` | – |
| `state_changer` | State Changer | `trigger`, `state` | – |
| `force_giver` | Force Giver (Advanced) | `enable`, `x`, `y`, `z` | – |
| `property_multiplier` | Property Multiplier (Advanced) | `apply`, `reset` | – |

### Settings

| Function | Settings |
|---|---|
| `generator` | `power` |
| `logic` | `op`: `and`, `or`, `nand`, `nor`, `xor`, `xnor` |
| `math` | `op`: `add`, `subtract`, `multiply`, `divide`, `modulo`, `less`, `greater`, `less_equal`, `greater_equal`, `equal`, `not_equal`, `min`, `max`, `pow`, `log`, `abs`, `sign`, `repeat`, `ping_pong`, `sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `atan2`, `sqrt`, `divide_scaled` |
| `math_3_pins` | `op`: `lerp`, `smooth_lerp` |
| `digitizer` | `invert` |
| `delay` | `ticks` (required, at least 1) |
| `pulser` | `mode`: `rise`, `fall`, `any_rise`, `any_fall`, `increased`, `decreased` |
| `random` | `min`, `max` |
| `simple_memory`, `cheese_memory` | `initial` |
| `counter` | `max_reset` (0 or more), `min_reset` (0 or less) |
| `toggle_latch` | `initial` (`true` or `false`) |
| `date_timer` | `utc` (`true` or `false`) |
| `player_input` | `player` |
| `simple_telemetry` | `player`; `mode`: `basic`, `local_velocity`, `global_velocity` |
| `wheel_telemetry` | `player`; `mode`: `grounded`, `slipping`, `surface`, `physics_material` |
| `passthrough` | `style`: `normal`, `skinny_1h`, `skinny_2h`, `skinny_3h` |
| `power_display` | `style`: `normal`, `compact` |
| `toggler` | `targets`: a list of 1 to 8 placed blocks, like `[wall_1, wall_2]` |
| `death`, `parking`, `state_changer`, `property_multiplier` | `player` (`all` or `1`–`4`) |
| `force_giver` | `player` (`all` or `1`–`4`); `force_mode`: `acceleration`, `force`, `impulse`, `velocity_change`; `space`: `local`, `global`, `follow_velocity`, `local_rotation`, `global_rotation`; `particles`: `none`, `booster`, `slowster`, `feather`, `flying`, `fan` |

Every block function also takes `hide`.
The Property Multiplier's multipliers aren't in the language yet; set them in the editor.

### Placed-only blocks

These blocks can only be reached with `placed(...)`.
Insert selected names them after the type, like `lamp_1`.

| Type | In-game block | Inputs | Outputs |
|---|---|---|---|
| `trigger` | Trigger | – | one: how many bodies are inside |
| `distance_sensor` | Distance Sensor (Advanced) | – | `distance`, `x`, `y`, `z` |
| `lamp` | Dynamic Light (Advanced) | `intensity`, `red`, `green`, `blue`, `range`, `angle` | – |
| `cannon` | Physics Cannon | `fire`, `object` | – |
| `dynamic_mover` | Dynamic Mover, Dynamic Mover (Primitives) | `go`, `return`, `reverse` | `arrived_target`, `arrived_start`, `player_on` |
| `dynamic_rotator` | Dynamic Rotator | `speed`, `stop_at_target`, `target_angle` | `arrived`, `angle` |
| `advanced_mover` | Dynamic Mover (Advanced), Dynamic Mover (Advanced) (Primitives) | `x`, `y`, `z`, `rot_x`, `rot_y`, `rot_z` | – |
| `block` | any other block | – | – (only a Toggler target) |

## License

See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
