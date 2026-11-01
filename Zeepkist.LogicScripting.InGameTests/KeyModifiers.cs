using System;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// The same values as Unity's <c>EventModifiers</c>, for the same reason as <see cref="KeyPress.Key"/>.
/// </summary>
[Flags]
internal enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Command = 8,
}
