# In-game tests

`tools/ingame-test.sh` runs Scenarios against the real game: it builds this branch, installs exactly that build into Zeepkist, starts the game through Steam, and lets a test plugin (`Zeepkist.LogicScripting.InGameTests`) walk from the start screen into the level editor and run each Scenario's steps there. It records every Run Step's result and screenshots, quits the game, and puts the install back as it was.

It runs on Linux, with Zeepkist installed through Steam and running under Proton.

## Running it

```sh
tools/ingame-test.sh                     # the smoke Scenario
tools/ingame-test.sh my.zlstest other.zlstest
```

Steam has to be running and Zeepkist closed. A run takes over the game for a minute or two, plus about half a minute for each Scenario after the first: don't touch it meanwhile, and raise `--timeout` for a long list.

Scenarios run in the order given, each on a new, empty level in the editor: the first on the one the walk from the start screen opens, every later one on a new level started from the main menu, the way a player starts one. A failing step stops its own Scenario, with a screenshot, and the run goes on with the next. If the game can't get to a new level, the run stops there.

The script prints a summary and exits 0 only when every step of every Scenario passed. Everything the run found is in `.ingame-test/<run id>/`:

- `results.md`: each Scenario's steps, with what a failing check expected and found, the exceptions the game logged meanwhile, and the exceptions it logged as it quit
- `results.json`: the same, for tools, without the exceptions at quit
- the screenshots: `editor.png` once the walk reaches the editor, and `<scenario>-<name>.png` for a Scenario's own, including `<scenario>-failure.png` where one failed
- `LogOutput.log`: the game's BepInEx log, where the test plugin's lines start with `[ZLS-TEST]`

Only one run can use the game at a time. A second run, say from another worktree, waits until the first is done.

### Options

| Option | What it does |
| --- | --- |
| `--no-build` | Uses the Release build that's already there instead of building. |
| `--keep` | Leaves this build installed afterwards, for playing it by hand. The other copies of the mod stay set aside until the next run puts them back. The test plugin is always removed. |
| `--timeout <seconds>` | Stops the game and fails the run after this long. The default is 300. |
| `-h`, `--help` | Prints the usage. |

`ZEEPKIST_DIR` sets the game's install folder, when it isn't found in Steam's libraries.

### Exceptions

The test plugin records every exception the game logs while the run is going, mod or not, with the first line of its stack trace. `results.md` lists each Scenario's under it, and those logged on the way to the editor at the top, counted where the same one comes from the same place. They don't fail a step: end a Scenario with `expect-no-exceptions` to check that it logged none.

Once the game has quit, the script reads what it logged after the run finished from `LogOutput.log` into an **Exceptions at quit** section, grouped by the exception and its first stack line, with a count. These are reported, but don't fail the run. When the run didn't finish, the section says so instead.

## Scenarios

A Scenario is a `.zlstest` file, named after the file. It has one step per line. Blank lines and lines starting with `#` are skipped. `set-text` takes the lines below it, up to a line that is just `end`, so indent a Function's own `end` inside it. The indentation they share is taken off.

```
# A Script drives a block in test mode, and is still there back in the editor
open-window
new-script Lights
set-text
power_display(1 & 1)
end
compile
expect-result Placed 3 blocks
expect-blocks 3
place-start
test-mode
wait 2
screenshot running
back-to-editor
expect-blocks 3
open-window
select-script Lights
expect-text power_display(1 & 1)
```

A Scenario that can't be read fails before the game starts it, with the line that's wrong.

The test plugin's own Scenarios are in `Zeepkist.LogicScripting.InGameTests/Scenarios/`.

### Steps

