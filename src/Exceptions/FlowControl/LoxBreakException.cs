internal class LoxBreakException(LoxToken token) : Exception
{
    public int Line => token.Line;
    public int Column => token.Column;
}