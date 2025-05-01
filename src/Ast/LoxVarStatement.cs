internal record LoxVarStatement(LoxToken Name, LoxExpressionBase? Initializer) : LoxStatementBase(Name.Line, Name.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitVarStmt(this);
}