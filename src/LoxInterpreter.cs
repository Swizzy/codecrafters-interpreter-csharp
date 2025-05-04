internal class LoxInterpreter : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    private readonly LoxEnvironment _globals = new();
    private readonly Dictionary<LoxExpressionBase, int> _locals = new();
    private LoxEnvironment _environment;

    public event EventHandler<(int Line, int Column, string Message)>? Error;

    public LoxInterpreter()
    {
        _environment = _globals;
        _environment.Define("clock", new LoxClock());
    }

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

    public void Resolve(LoxExpressionBase expr, int scopeIndex) => _locals[expr] = scopeIndex;

    public object? Evaluate(LoxExpressionBase expr) => expr.Accept(this);

    public object? VisitAssignExpr(LoxAssignExpression expr)
    {
        var value = Evaluate(expr.Value);
        AssignVariable(expr.Name, expr, value);
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
                return (double)left! / (double)right;
            default:
                throw new NotImplementedException($"Unknown binary operator: {expr.Operator.TokenType}");
        }
    }

    public object? VisitCallExpr(LoxCallExpression expr)
    {
        var callee = Evaluate(expr.Callee);

        var arguments = expr.Arguments.Select(Evaluate).ToList();

        if (callee is ICallable function)
        {
            if (arguments.Count != function.Arity)
            {
                throw new LoxRuntimeErrorException(expr.Paren, $"Expected {function.Arity} arguments but got {arguments.Count}.");
            }

            return function.Call(this, arguments);
        }

        throw new LoxRuntimeErrorException(expr.Paren, "Can only call functions and classes.");

    }

    public object? VisitGetExpr(LoxGetExpression expr)
    {
        var obj = Evaluate(expr.Object);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expr.Name);
        }
        throw new LoxRuntimeErrorException(expr.Name, "Only instances have properties.");
    }

    public object? VisitGroupingExpr(LoxGroupingExpression expr) => Evaluate(expr.Expression);

    public object? VisitLiteralExpr(LoxLiteralExpression expr) => expr.Value.Literal;

    public object? VisitLogicalExpr(LoxLogicalExpression expr)
    {
        var left = Evaluate(expr.Left);

        if (expr.Operator.TokenType == LoxTokenTypes.OR)
        {
            if (IsTruthy(left))
            {
                return left;
            }
        }
        else
        {
            if (IsTruthy(left) == false)
            {
                return left;
            }
        }

        return Evaluate(expr.Right);
    }

    public object? VisitSetExpr(LoxSetExpression expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj is LoxInstance instance)
        {
            return instance.Set(expr.Name, Evaluate(expr.Value));
        }

        throw new LoxRuntimeErrorException(expr.Name, "Only instances have properties.");

    }

    public object VisitSuperExpr(LoxSuperExpression expr)
    {
        var distance = _locals[expr];
        var super = (LoxClass)_environment.GetAt(distance, expr.Keyword)!;
        var obj = (LoxInstance)_environment.GetAt(distance - 1, expr.Keyword with { Lexeme = "this" })!;

        var method = super.FindMethod(expr.Method.Lexeme!) ?? throw new LoxRuntimeErrorException(expr.Method, "Undefined property '" + expr.Method.Lexeme + "'.");

        return method.Bind(obj);
    }

    public object? VisitThisExpr(LoxThisExpression expr) => LookupVariable(expr.Keyword, expr);

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

    public object? VisitVariableExpr(LoxVariableExpression expr) => LookupVariable(expr.Name, expr);

    public object? VisitBlockStmt(LoxBlockStatement stmt)
    {
        ExecuteBlock(stmt.Statements, new LoxEnvironment(_environment));
        return null;
    }

    public object? VisitClassStmt(LoxClassStatement stmt)
    {
        LoxClass? superClass = null;
        if (stmt.Superclass is not null)
        {
            superClass = Evaluate(stmt.Superclass) as LoxClass;
            if (superClass is null)
            {
                throw new LoxRuntimeErrorException(stmt.Superclass.Name, "Superclass must be a class.");
            }
        }

        _environment.Define(stmt.Name.Lexeme!, null);

        var enclosingEnvironment = _environment; // We need a reference to the current environment so we can restore it after processing the methods in case we replace it for the possible super class reference

        if (stmt.Superclass is not null)
        {
            _environment = new LoxEnvironment(_environment);
            _environment.Define("super", superClass);
        }

        var methods = new Dictionary<string, LoxFunction>();
        foreach (var method in stmt.Methods)
        {
            var function = new LoxFunction(method, _environment, method.Name.Lexeme!.Equals("init"));
            //TODO: Disallow same name methods?
            methods[method.Name.Lexeme!] = function;
        }

        _environment = enclosingEnvironment;

        _environment.Assign(stmt.Name, new LoxClass(stmt.Name, superClass, methods));
        return null;
    }

    public object? VisitExpressionStmt(LoxExpressionStatement stmt) => Evaluate(stmt.Expr);

    public object? VisitFunctionStmt(LoxFunctionStatement stmt)
    {
        var function = new LoxFunction(stmt, _environment);
        _environment.Define(stmt.Name.Lexeme!, function);
        return null;
    }

    public object? VisitIfStmt(LoxIfStatement stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch is not null)
        {
            Execute(stmt.ElseBranch);
        }
        return null;
    }

    public object? VisitPrintStmt(LoxPrintStatement stmt)
    {
        var value = Evaluate(stmt.Expr);
        Console.WriteLine(Stringify(value));
        return null;
    }

    public object VisitReturnStmt(LoxReturnStatement stmt)
    {
        object? value = null;
        if (stmt.Value is not null)
        {
            value = Evaluate(stmt.Value);
        }
        throw new LoxReturnException(stmt.Keyword, value);
    }

    public object VisitBreakStmt(LoxBreakStatement stmt) => throw new LoxBreakException(stmt.Keyword);

    public object VisitContinueStmt(LoxContinueStatement stmt) => throw new LoxContinueException(stmt.Keyword);

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
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            try
            {
                Execute(stmt.Body);
            }
            catch (LoxBreakException)
            {
                break; // We're done with this loop, just break out of it...
            }
            catch (LoxContinueException)
            {
                // Continue with execution, we just wanted to end up back here again
            }
        }
        return null;
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

    private void Execute(LoxStatementBase statement) => statement.Accept(this);

    private object? LookupVariable(LoxToken name, LoxExpressionBase expr)
    {
        if (_locals.TryGetValue(expr, out var distance))
        {
            return _environment.GetAt(distance, name);
        }

        return _globals.Get(name);
    }

    private void AssignVariable(LoxToken name, LoxExpressionBase expr, object? value)
    {
        if (_locals.TryGetValue(expr, out var distance))
        {
            _environment.AssignAt(distance, name, value);
            return;
        }

        _globals.Assign(name, value);
    }
}