internal abstract record LoxStatementBase(int Line, int Column)
{
    internal abstract TResult Accept<TResult>(IStatementVisitor<TResult> visitor);
}