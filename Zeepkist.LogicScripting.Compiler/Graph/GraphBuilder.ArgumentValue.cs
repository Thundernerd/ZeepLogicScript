namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a call passes to one of a Function's inputs: a value, or the constant for a Setting input.
    /// </summary>
    private sealed class ArgumentValue
    {
        public ArgumentValue(Resolved value)
        {
            Value = value;
        }

        public ArgumentValue(SettingConstant constant)
        {
            Constant = constant;
        }

        public Resolved Value { get; }
        public SettingConstant Constant { get; }

        /// <summary>
        /// Makes the input stand for this in the body's scope. A value that failed, already reported, stands for nothing.
        /// </summary>
        public void BindTo(Scope scope, string input)
        {
            if (Constant != null)
                scope.SettingConstants.Add(input, Constant);
            else if (Value is FailedValue)
                scope.FailedNames.Add(input);
            else
                scope.Names.Add(input, Value);
        }
    }
}
