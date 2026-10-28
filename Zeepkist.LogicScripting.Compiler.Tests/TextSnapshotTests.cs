using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class TextSnapshotTests
{
    [Fact]
    public void ADefaultSnapshot_IsAnEmptyText()
    {
        var snapshot = default(TextSnapshot);

        Assert.Equal(string.Empty, snapshot.Text);
        Assert.Equal(new TextSnapshot(string.Empty, 0), snapshot);
        Assert.Equal(new TextSnapshot(string.Empty, 0).GetHashCode(), snapshot.GetHashCode());
    }
}
