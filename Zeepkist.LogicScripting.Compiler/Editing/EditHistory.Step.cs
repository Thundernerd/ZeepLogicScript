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
            Time = time;
        }

        public StepKind Kind { get; }
        public TextSnapshot Before { get; }
        public TextSnapshot After { get; set; }

        /// <summary>
        /// When the step last changed.
        /// </summary>
        public double Time { get; set; }
    }
}
