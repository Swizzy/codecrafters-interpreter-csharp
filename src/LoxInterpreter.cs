internal class LoxInterpreter : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    private LoxEnvironment _environment = new();

    public event EventHandler<(int Line, int Column, string Message)>? Error;

    public void Interpret(LoxExpressionBase expression)
    {
        try
        {
            Console.WriteLine(Stringify(Evaluate(expression)));
        }
        catch (LoxRuntimeErrorException error)
        {
            Error?.Invoke(this, (error.Line, error.Column, error.Message));
        }
    }

    public void Interpret(List<LoxStatementBase> statements, bool evaluateLast = false)
    {
        try
        {
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        catch (LoxRuntimeErrorException error)
        {
            Error?.Invoke(this, (error.Line, error.Column, error.Message));
        }
    }

    public void ExecuteBlock(List<LoxStatementBase> statements, LoxEnvironment scopedEnvironment)
    {
        var previous = _environment;
        try
        {
            _environment = scopedEnvironment;
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous;
        }
    }

    private void Execute(LoxStatementBase statement) => statement.Accept(this);

    public object? Evaluate(LoxExpressionBase expr) => expr.Accept(this);

    public object? VisitAssignExpr(LoxAssignExpression expr)
    {
        var value = Evaluate(expr.Value);
        _environment.Assign(expr.Name, value);
        return value;
    }

    public object? VisitBinaryExpr(LoxBinaryExpression expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);

        switch (expr.Operator.TokenType)
        {
            case LoxTokenTypes.GREATER:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! > (double)right!;
            case LoxTokenTypes.GREATER_EQUAL:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! >= (double)right!;
            case LoxTokenTypes.LESS:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! < (double)right!;
            case LoxTokenTypes.LESS_EQUAL:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! <= (double)right!;
            case LoxTokenTypes.MINUS:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! - (double)right!;
            case LoxTokenTypes.EQUAL_EQUAL:
                return IsEqual(left, right);
            case LoxTokenTypes.BANG_EQUAL:
                return !IsEqual(left, right);
            case LoxTokenTypes.PLUS:
                return left switch
                {
                    double leftNum when right is double rightNum => leftNum + rightNum,
                    string leftStr when right is string rightStr => leftStr + rightStr,
                    //string leftStr2 when right is double rightNum2 => leftStr2 + rightNum2,
                    //double leftNum2 when right is string rightStr2 => leftNum2 + rightStr2,
                    //_ => throw new LoxRuntimeErrorException(expr.Operator, "Operands must be either two numbers, two strings or one of each on either side.")
                    _ => throw new LoxRuntimeErrorException(expr.Operator, "Operands must be either two numbers or two strings.")
                };
            case LoxTokenTypes.STAR:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! * (double)right!;
            case LoxTokenTypes.SLASH:
                CheckNumberOperands(expr.Operator, left, right);
                if ((double)right! == 0)
                {
                    throw new LoxRuntimeErrorException(expr.Operator, "Divide by 0 detected.");
                }
                return (double)left! / (double)right!;
            default:
                throw new NotImplementedException($"Unknown binary operator: {expr.Operator.TokenType}");
        }
    }

    public object? VisitCallExpr(LoxCallExpression expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitGetExpr(LoxGetExpression expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitGroupingExpr(LoxGroupingExpression expr) => Evaluate(expr.Expression);

    public object? VisitLiteralExpr(LoxLiteralExpression expr) => expr.Value.Literal;

    public object? VisitLogicalExpr(LoxLogicalExpression expr) => Evaluate(expr);

    public object? VisitSetExpr(LoxSetExpression expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitSuperExpr(LoxSuperExpression expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitThisExpr(LoxThisExpression expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitUnaryExpr(LoxUnaryExpression expr)
    {
        var right = Evaluate(expr.Right);

        switch (expr.Operator.TokenType)
        {
            case LoxTokenTypes.BANG:
                return !IsTruthy(right);
            case LoxTokenTypes.MINUS:
                CheckNumberOperand(expr.Operator, right);
                return -(double)right!;
            default:
                throw new NotImplementedException($"Unknown unary operator: {expr.Operator.TokenType}");
        }
    }

    public object? VisitVariableExpr(LoxVariableExpression expr) => _environment.Get(expr.Name);

    public object? VisitBlockStmt(LoxBlockStatement stmt)
    {
        ExecuteBlock(stmt.Statements, new LoxEnvironment(_environment));
        return null;
    }

    public object? VisitClassStmt(LoxClassStatement stmt)
    {
        throw new NotImplementedException();
    }

    public object? VisitExpressionStmt(LoxExpressionStatement stmt) => Evaluate(stmt.Expr);

    public object? VisitFunctionStmt(LoxFunctionStatement stmt)
    {
        throw new NotImplementedException();
    }

    public object? VisitIfStmt(LoxIfStatement stmt)
    {
        throw new NotImplementedException();
    }

    public object? VisitPrintStmt(LoxPrintStatement stmt)
    {
        var value = Evaluate(stmt.Expr);
        Console.WriteLine(Stringify(value));
        return null;
    }

    public object? VisitReturnStmt(LoxReturnStatement stmt)
    {
        throw new NotImplementedException();
    }

    public object? VisitVarStmt(LoxVarStatement stmt)
    {
        object? value = null;
        if (stmt.Initializer is not null)
        {
            value = Evaluate(stmt.Initializer);
        }

        _environment.Define(stmt.Name.Lexeme!, value);
        return null;
    }

    public object? VisitWhileStmt(LoxWhileStatement stmt)
    {
        throw new NotImplementedException();
    }

    private static bool IsTruthy(object? obj)
    {
        return obj switch
        {
            null => false,
            bool b => b,
            _ => true
        };
    }


    private bool IsEqual(object? a, object? b)
    {
        return a switch
        {
            null when b is null => true,
            null => false,
            _ => a.Equals(b)
        };
    }

    private void CheckNumberOperand(LoxToken token, object? right)
    {
        if (right is double)
        {
            return;
        }

        throw new LoxRuntimeErrorException(token, "Operand must be a number.");
    }

    private void CheckNumberOperands(LoxToken token, object? left, object? right)
    {
        if (left is double && right is double)
        {
            return;
        }

        throw new LoxRuntimeErrorException(token, "Operands must be numbers.");
    }

    private string? Stringify(object? value)
    {
        return value switch
        {
            null => "nil",
            bool b => b.ToString().ToLower(),
            double d => d.ToString("0.##########"),
            _ => value.ToString()
        };
    }
}