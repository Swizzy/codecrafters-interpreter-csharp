internal record LoxLiteralExpression(LoxToken Value) : LoxExpressionBase(Value.Line, Value.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitLiteralExpr(this);
}