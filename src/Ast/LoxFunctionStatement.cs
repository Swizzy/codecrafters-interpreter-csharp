internal record LoxFunctionStatement(LoxToken Name, List<LoxToken> Params, List<LoxStatementBase> Body) : LoxStatementBase(Name.Line, Name.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitFunctionStmt(this);
}