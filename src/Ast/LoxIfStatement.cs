internal record LoxIfStatement(LoxExpressionBase Condition, LoxStatementBase ThenBranch, LoxStatementBase? ElseBranch) : LoxStatementBase(Condition.Line, Condition.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitIfStmt(this);
}