internal class LoxClock : ICallable
{
    public int Arity => 0;

    public object Call(LoxInterpreter interpreter, List<object?> arguments) => (DateTime.Now - DateTime.UnixEpoch).TotalSeconds;

    public override string ToString() => "<native fn>";
}