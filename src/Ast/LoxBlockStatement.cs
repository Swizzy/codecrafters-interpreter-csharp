internal record LoxBlockStatement(LoxToken Token, List<LoxStatementBase> Statements) : LoxStatementBase(Token.Line, Token.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitBlockStmt(this);
}