# Custom Script language instead of Lua

ZeepSDK already embeds MoonSharp Lua, but a Script is a custom language with its own parser. A Script only describes wiring, so Lua's main strength (loops, functions and conditionals that run at Compile time) isn't wanted. Worse, players would expect a Lua `if` or `for` to act at runtime. MoonSharp is Lua 5.2, so `&`, `|` and `~` don't exist and `a & b` couldn't be written naturally. Syntax errors only come as a formatted message string. A small custom language gives operators that match the Logic Blocks and exact line and column for every error, at the cost of maintaining a parser.

ADR 0008 partly supersedes this: a Script can define Functions, which expand inline into wiring and run nothing at Compile time.
