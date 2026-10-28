namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public sealed partial class EditHistory
{
    private sealed class Step
    {
        public Step(StepKind kind, TextSnapshot before, TextSnapshot after, double time)
        {
            Kind = kind;
            Before = before;
            After = after;
            LastChanged = time;
        }

        public StepKind Kind { get; }
        public TextSnapshot Before { get; }
        public TextSnapshot After { get; set; }

        public double LastChanged { get; set; }
    }
}
