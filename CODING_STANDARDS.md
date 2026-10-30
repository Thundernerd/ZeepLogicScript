# Coding standards

## Formatting and naming

`.editorconfig` is the source of truth for layout, `var`, and naming, and the build reports violations as warnings. Run `dotnet format` before committing; the build should finish with zero warnings.

## Files

- One top-level type per file: class, struct, record, interface, enum, or delegate. Name the file after the type (`BlockGraph.cs`).
- The build enforces both with StyleCop (SA1402, SA1649); `stylecop.json` sets the type kinds.
- A large type may be split with `partial` across files named `<Type>.<Part>.cs` (e.g. `GraphBuilder.Settings.cs`).
- Each nested type gets its own `partial` file of the containing type, named `<Outer>.<Nested>.cs` (e.g. `GraphBuilder.Resolved.cs`; deeper nesting adds a part per level). It keeps its access modifier.
- The repo's own analyzer (`Zeepkist.LogicScripting.Analyzers`, rule LS0001) enforces this in the build.

## Comments

- Write a comment only for what a reader can't reasonably work out from the code itself: why a decision was made, a constraint or game quirk the code works around, or a pointer to the ADR behind it.
- Let names and structure explain what the code does. A comment that restates the code is noise; rename or extract instead.
- This includes `///` doc comments: a member doesn't need one just because it is public or internal. The build doesn't ask for them (CS1591 is off).

## Harmony patches

- One patch class per patched method, in `Patches/`, named `<Type>_<Method>` (e.g. `LEV_SaveLoad_SaveFile`).
- Target with `[HarmonyPatch(typeof(Type), nameof(Type.Method))]`, and mark each method `[HarmonyPrefix]` / `[HarmonyPostfix]`.
- Patch classes are `internal` and static-only.
- Keep patch bodies thin: raise an event or call into a mod class, and keep the logic there where it can be tested.
- Patch only what ZeepSDK doesn't already expose; prefer its APIs (`LevelEditorApi`, `StorageApi`, …).

## Unity lifecycle

- Subscribe to events in `Awake`/`Start` and unsubscribe from every one of them in `OnDestroy`.
- Keep scene searches (`FindObjectOfType` and friends) out of `Update`, other per-frame methods, and loops. Look things up once and cache the result.

## Spelling

Write American English in code, comments, docs, and UI text (`color`, `center`, `canceled`). Names the game or a library defines keep their own spelling.

## Punctuation

Player-facing text avoids semicolons. Join two thoughts with a conjunction (`… is defined later, so only delay(...) can refer to it`), a comma (`1 error, nothing placed`), a colon before a fix or a list, or split them into two sentences (`'x' is a function. Call it with ( )`).
Errors and result lines end without a final period, even when they hold two sentences. Prompts and notes in the window end with one.
Tables that list Settings may keep `;` between them.

## Names

Use the domain terms from `CONTEXT.md` in type, member, and file names, so the code reads in the same language as the design notes.

## Comments about the game

- The repo is public and the game's source isn't. A comment may name the game types and members the mod calls or mirrors, since they're in the game's own libraries, and say what the mod relies on them to do.
- Don't retell how the game's own features are written, such as which steps a tool takes or what it records; say what the mod does and why.
