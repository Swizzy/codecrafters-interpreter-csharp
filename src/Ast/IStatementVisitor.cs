internal interface IStatementVisitor<out TResult>
{
    TResult VisitBlockStmt(LoxBlockStatement stmt);
    TResult VisitClassStmt(LoxClassStatement stmt);
    TResult VisitExpressionStmt(LoxExpressionStatement stmt);
    TResult VisitFunctionStmt(LoxFunctionStatement stmt);
    TResult VisitIfStmt(LoxIfStatement stmt);
    TResult VisitPrintStmt(LoxPrintStatement stmt);
    TResult VisitReturnStmt(LoxReturnStatement stmt);
    TResult VisitBreakStmt(LoxBreakStatement stmt);
    TResult VisitContinueStmt(LoxContinueStatement stmt);
    TResult VisitVarStmt(LoxVarStatement stmt);
    TResult VisitWhileStmt(LoxWhileStatement stmt);
}