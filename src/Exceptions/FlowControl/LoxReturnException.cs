internal class LoxReturnException(LoxToken token, object? value) : Exception
{
    public object? Value => value;
    public int Line => token.Line;
    public int Column => token.Column;
}