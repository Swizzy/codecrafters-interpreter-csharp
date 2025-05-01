internal record LoxSuperExpression(LoxToken Keyword, LoxToken Method) : LoxExpressionBase(Keyword.Line, Keyword.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitSuperExpr(this);
}