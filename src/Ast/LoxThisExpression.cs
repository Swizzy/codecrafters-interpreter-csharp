internal record LoxThisExpression(LoxToken Keyword) : LoxExpressionBase(Keyword.Line, Keyword.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitThisExpr(this);
}