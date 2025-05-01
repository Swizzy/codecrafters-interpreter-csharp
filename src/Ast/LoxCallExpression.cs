internal record LoxCallExpression(LoxExpressionBase Callee, LoxToken Paren, List<LoxExpressionBase> Arguments) : LoxExpressionBase(Callee.Line, Callee.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitCallExpr(this);
}