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
- Inside a call, a box above the line shows its inputs and settings, highlighting the one you're writing.
- The Reference tab lists the blocks under four headings, Inputs, Operators, Outputs and Movers,
  split the same way as the subfolders of the game's logic block folder, and sorted by name.
  A heading can be collapsed, and stays collapsed when you reopen the window.
- Functions: `function name(inputs) -> outputs` … `end` names wiring you use more than once.
  Each call places the body's blocks inline, so the level still plays without the mod.

### Changed

- The mod's file is now `Zeepkist.LogicScripting.dll`, next to `Zeepkist.LogicScripting.Compiler.dll`.
  The mod's settings, Drafts and the window layout from 0.1.0-alpha.1 aren't carried over.
  If you installed the mod by hand, delete the old `net.tnrd.zeepkist.logicscripting.dll` from `BepInEx/plugins`:
  with both files there, BepInEx loads the mod twice.

### Fixed

- Undo in the Script editor takes back a line break together with the typing after it, instead of on its own.

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
- Scripts are stored in the level when you compile them; typed text is kept as a Draft on your machine.
- Script list with rename, select and delete, and an Orphaned Groups list for compiled blocks whose Script is gone.
- A Reference tab that lists every block function with its inputs, outputs and settings.
- A check on entering the editor that turns off blocks that changed in a game update.
