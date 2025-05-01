internal record LoxWhileStatement(LoxExpressionBase Condition, LoxStatementBase Body) : LoxStatementBase(Condition.Line, Condition.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitWhileStmt(this);
}