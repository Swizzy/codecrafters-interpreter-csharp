internal record LoxReturnStatement(LoxToken Keyword, LoxExpressionBase? Value) : LoxStatementBase(Keyword.Line, Keyword.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitReturnStmt(this);
}