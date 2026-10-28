namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public static partial class EditCommands
{
    private readonly struct Change
    {
        public Change(int index, int length, string text)
        {
            Index = index;
            Length = length;
            Text = text;
        }

        public int Index { get; }
        public int Length { get; }
        public string Text { get; }
    }
}
