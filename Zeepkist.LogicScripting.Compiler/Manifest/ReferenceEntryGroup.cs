using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// Named rows under a heading in a <see cref="ReferenceEntryLayout"/>: the Settings, or the outputs per mode.
/// </summary>
public sealed class ReferenceEntryGroup
{
    public ReferenceEntryGroup(string heading, IReadOnlyList<BlockReferenceRow> rows)
    {
        Heading = heading;
        Rows = rows;
    }

    public string Heading { get; }

    public IReadOnlyList<BlockReferenceRow> Rows { get; }
}
