internal record LoxResolver(LoxInterpreter Interpreter) : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    private enum FunctionTypes
    {
        None,
        Function
    }

    private readonly Stack<Dictionary<string, bool>> _scopes = new();
    private FunctionTypes _currentFunction = FunctionTypes.None;

    public event EventHandler<(int Line, int Column, string Message)>? Error;

    public object? VisitAssignExpr(LoxAssignExpression expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitBinaryExpr(LoxBinaryExpression expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitCallExpr(LoxCallExpression expr)
    {
        Resolve(expr.Callee);
        expr.Arguments.ForEach(Resolve); // This is equivalent to a foreach loop that just calls the resolve function with each argument as the only parameter
        return null;
    }

    public object? VisitGetExpr(LoxGetExpression expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitGroupingExpr(LoxGroupingExpression expr)
    {
        Resolve(expr.Expression);
        return null;
    }

    public object? VisitLiteralExpr(LoxLiteralExpression expr) => null; // Nothing to resolve here

    public object? VisitLogicalExpr(LoxLogicalExpression expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

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
        Resolve(expr.Right);
        return null;
    }

    public object? VisitVariableExpr(LoxVariableExpression expr)
    {
        if (_scopes.Any() && _scopes.Peek().TryGetValue(expr.Name.Lexeme!, out var defined) && defined == false)
        {
            TriggerError(expr.Name, "Can't read local variable in its own initializer.");
        }

        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitBlockStmt(LoxBlockStatement stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return null;
    }

    public object? VisitClassStmt(LoxClassStatement stmt)
    {
        throw new NotImplementedException();
    }

    public object? VisitExpressionStmt(LoxExpressionStatement stmt)
    {
        Resolve(stmt.Expr);
        return null;
    }

    public object? VisitFunctionStmt(LoxFunctionStatement stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        ResolveFunction(stmt, FunctionTypes.Function);
        return null;
    }

    public object? VisitIfStmt(LoxIfStatement stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch is not null)
        {
            Resolve(stmt.ElseBranch);
        }

        return null;
    }

    public object? VisitPrintStmt(LoxPrintStatement stmt)
    {
        Resolve(stmt.Expr);
        return null;
    }

    public object? VisitReturnStmt(LoxReturnStatement stmt)
    {
        if (_currentFunction == FunctionTypes.None)
        {
            TriggerError(stmt.Keyword, "Can't return from top-level code.");
        }

        if (stmt.Value is not null)
        {
            Resolve(stmt.Value);
        }

        return null;
    }

    public object? VisitBreakStmt(LoxBreakStatement stmt) => null; // There's nothing to resolve here

    public object? VisitContinueStmt(LoxContinueStatement stmt) => null; // There's nothing to resolve here

    public object? VisitVarStmt(LoxVarStatement stmt)
    {
        Declare(stmt.Name);
        if (stmt.Initializer is not null)
        {
            Resolve(stmt.Initializer);
        }
        Define(stmt.Name);
        return null;
    }

    public object? VisitWhileStmt(LoxWhileStatement stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return null;
    }

    public void Resolve(List<LoxStatementBase> statements) => statements.ForEach(Resolve);

    private void Resolve(LoxStatementBase statement) => statement.Accept(this);

    private void Resolve(LoxExpressionBase expression) => expression.Accept(this);

    private void BeginScope() => _scopes.Push(new Dictionary<string, bool>());

    private void EndScope() => _scopes.Pop();

    private void Declare(LoxToken name)
    {
        if (_scopes.Any() == false)
        {
            return; // Global variables don't need this, they're always in scope
        }

        var scope = _scopes.Peek();

        if (scope.ContainsKey(name.Lexeme!))
        {
            TriggerError(name, "Variable with this name already declared in this scope.");
        }

        scope.Add(name.Lexeme!, false);
    }

    private void Define(LoxToken name)
    {
        if (_scopes.Any() == false)
        {
            return; // Global variables don't need this, they're always in scope
        }
        var scope = _scopes.Peek();
        scope[name.Lexeme!] = true;
    }

    private void TriggerError(LoxToken token, string message) => Error?.Invoke(this, (token.Line, token.Column, message));

    private void ResolveLocal(LoxExpressionBase expr, LoxToken name)
    {
        int distance = 0;
        foreach (var scope in _scopes)
        {
            // Check if the current scope contains the variable name by its lexeme.
            if (scope.ContainsKey(name.Lexeme!))
            {
                // Found the variable!
                // The 'distance' is the number of scopes down from the top of the stack.
                Interpreter.Resolve(expr, distance);

                // Once resolved, we can stop searching.
                return;
            }

            // If the variable was not in the current scope, increment the distance
            // for the next scope further down the stack.
            distance++;
        }

        // If the loop finishes, the variable was not found in any local scope.
        // It's considered a global variable. Global variables don't need a distance recorded; they are looked up directly in the global environment.
        // So, no action is needed here for resolution distance.
    }

    private void ResolveFunction(LoxFunctionStatement function, FunctionTypes functionType)
    {
        var enclosingFunction = _currentFunction;
        _currentFunction = functionType;
        BeginScope();

        foreach (var param in function.Params)
        {
            Declare(param);
            Define(param);
        }
        Resolve(function.Body);

        EndScope();
        _currentFunction = enclosingFunction;
    }
}