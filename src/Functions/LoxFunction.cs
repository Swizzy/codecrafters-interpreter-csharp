internal record LoxFunction(LoxFunctionStatement FunctionStatement, LoxEnvironment Closure, bool IsInitializer = false) : ICallable
{
    private object? BoundInstance => Closure.Get(new LoxToken(LoxTokenTypes.THIS, "this", null, FunctionStatement.Line, FunctionStatement.Column));

    public int Arity => FunctionStatement.Params.Count;

    public object? Call(LoxInterpreter interpreter, List<object?> arguments)
    {
        var environment = new LoxEnvironment(Closure);

        for (int i = 0; i < FunctionStatement.Params.Count; i++)
        {
            var param = FunctionStatement.Params[i];
            environment.Define(param.Lexeme!, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(FunctionStatement.Body, environment);
        }
        catch (LoxReturnException ret)
        {
            if (IsInitializer)
            {
                return BoundInstance;
            }

            return ret.Value;
        }

        if (IsInitializer)
        {
            return BoundInstance;
        }
        return null;
    }

    public override string ToString() => $"<fn {FunctionStatement.Name.Lexeme}>";

    public LoxFunction Bind(LoxInstance instance)
    {
        var environment = new LoxEnvironment(Closure);

        environment.Define("this", instance);

        return this with { Closure = environment };
    }
}