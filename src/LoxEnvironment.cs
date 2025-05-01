internal class LoxEnvironment
{
    private readonly LoxEnvironment? _enclosing;

    private readonly Dictionary<string, object?> _values = new();

    public LoxEnvironment(LoxEnvironment? enclosing = null) => _enclosing = enclosing;

    public void Define(string name, object? value) => _values[name] = value;

    public object? Get(LoxToken token)
    {
        if (_values.TryGetValue(token.Lexeme!, out var value))
        {
            return value;
        }

        if (_enclosing is not null)
        {
            return _enclosing.Get(token);
        }

        throw new LoxRuntimeErrorException(token, $"Undefined variable '{token.Lexeme}'.");
    }

    public void Assign(LoxToken name, object? value)
    {
        if (_values.ContainsKey(name.Lexeme!))
        {
            _values[name.Lexeme!] = value;
            return;
        }

        if (_enclosing is not null)
        {
            _enclosing.Assign(name, value);
            return;
        }

        throw new LoxRuntimeErrorException(name, "Undefined variable '" + name.Lexeme + "'.");
    }
}