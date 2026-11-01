namespace TNRD.Zeepkist.LogicScripting.InGameTests;

internal sealed class RunStep
{
    public string Name { get; set; }
    public bool Passed { get; set; }
    public string Detail { get; set; } = "";

    public static RunStep Pass(string name)
    {
        return new RunStep { Name = name, Passed = true };
    }

    public static RunStep Fail(string name, string detail)
    {
        return new RunStep { Name = name, Passed = false, Detail = detail };
    }
}
