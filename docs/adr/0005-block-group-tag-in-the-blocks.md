# Block Group membership is a tag inside the blocks

Each compiled block carries a tag in its own saved properties, `"<scriptId>:<ownUID>"`. A block belongs to a Script's Block Group only when the tag names that Script and the UID inside the tag is the block's own UID. The Script text lives in the level too, on a block of its own group. Recompile finds the group by scanning for valid tags, then replaces it as one undo step.

The alternative was a member list stored next to the Script in the mod's local storage. That list would have to stay in sync with editor undo/redo, which the mod doesn't control. It also stays on the player's machine, so it's lost when a level is copied, renamed or shared through the workshop. A tag inside the blocks travels with the level and is always as current as the blocks themselves.

The UID inside the tag exists because the editor copies every saved property when it duplicates a block. A pasted copy keeps the tag but gets a new UID, so it fails the check and is treated as an ordinary hand-placed block. Without that check, recompiling would delete the player's copies, or a copy could take over its original's group. `scriptId` is a GUID made when the Script is created, so renaming a Script doesn't orphan its group.

The cost: stale tags stay on copies, and a group whose Script text was deleted by hand becomes an orphan that the window has to offer to delete, untag or select.
