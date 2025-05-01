using System.Text;

internal class AstPrinter : IExpressionVisitor<string>, IStatementVisitor<string>
{
    internal string Print(LoxExpressionBase expr) => expr.Accept(this);

    internal string Print(LoxStatementBase stmt) => stmt.Accept(this);

    public string VisitBlockStmt(LoxBlockStatement stmt)
    {
        var builder = new StringBuilder();
        builder.Append("(block");

        foreach (var statement in stmt.Statements)
        {
            builder.Append(" ");
            builder.Append(statement.Accept(this));
        }

        builder.Append(")");
        return builder.ToString();
    }

    public string VisitClassStmt(LoxClassStatement stmt)
    {
        var builder = new StringBuilder();
        builder.Append($"(class {stmt.Name.Lexeme}!");

        if (stmt.Superclass is not null)
        {
            builder.Append($" < {Print(stmt.Superclass)}");
        }

        foreach (var method in stmt.Methods)
        {
            builder.Append(" ");
            builder.Append(Print(method));
        }

        builder.Append(")");
        return builder.ToString();
    }

    public string VisitExpressionStmt(LoxExpressionStatement stmt) => Parenthesize(";", stmt.Expr);

    public string VisitFunctionStmt(LoxFunctionStatement stmt)
    {
        var builder = new StringBuilder();
        builder.Append($"(fun {stmt.Name.Lexeme}!(");

        for (var i = 0; i < stmt.Params.Count; i++)
        {
            if (i > 0) builder.Append(" ");
            builder.Append(stmt.Params[i].Lexeme!);
        }

        builder.Append(") ");

        foreach (var body in stmt.Body)
        {
            builder.Append(body.Accept(this));
        }

        builder.Append(")");
        return builder.ToString();
    }

    public string VisitIfStmt(LoxIfStatement stmt)
    {
        if (stmt.ElseBranch is null)
        {
            return Parenthesize2("if", stmt.Condition, stmt.ThenBranch);
        }

        return Parenthesize2("if-else", stmt.Condition, stmt.ThenBranch, stmt.ElseBranch);
    }

    public string VisitPrintStmt(LoxPrintStatement stmt) => Parenthesize("print", stmt.Expr);

    public string VisitReturnStmt(LoxReturnStatement stmt)
    {
        if (stmt.Value is null)
        {
            return "(return)";
        }
        return Parenthesize("return", stmt.Value);
    }

    public string VisitVarStmt(LoxVarStatement stmt)
    {
        if (stmt.Initializer is null)
        {
            return Parenthesize2("var", stmt.Name); // Name token's lexeme handled in Transform
        }

        return Parenthesize2("var", stmt.Name, "=", stmt.Initializer); // Name token's lexeme handled in Transform
    }

    public string VisitWhileStmt(LoxWhileStatement stmt) => Parenthesize2("while", stmt.Condition, stmt.Body);

    public string VisitAssignExpr(LoxAssignExpression expr) => Parenthesize2("=", expr.Name.Lexeme!, expr.Value);

    public string VisitBinaryExpr(LoxBinaryExpression expr) => Parenthesize(expr.Operator.Lexeme!, expr.Left, expr.Right);

    public string VisitCallExpr(LoxCallExpression expr) => Parenthesize2("call", expr.Callee, expr.Arguments);

    public string VisitGetExpr(LoxGetExpression expr) => Parenthesize2(".", expr.Object, expr.Name.Lexeme!);

    public string VisitGroupingExpr(LoxGroupingExpression expr) => Parenthesize("group", expr.Expression);

    public string VisitLiteralExpr(LoxLiteralExpression expr)
    {
        return expr.Value.Literal switch
        {
            null => "nil",
            bool b => b.ToString().ToLower(),
            double d => d.ToString("0.0#########"),
            _ => expr.Value.Literal.ToString()!
        };
    }

    public string VisitLogicalExpr(LoxLogicalExpression expr) => Parenthesize(expr.Operator.Lexeme!, expr.Left, expr.Right);

    public string VisitSetExpr(LoxSetExpression expr) => Parenthesize2("=", expr.Object, expr.Name.Lexeme!, expr.Value);

    public string VisitSuperExpr(LoxSuperExpression expr) => Parenthesize2("super", expr.Method);

    public string VisitThisExpr(LoxThisExpression expr) => "this";

    public string VisitUnaryExpr(LoxUnaryExpression expr) => Parenthesize(expr.Operator.Lexeme!, expr.Right);

    public string VisitVariableExpr(LoxVariableExpression expr) => expr.Name.Lexeme!;

    private string Parenthesize(string name, params LoxExpressionBase[] exprs)
    {
        var builder = new StringBuilder();

        builder.Append('(').Append(name);
        foreach (var expr in exprs)
        {
            builder.Append(' ');
            builder.Append(expr.Accept(this));
        }
        builder.Append(')');

        return builder.ToString();
    }

    private string Parenthesize2(string name, params object[] parts)
    {
        var builder = new StringBuilder();

        builder.Append('(').Append(name);
        Transform(builder, parts);
        builder.Append(')');

        return builder.ToString();
    }

    private void Transform(StringBuilder builder, params object[] parts)
    {
        foreach (var part in parts)
        {
            builder.Append(' ');
            switch (part)
            {
                case LoxExpressionBase expr:
                    builder.Append(expr.Accept(this));
                    break;
                case LoxStatementBase stmt:
                    builder.Append(stmt.Accept(this));
                    break;
                case LoxToken token:
                    builder.Append(token.Lexeme!);
                    break;
                case IEnumerable<object> list:
                    Transform(builder, list.ToArray());
                    break;
                default:
                    builder.Append(part);
                    break;
            }
        }
    }
}
