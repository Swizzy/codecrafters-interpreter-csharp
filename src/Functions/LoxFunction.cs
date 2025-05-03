internal record LoxFunction(LoxFunctionStatement FunctionStatement, LoxEnvironment Closure) : ICallable
{
    public int Arity => FunctionStatement.Params.Count;

    public object? Call(LoxInterpreter interpreter, List<object?> arguments)
    {
        var environment = new LoxEnvironment(Closure);

        for (int i = 0; i < FunctionStatement.Params.Count; i++)
        {
            var param = FunctionStatement.Params[i];
            environment.Define(param.Lexeme!, arguments[i]);
        }

        interpreter.ExecuteBlock(FunctionStatement.Body, environment);
        return null;
    }

    public override string ToString() => $"<fn {FunctionStatement.Name.Lexeme}>";
}