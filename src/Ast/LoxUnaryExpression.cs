internal record LoxUnaryExpression(LoxToken Operator, LoxExpressionBase Right) : LoxExpressionBase(Operator.Line, Operator.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitUnaryExpr(this);
}