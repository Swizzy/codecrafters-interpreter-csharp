internal record LoxVariableExpression(LoxToken Name) : LoxExpressionBase(Name.Line, Name.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitVariableExpr(this);
}