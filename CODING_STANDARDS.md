# Coding standards

## Formatting and naming

`.editorconfig` is the source of truth for layout, `var`, and naming, and the build reports violations as warnings. Run `dotnet format` before committing; the build should finish with zero warnings.

## Files

- One top-level type per file: class, struct, record, interface, enum, or delegate. Name the file after the type (`BlockGraph.cs`).
- The build enforces both with StyleCop (SA1402, SA1649); `stylecop.json` sets the type kinds.
- Nested types belong to their containing type and stay in its file.
- A large type may be split with `partial` across files named `<Type>.<Part>.cs` (e.g. `GraphBuilder.Settings.cs`).

## Harmony patches

- One patch class per patched method, in `Patches/`, named `<Type>_<Method>` (e.g. `LEV_SaveLoad_SaveFile`).
- Target with `[HarmonyPatch(typeof(Type), nameof(Type.Method))]`, and mark each method `[HarmonyPrefix]` / `[HarmonyPostfix]`.
- Patch classes are `internal` and static-only.
- Keep patch bodies thin: raise an event or call into a mod class, and keep the logic there where it can be tested.
- Patch only what ZeepSDK doesn't already expose; prefer its APIs (`LevelEditorApi`, `StorageApi`, …).

## Unity lifecycle

- Subscribe to events in `Awake`/`Start` and unsubscribe from every one of them in `OnDestroy`.
- Keep scene searches (`FindObjectOfType` and friends) out of `Update`, other per-frame methods, and loops. Look things up once and cache the result.

## Names

Use the domain terms from `CONTEXT.md` in type, member, and file names, so the code reads in the same language as the design notes.
