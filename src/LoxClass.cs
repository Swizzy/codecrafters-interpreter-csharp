internal record LoxClass(LoxToken Token, Dictionary<string, LoxFunction> Methods) : ICallable
{
    private LoxFunction? Initializer => FindMethod("init");

    public int Arity => Initializer?.Arity ?? 0;

    public object Call(LoxInterpreter interpreter, List<object?> arguments)
    {
        var instance = new LoxInstance(this);

        var initializer = Initializer;
        if (initializer is not null)
        {
            initializer.Bind(instance).Call(interpreter, arguments);
        }

        return instance;
    }

    public override string ToString() => Token.Lexeme!;

    public LoxFunction? FindMethod(string name) => Methods.GetValueOrDefault(name);
}