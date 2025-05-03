internal record LoxBreakStatement(LoxToken Keyword) : LoxStatementBase(Keyword.Line, Keyword.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitBreakStmt(this);
}