| Step | What it does |
| --- | --- |
| `open-window` | Opens the Script window. |
| `close-window` | Closes it. |
| `new-script <name>` | Makes a new Script and selects it, as New Script does. Fails when the name is taken. |
| `select-script <name>` | Selects a Script, as clicking it in the list does. Fails when there's none by that name. |
| `new-library <name>` | Makes a new Library and selects it, as New Library on the Libraries tab does. Fails when the name is taken. |
| `select-library <name>` | Selects a Library, as clicking it in the Libraries tab's list does. Fails when there's none by that name. |
| `show-tab scripts\|libraries` | Shows the Scripts side's Scripts or Libraries tab, as clicking it does. The steps for a list and its footer work on the shown tab. |
| `open-new` | Opens New's name prompt on the shown tab, as clicking New Script or New Library does. |
| `open-rename <name>` | Opens the rename prompt for a Script or Library on the shown tab, as Rename in its context menu does. Fails when there's none by that name. |
| `open-delete <name>` | Opens the delete prompt for a Script or Library on the shown tab, as Delete in its context menu does, leaving it open. Fails when there's none by that name. |
| `type-name <text>` | Replaces the text in the open name prompt, as typing it does. Fails when no name prompt is open. |
| `press-prompt <button>` | Clicks a button of the open prompt: `ok`, `cancel`, `delete` (Delete blocks too, for a Script with blocks) or `keep-blocks`. Fails when the open prompt has no such button. |
| `select-group <name>` | Selects a Script's blocks, as Select group in its context menu on the Scripts tab does, showing that tab. Fails when there's no Script by that name. |
| `expect-list-result <text>` | The shown tab's result line, in its footer, contains the text, matching case. |
| `expect-prompt none\|new\|rename\|delete` | Which prompt the shown tab's footer has open. |
| `expect-footer-in-view` | The open prompt, the result line and New all lie inside the shown tab, and the tab inside the window, with the list at least two rows tall. The prompts may scroll when there isn't room for all of them, but get at least a row. |
| `set-window-height <px>` | Sets the window's height, as dragging its edge does, keeping its top edge. The window stays no shorter than its minimum, so a small enough height gives the smallest window. |
| `set-text` | Replaces the selected Script's or Library's text with the lines up to `end`, as one edit. `{uid:<name>}` in them is written as the UID of the block `place-block ... as <name>` placed, so a Script can write `placed("{uid:lamp}")`. Fails for a name no block was placed as. |
| `compile` | Compiles the selected Script, as Ctrl+Enter does. It passes whatever the Compile did: check that with the steps below. |
| `expect-text <text>` | The selected Script's or Library's text contains the text, matching case. |
| `expect-result <text>` | The result line contains the text, matching case. |
| `expect-no-errors` | The window's check finds no errors in the text. |
| `expect-errors <count>` | The check finds this many errors. |
| `expect-error <line>:<column> <text>` | The check finds an error at that one-based line and column whose message contains the text, matching case, such as `expect-error 2:15 'nope' isn't defined`. |
| `expect-blocks <count>` | The level has this many Logic Blocks, placed by a Script or not. Other blocks, such as a start block, don't count. |
| `caret-at-start` | Puts the caret before the selected Script's or Library's first character and focuses the field, as clicking there does. |
| `caret-at-end` | Puts the caret after its last character, the same way. The call help shows for the call the caret is then in. |
| `complete` | Opens the completion list at the caret, as Ctrl+Space does. |
| `expect-completion <label> [<detail>]` | The open completion list has a row with the label, matching case, and with exactly the detail when one is given, such as `t logic` or `a input`. |
| `expect-call-help <text>` | The call help shows, and its call, written as `logic(a, b, op_in, op:, hide:)`, contains the text, matching case. |
| `expect-call-help-active <label>` | The call help shows, and the input or Setting it highlights has the label, written as the call help writes it, such as `b`, `op_in` or `op:`. |
| `expect-call-help-no-active` | The call help shows, and highlights none of its inputs and Settings, such as for an argument past the last input, or after a named argument the call doesn't have. |
| `expect-no-completion` | The completion list is closed. |
| `expect-no-call-help` | The call help doesn't show. |
| `press <keys>` | Presses the keys, separated by spaces, in the same frame, such as `press ctrl+space`, `press enter` or `press escape escape`, up to 8. A key is a character, such as `a` or `/`, or one of `space`, `enter`, `tab`, `escape`, `backspace`, `delete`, `up`, `down`, `left`, `right`, `home`, `end`, `pageup` and `pagedown`, with any of `shift+`, `ctrl+`, `alt+` and `cmd+` before it. `shift+` makes a letter upper case and leaves other characters as they are. The keys go to the focused field the way a player's do; see [Keys](#keys). |
| `type <text>` | Types the text into the focused field, one key a frame. Spaces at either end are taken off with the line's: press them with `press space`. |
| `place-block <function> [as <name>]` | Places a block of the Block Function's type in its first style, beside where `place-start` puts the start block, and selects it. A placed-only type such as `lamp` works too. With `as <name>`, the Scenario's later `set-text` steps can write its UID as `{uid:<name>}`. The name takes letters, digits, `-` and `_`, and fails when a block was already placed as it. |
| `insert-selected` | Writes a Placed Block Reference for each selected block at the caret, as Insert selected does. Fails when it writes none. |
| `undo` | Presses the editor's undo key. Fails when there was nothing to undo. |
| `redo` | Presses the editor's redo key. Fails when there was nothing to redo. |
| `place-start` | Places a start block on the ground below the camera, facing the way it looks, as one undo step. A level needs one for test mode. |
| `test-mode` | Starts test mode, as the editor's Test key does, and waits until the round starts and the soapbox can drive. Fails without a start block. |
| `back-to-editor` | Quits test mode from the pause menu, and waits until the editor has loaded the level again. |
| `wait <seconds>` | Waits, such as for the level to run a little before a screenshot. |
| `screenshot <name>` | Saves a screenshot as `<scenario>-<name>.png`. The name takes letters, digits, `-` and `_`. |
| `expect-no-exceptions` | The game logged no exception since the Scenario started, or since the last `expect-no-exceptions`. It fails with the ones it logged. |
| `log <text>` | Writes the text to the game's log. |

