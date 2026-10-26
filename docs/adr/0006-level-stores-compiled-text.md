# The level stores compiled text; drafts stay local

Each Script's id, name, text and language version are stored in the level, on the first block of its Block Group, and they're written only by Compile, as part of the Compile's undo step. Text typed since the last Compile is a Draft, kept in the mod's local storage under the Script's id, along with a hash of the compiled text it started from.

The level has no free-form place for extra data, so a block has to carry the text. Writing the latest typed text into the level at save time would keep drafts in the level too, but that write wouldn't be part of the editor's undo, wouldn't mark the level as changed, and would hand other players text that doesn't match the blocks. Writing only at Compile keeps the level's text and its blocks in step. Undoing a Compile restores both, and anyone who opens the level sees the text that produced its logic.

Drafts are keyed by the Script's id rather than the level's file name, so they survive renaming or copying the level. They are only used when their base hash matches the level's compiled text, so a stale draft never overwrites newer compiled work.

The cost: a Script that has never been compiled exists only in the open window, and an empty Script has no block to live on, so it's no longer stored in the level after Compile.
