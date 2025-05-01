internal record LoxSetExpression(LoxExpressionBase Object, LoxToken Name, LoxExpressionBase Value) : LoxExpressionBase(Object.Line, Object.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitSetExpr(this);
}