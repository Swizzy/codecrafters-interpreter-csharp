internal record LoxToken
{
    public LoxTokenTypes TokenType { get; }
    public string? Lexeme { get; }
    public object? Literal { get; init; }
    public int Line { get; }
    public int Column { get; }

    public LoxToken(LoxTokenTypes tokenType, string? lexeme, object? literal, int line, int column)
    {
        TokenType = tokenType;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
        Column = column;
    }

    private string? StringifyLiteral()
    {
        if (Literal is null)
        {
            return "null";
        }

        if (Literal is double d)
        {
            return d.ToString("0.0###");
        }

        return Literal.ToString();
    }

    public override string ToString() => $"{TokenType} {Lexeme} {StringifyLiteral()}";
}