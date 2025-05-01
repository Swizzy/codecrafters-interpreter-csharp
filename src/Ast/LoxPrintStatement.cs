internal record LoxPrintStatement(LoxExpressionBase Expr) : LoxStatementBase(Expr.Line, Expr.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitPrintStmt(this);
}