internal record LoxClassStatement(LoxToken Name, LoxVariableExpression? Superclass, List<LoxFunctionStatement> Methods) : LoxStatementBase(Name.Line, Name.Column)
{
    internal override TResult Accept<TResult>(IStatementVisitor<TResult> visitor) => visitor.VisitClassStmt(this);
}