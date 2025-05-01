internal class LoxRuntimeErrorException(string message) : Exception(message)
{
    public LoxRuntimeErrorException(LoxToken token, string message) : this(message)
    {
        Line = token.Line;
        Column = token.Column;
    }

    public int Line { get; }
    public int Column { get; }
}