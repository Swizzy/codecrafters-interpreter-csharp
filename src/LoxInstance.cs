internal record LoxInstance(LoxClass LoxClass)
{
    private readonly Dictionary<string, object?> _fields = new();
    public override string ToString() => LoxClass + " instance";

    public object? Get(LoxToken name)
    {
        if (_fields.TryGetValue(name.Lexeme!, out var value))
        {
            return value;
        }

        var method = LoxClass.FindMethod(name.Lexeme);
        if (method is not null)
        {
            return method.Bind(this);
        }

        throw new LoxRuntimeErrorException(name, $"Undefined property '{name.Lexeme}'");
    }

    public object? Set(LoxToken name, object? value) => _fields[name.Lexeme!] = value;
}