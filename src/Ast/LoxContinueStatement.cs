internal record LoxContinueStatement(LoxToken Keyword) : LoxStatementBase(Keyword.Line, Keyword.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitContinueStmt(this);
}