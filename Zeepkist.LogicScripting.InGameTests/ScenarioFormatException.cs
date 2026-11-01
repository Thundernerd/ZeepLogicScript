using System;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// A step's line can't be read. <see cref="ScenarioParser"/> adds the line number to the message.
/// </summary>
internal sealed class ScenarioFormatException : Exception
{
    public ScenarioFormatException(string message)
        : base(message)
    {
    }
}
