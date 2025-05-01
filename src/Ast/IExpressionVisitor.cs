internal interface IExpressionVisitor<out TResult>
{
    TResult VisitAssignExpr(LoxAssignExpression expr);
    TResult VisitBinaryExpr(LoxBinaryExpression expr);
    TResult VisitCallExpr(LoxCallExpression expr);
    TResult VisitGetExpr(LoxGetExpression expr);
    TResult VisitGroupingExpr(LoxGroupingExpression expr);
    TResult VisitLiteralExpr(LoxLiteralExpression expr);
    TResult VisitLogicalExpr(LoxLogicalExpression expr);
    TResult VisitSetExpr(LoxSetExpression expr);
    TResult VisitSuperExpr(LoxSuperExpression expr);
    TResult VisitThisExpr(LoxThisExpression expr);
    TResult VisitUnaryExpr(LoxUnaryExpression expr);
    TResult VisitVariableExpr(LoxVariableExpression expr);
}