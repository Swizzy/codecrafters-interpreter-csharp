internal record LoxGetExpression(LoxExpressionBase Object, LoxToken Name) : LoxExpressionBase(Object.Line, Object.Column)
{
    internal override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor) => visitor.VisitGetExpr(this);
}