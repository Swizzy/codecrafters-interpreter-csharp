internal record LoxGroupingExpression(LoxExpressionBase Expression) : LoxExpressionBase(Expression.Line, Expression.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitGroupingExpr(this);
}