internal interface ICallable
{
    int Arity { get; }
    object? Call(LoxInterpreter interpreter, List<object?> arguments);
}