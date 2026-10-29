# Functions expand inline; Libraries stay local

A Script can define its own Functions: `function name(inputs) -> outputs` … `end`. Each call expands the body inline into ordinary Logic Blocks, as if the player had written that wiring by hand, so a level still plays without the mod. This partly supersedes ADR 0001's "no functions": what ADR 0001 rejects is code that runs at Compile time, like Lua's loops and conditionals, which players would expect to act at runtime. A Function runs nothing; it names a wiring pattern, the same way a Block Function names a block.

Each body is its own scope. It sees only its inputs, its own definitions and other Functions, never the caller's names, and follows ADR 0002 on its own, so a forward reference inside a body still has to sit in `delay(...)`. An argument is evaluated once at the call and shared, like a definition, so using an input twice doesn't place its blocks twice. A Function can't call itself, directly or through others: expansion would never end, and feedback already has its place in `delay(...)`.

Functions stay local to the Script that defines them. Sharing them between Scripts or levels, as a library, would tie a Script's meaning to text outside it, which the level doesn't store (ADR 0006), and a recompile could then change blocks the player didn't touch.
