# Changelog

All notable changes to Logic Scripting are listed here.

## Unreleased

### Added

- Completion in the Script editor: a list of block functions while you type a name, or on Ctrl+Space.
  Next to each one it shows the block's inputs and settings, such as `(a, b, op_in, op:)` for `logic`.
  It can be turned off for typing with **Completion while typing** in the mod's settings.
- Completion lists the names your Script defines above the line, with their block type,
  and every name inside `delay(...)`.
- Completion lists a call's inputs and settings by name, without the ones it already has, and `hide:` after `default`.
  Typing a call's `(`, or a comma or space before its next argument, opens the list with them,
  and so does choosing a block function from the list.
- Completion lists a setting's values after its `:`: option words, `true` and `false`, players and styles.
  Typing the `:` or a space after it opens the list, and so does choosing the setting from the list.
- Completion lists a block's outputs after its name and a `.`, like `t.speed`, in the `mode:` the call is written with,
  or for a `placed(...)` block, the mode it has in the level. Typing the `.` opens the list.
  A name set to a call of one of your Functions lists the Function's outputs, like `r.hi` for `function f(a) -> hi, lo`.
  A call followed by a `.` lists its outputs too, like `simple_telemetry(mode: global_velocity).heading` or `f(1).hi`.
- Inside a call, a box above the line shows its inputs and settings, highlighting the one you're writing.
- Resting the mouse on the Script editor's text tells you about what's under it:
  a block function's Reference entry, the line that defines a name, a named argument's input or setting,
  and an error underline's message.
- Ctrl+click (Cmd+click on macOS) a name in the Script editor to jump to the line that defines it.
- The Reference tab lists the blocks under four headings, Inputs, Operators, Outputs and Movers,
  split the same way as the subfolders of the game's logic block folder, and sorted by name.
  A heading can be collapsed, and stays collapsed when you reopen the window.
- Functions: `function name(inputs) -> outputs` … `end` names wiring you use more than once.
  Each call places the body's blocks inline, so the level still plays without the mod.
- A function's input can give a setting in its body, like `delay(x, ticks: ticks)`.
  A call passes it a number, `true` or `false`, or a word, also on to another function's setting input.
- Libraries: named sets of functions on your own machine, which any Script in any level calls as `timing.edge(x)`.
  Make, rename and delete them in the Libraries section of the Script window,
  and the editor checks and colors them like a Script.
  Someone without a Library can't recompile a Script that calls it, but the blocks in the level keep working.
- Completion lists the functions your Script defines, with their inputs, and `function` and `end` at the start of a line.
  Inside a call to one of them it lists its inputs by name, and the box above the line shows them.
  Inside a function's body it lists that function's inputs and the names its body defines, and nothing from outside it.
- The Reference tab lists your Libraries' functions under a Libraries heading, as `timing.edge`,
  with their inputs and outputs. An input the function uses as a setting is marked `(setting)`.
- In the Script editor, Enter after a Function's first line indents the body, and typing `end` lines it up with that first line.

### Changed

- The mod's file is now `Zeepkist.LogicScripting.dll`, next to `Zeepkist.LogicScripting.Compiler.dll`.
  The mod's settings, Drafts and the window layout from 0.1.0-alpha.1 aren't carried over.
  If you installed the mod by hand, delete the old `net.tnrd.zeepkist.logicscripting.dll` from `BepInEx/plugins`:
  with both files there, BepInEx loads the mod twice.
- Errors and result lines read as plain sentences, such as `'x' is a function. Call it with ( )` and `1 error, nothing placed`.
- The error panel is a table, with the line, column and message of each error in columns of their own.
  An error inside a Library's Function shows where in the Library it is above the message.
- The Scripts side has two tabs, Scripts and Libraries, so it shows one list at a time.
  Orphaned Groups sit under the Scripts list and can be collapsed, and the Scripts tab shows a `!` while there are any.
  The help text moved into tooltips on the tabs and the lists.
- New Script and New Library are pinned at the bottom of their tab, with the name and delete prompts
  and the result line just above them, so they stay in view however long the list is.
  The warning that `libraries.json` couldn't be read stays above the Libraries list as it scrolls.
- The dot that picks an output, or a Library's function, touches both sides: `t.speed`, `f(x).hi`, `timing.edge(x)`.
  A space before or after it, like `t .speed`, is an error at the dot, the same way completion only follows a touching dot.

### Fixed

- Undo in the Script editor takes back a line break together with the typing after it, instead of on its own.
- A long error message in the error panel wraps onto more lines, instead of being cut off at both ends.

## 0.1.0-alpha.1

The first pre-release.

### Added

- The Logic Script window in the level editor.
  Open it with F9 (configurable) or the Logic Script menu in ZeepSDK's menu bar.
- A text language for logic: names, whole numbers, operators (`& | ^ ! == != < > <= >= + - * / %`),
  a function for every logic block, settings as named arguments, and `default hide: true`.
- Feedback through `delay(...)`: a name inside it can refer to itself or a later line.
  A Script can't build a loop without a Delay.
- Compile places the blocks and wires them in one undo step.
  Compiling again replaces the Script's blocks at the same spot.
- `placed(...)` refers to blocks you placed by hand: triggers, sensors, lights, cannons, movers,
  and any block as a Toggler target.
  **Insert selected** writes these lines for the blocks you selected.
- Errors with line and column while you type, with a suggestion when an argument name is misspelled.
- A code editor with line numbers, colors, and shortcuts for undo, duplicate, move and comment lines.
- Scripts are stored in the level when you compile them, and typed text is kept as a Draft on your machine.
- Script list with rename, select and delete, and an Orphaned Groups list for compiled blocks whose Script is gone.
- A Reference tab that lists every block function with its inputs, outputs and settings.
- A check on entering the editor that turns off blocks that changed in a game update.
