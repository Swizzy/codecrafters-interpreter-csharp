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

        throw new LoxRuntimeErrorException(token, $"Undefined variable '{token.Lexeme}'.");
    }

    public object? GetAt(int distance, LoxToken name) => Ancestor(distance, name).Get(name);

    public void Assign(LoxToken name, object? value)
    {
        if (_values.ContainsKey(name.Lexeme!))
        {
            _values[name.Lexeme!] = value;
            return;
        }

        throw new LoxRuntimeErrorException(name, "Undefined variable '" + name.Lexeme + "'.");
    }

    public void AssignAt(int distance, LoxToken name, object? value) => Ancestor(distance, name).Assign(name, value);

    private LoxEnvironment Ancestor(int distance, LoxToken token)
    {
        var environment = this;
        for (int i = 0; i < distance; i++)
        {
            environment = environment._enclosing ?? throw new LoxRuntimeErrorException(token, "No enclosing environment found.");
        }
        return environment;
    }
}