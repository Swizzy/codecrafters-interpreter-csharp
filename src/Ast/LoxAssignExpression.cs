internal record LoxAssignExpression(LoxToken Name, LoxExpressionBase Value) : LoxExpressionBase(Name.Line, Name.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitAssignExpr(this);
}