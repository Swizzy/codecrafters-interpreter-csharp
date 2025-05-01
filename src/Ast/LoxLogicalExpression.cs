internal record LoxLogicalExpression(LoxExpressionBase Left, LoxToken Operator, LoxExpressionBase Right) : LoxExpressionBase(Left.Line, Left.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitLogicalExpr(this);
}