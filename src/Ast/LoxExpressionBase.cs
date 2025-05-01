internal abstract record LoxExpressionBase(int Line, int Column)
{
    internal abstract TResult Accept<TResult>(IExpressionVisitor<TResult> visitor);
}