A run starts without the player's Libraries and Drafts, and deletes the Libraries before each Scenario after the first, so a Scenario sees only the Libraries it makes itself.

The window keeps its height and shown tab from one Scenario to the next, the way it does for a player: a Scenario that changes them puts them back at its end.

The window steps need the window open, and the steps for the editor's text need a Script or a Library selected: they fail and say which step to add first. `compile` and `insert-selected` need a Script. `press` and `type` need a field focused, such as by `caret-at-end`.

### Keys

`press` and `type` hand each key to Imui the way Unity hands it a player's: a key event, then the character it types as an event of its own. Every field, the Script editor's completion list and call help among them, handles them the next frame as typed. A character is the one a US keyboard types, and `type` sends a character without a key of its own here, such as `(`, as its character alone.

Unity's own `Input` never sees these keys, so whatever reads it doesn't either. The Script editor reads Escape there before it handles Escape's key event, so the steps hold an Escape for it the same way. Unity reports one Escape a frame, so each Escape after the first in a `press` comes a frame later. Unity's other Escape path isn't covered: a name prompt that reads Escape from `Input`, and the level editor's own pause key, never see one.

### Adding a step

1. Add a class to `Zeepkist.LogicScripting.InGameTests/Steps/` that derives from `ScenarioStep`. Its `Run` is a coroutine: it can wait for frames, and it fails the step with `context.Fail(...)`. A check also records what it compared, with `context.Pass(...)` or `context.Fail(detail, expected, actual)`.
2. Add it to `StepList`, by the name it's written with, reading what follows the name with `StepArgument`.
3. Add it to the table above and to `StepListTests`, which checks that the two lists match.

To do what a button in the Script window does, add a method to `ScriptWindow.Driver` in the mod. It calls the same code the button does, so the step tests the real thing. A button in a list tab's footer is on `ScriptWindow.Driver.ShownList`, the shown tab's list, whose prompts take a button pressed for the player the next time they're drawn.

## What a run changes, and how it puts it back

The script undoes all of this when it ends, also after a failure, a timeout or Ctrl+C:

- **Other copies of the mod** anywhere under `BepInEx/plugins` (mod.io, an old manual install) move to `BepInEx/zls-ingame-test-disabled/`, so the game loads only this build. They move back afterwards. The test plugin fails the run when the game loaded the mod from anywhere else.
- **This build** goes into `BepInEx/plugins/Sideloaded/Plugins/Zeepkist.LogicScripting/`, and the test plugin, the Scenarios and a `run.json` into `.../Zeepkist.LogicScripting.InGameTests/`. Both are removed afterwards, except the build with `--keep`.
- **The mod's data** (Drafts, Libraries, the window's layout) and its BepInEx config are copied into the run folder first, in `backup/`, and put back once the game has quit. The game starts without the mod's data, so a run sees none of the player's Drafts or Libraries and the window opens as it does the first time. Meanwhile the player's data waits in the game's `AppData/Roaming/Zeepkist/zls-ingame-test-mod-data/`.
- **The game's level saves** in its `Autosaves` and `Backups` folders, which test mode and going back to the main menu add to, are copied the same way and put back.

The test plugin only does anything when the game starts with the script's command-line flag and finds a `run.json` next to it, so a copy left behind by a crash can't take over normal play. If a run was killed outright, the next run first puts back what the last one left, the player's mod data among it.
