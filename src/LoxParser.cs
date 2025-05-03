internal class LoxParser
{
    private readonly List<LoxToken> _tokens;
    private int _current;
    private readonly Stack<object?> _loopStack = new();

    public LoxParser(List<LoxToken> tokens) => _tokens = tokens;

    public event EventHandler<(LoxToken token, string Message)>? Error;

    public LoxExpressionBase? ParseExpression()
    {
        try
        {
            return expression();
        }
        catch (LoxParseErrorException)
        {
            return null;
        }
    }

    public List<LoxStatementBase> Parse()
    {
        var statements = new List<LoxStatementBase>();
        while (!isAtEnd())
        {
            var statement = declaration();
            if (statement != null)
            {
                statements.Add(statement);
            }
        }
        return statements;
    }

    private bool isAtEnd() => peek().TokenType == LoxTokenTypes.EOF;
    private LoxToken peek() => _tokens[_current];
    private LoxToken previous() => _tokens[_current - 1];

    private LoxToken consume(LoxTokenTypes type, string message)
    {
        if (check(type))
        {
            return advance();
        }
        throw error(peek(), message);
    }

    private LoxToken advance()
    {
        if (!isAtEnd())
        {
            _current++;
        }
        return previous();
    }
    private bool check(LoxTokenTypes type)
    {
        if (isAtEnd())
        {
            return false;
        }
        return peek().TokenType == type;
    }

    private bool match(params IEnumerable<LoxTokenTypes> types)
    {
        if (types.Any(check))
        {
            advance();
            return true;
        }
        return false;
    }

    private void synchronize()
    {
        advance();

        while (!isAtEnd())
        {
            if (previous().TokenType == LoxTokenTypes.SEMICOLON) return;

            switch (peek().TokenType)
            {
                case LoxTokenTypes.CLASS:
                case LoxTokenTypes.FUN:
                case LoxTokenTypes.VAR:
                case LoxTokenTypes.FOR:
                case LoxTokenTypes.IF:
                case LoxTokenTypes.WHILE:
                case LoxTokenTypes.PRINT:
                case LoxTokenTypes.RETURN:
                    return;
            }

            advance();
        }
    }

    private LoxParseErrorException error(LoxToken token, string message)
    {
        Error?.Invoke(this, (token, message));
        return new LoxParseErrorException();
    }

    private LoxStatementBase? declaration()
    {
        try
        {
            if (match(LoxTokenTypes.FUN))
            {
                return function("function");
            }
            if (match(LoxTokenTypes.VAR))
            {
                return varDeclaration();
            }
            return statement();
        }
        catch (LoxParseErrorException)
        {
            synchronize();
            return null;
        }
    }

    private LoxStatementBase? function(string kind)
    {
        var name = consume(LoxTokenTypes.IDENTIFIER, $"Expect {kind} name.");
        consume(LoxTokenTypes.LEFT_PAREN, $"Expect '(' after {kind} name.");

        var parameters = new List<LoxToken>();
        if (check(LoxTokenTypes.RIGHT_PAREN) == false)
        {
            do
            {
                if (parameters.Count >= 255)
                {
                    error(peek(), "Cannot have more than 255 parameters.");
                }
                parameters.Add(consume(LoxTokenTypes.IDENTIFIER, "Expect parameter name."));
            } while (match(LoxTokenTypes.COMMA));
        }
        consume(LoxTokenTypes.RIGHT_PAREN, $"Expect ')' after {kind} parameters.");

        consume(LoxTokenTypes.LEFT_BRACE, $"Expect '{{' before {kind} body.");
        var body = block();

        return new LoxFunctionStatement(name, parameters, body);
    }

    private LoxStatementBase varDeclaration()
    {
        var name = consume(LoxTokenTypes.IDENTIFIER, "Expect variable name.");

        LoxExpressionBase? initializer = null;
        if (match(LoxTokenTypes.EQUAL))
        {
            initializer = expression();
        }
        consume(LoxTokenTypes.SEMICOLON, "Expect ';' after variable declaration.");
        return new LoxVarStatement(name, initializer);
    }

    private LoxStatementBase statement()
    {
        if (match(LoxTokenTypes.FOR))
        {
            return forStatement();
        }

        if (match(LoxTokenTypes.IF))
        {
            return ifStatement();
        }

        if (match(LoxTokenTypes.PRINT))
        {
            return printStatement();
        }

        if (match(LoxTokenTypes.RETURN))
        {
            return returnStatement();
        }

        if (match(LoxTokenTypes.WHILE))
        {
            return whileStatement();
        }

        if (match(LoxTokenTypes.LEFT_BRACE))
        {
            var brace = previous();
            return new LoxBlockStatement(brace, block());
        }

        if (match(LoxTokenTypes.BREAK, LoxTokenTypes.CONTINUE)) // We match these together as they have exactly the same appearance and therefor just works...
        {
            var keywordToken = previous();
            if (_loopStack.Any() == false)
            {
                throw error(previous(), $"Cannot use '{keywordToken.Lexeme}' outside of a loop.");
            }

            consume(LoxTokenTypes.SEMICOLON, $"Expect ';' after '{keywordToken.Lexeme}'.");

            if (keywordToken.TokenType == LoxTokenTypes.BREAK)
            {
                return new LoxBreakStatement(keywordToken);
            }
            return new LoxContinueStatement(keywordToken);
        }
        return expressionStatement();
    }

    private LoxStatementBase returnStatement()
    {
        var keyword = previous();
        LoxExpressionBase? value = null;
        if (check(LoxTokenTypes.SEMICOLON) == false)
        {
            value = expression();
        }
        consume(LoxTokenTypes.SEMICOLON, "Expect ';' after return value.");
        return new LoxReturnStatement(keyword, value);
    }

    private LoxStatementBase forStatement()
    {
        consume(LoxTokenTypes.LEFT_PAREN, "Expect '(' after 'for'.");
        LoxStatementBase? initializer;
        if (match(LoxTokenTypes.SEMICOLON))
        {
            initializer = null; // This is an empty initializer
        }
        else if (match(LoxTokenTypes.VAR))
        {
            initializer = varDeclaration(); // This is a variable declaration
        }
        else
        {
            initializer = expressionStatement(); // This is an expression statement most likely an assignment
        }

        LoxExpressionBase? condition;
        if (check(LoxTokenTypes.SEMICOLON))
        {
            condition = new LoxLiteralExpression(previous() with { Literal = true, Lexeme = "true" });
        }
        else
        {
            condition = expression();
        }
        consume(LoxTokenTypes.SEMICOLON, "Expect ';' after loop condition.");

        LoxExpressionBase? increment = null;
        if (check(LoxTokenTypes.RIGHT_PAREN) == false)
        {
            increment = expression();
        }
        consume(LoxTokenTypes.RIGHT_PAREN, "Expect ')' after for clauses.");

        _loopStack.Push(null);
        var body = statement();
        _loopStack.Pop();

        if (increment != null)
        {
            body = new LoxBlockStatement(previous(), [body, new LoxExpressionStatement(increment)]);
        }

        body = new LoxWhileStatement(condition, body);
        if (initializer != null)
        {
            body = new LoxBlockStatement(previous(), [initializer, body]);
        }

        return body;
    }

    private LoxStatementBase whileStatement()
    {
        consume(LoxTokenTypes.LEFT_PAREN, "Expect '(' after 'while'.");
        var condition = expression();
        consume(LoxTokenTypes.RIGHT_PAREN, "Expect ')' after condition.");

        _loopStack.Push(null);

        var body = statement();

        _loopStack.Pop();

        return new LoxWhileStatement(condition, body);
    }

    private LoxStatementBase ifStatement()
    {
        consume(LoxTokenTypes.LEFT_PAREN, "Expect '(' after 'if'.");
        var condition = expression();
        consume(LoxTokenTypes.RIGHT_PAREN, "Expect ')' after condition.");

        var thenBranch = statement();

        LoxStatementBase? elseBranch = null;
        if (match(LoxTokenTypes.ELSE))
        {
            elseBranch = statement();
        }

        return new LoxIfStatement(condition, thenBranch, elseBranch);
    }

    private List<LoxStatementBase> block()
    {
        var statements = new List<LoxStatementBase>();

        while (!isAtEnd() && !check(LoxTokenTypes.RIGHT_BRACE))
        {
            var statement = declaration();
            if (statement != null)
            {
                statements.Add(statement);
            }
        }
        consume(LoxTokenTypes.RIGHT_BRACE, "Expect '}' after block.");
        return statements;
    }

    private LoxStatementBase expressionStatement()
    {
        var expr = expression();
        consume(LoxTokenTypes.SEMICOLON, "Expect ';' after expression.");
        return new LoxExpressionStatement(expr);
    }

    private LoxStatementBase printStatement()
    {
        var value = expression();
        consume(LoxTokenTypes.SEMICOLON, "Expect ';' after value.");
        return new LoxPrintStatement(value);
    }

    private LoxExpressionBase expression() => assignment();

    private LoxExpressionBase assignment()
    {
        var expr = or();

        if (match(LoxTokenTypes.EQUAL))
        {
            var equals = previous();
            var value = assignment();
            if (expr is LoxVariableExpression variable)
            {
                var name = variable.Name;
                return new LoxAssignExpression(name, value);
            }
            error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private LoxExpressionBase or()
    {
        var expr = and();

        while (match(LoxTokenTypes.OR))
        {
            var operatorToken = previous();
            var right = equality();
            expr = new LoxLogicalExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private LoxExpressionBase and()
    {
        var expr = equality();

        while (match(LoxTokenTypes.AND))
        {
            var operatorToken = previous();
            var right = equality();
            expr = new LoxLogicalExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private LoxExpressionBase equality()
    {
        var expr = comparison();

        while (match(LoxTokenTypes.BANG_EQUAL, LoxTokenTypes.EQUAL_EQUAL))
        {
            var operatorToken = previous();
            var right = comparison();
            expr = new LoxBinaryExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private LoxExpressionBase comparison()
    {
        var expr = term();
        while (match(LoxTokenTypes.GREATER, LoxTokenTypes.GREATER_EQUAL, LoxTokenTypes.LESS, LoxTokenTypes.LESS_EQUAL))
        {
            var operatorToken = previous();
            var right = term();
            expr = new LoxBinaryExpression(expr, operatorToken, right);
        }
        return expr;
    }

    private LoxExpressionBase term()
    {
        var expr = factor();
        while (match(LoxTokenTypes.MINUS, LoxTokenTypes.PLUS))
        {
            var operatorToken = previous();
            var right = factor();
            expr = new LoxBinaryExpression(expr, operatorToken, right);
        }
        return expr;
    }

    private LoxExpressionBase factor()
    {
        var expr = unary();
        while (match(LoxTokenTypes.SLASH, LoxTokenTypes.STAR))
        {
            var operatorToken = previous();
            var right = unary();
            expr = new LoxBinaryExpression(expr, operatorToken, right);
        }
        return expr;
    }

    private LoxExpressionBase unary()
    {
        if (match(LoxTokenTypes.BANG, LoxTokenTypes.MINUS))
        {
            var operatorToken = previous();
            var right = unary();
            return new LoxUnaryExpression(operatorToken, right);
        }
        return call();
    }

    private LoxExpressionBase call()
    {
        var expr = primary();
        while (true)
        {
            if (match(LoxTokenTypes.LEFT_PAREN))
            {
                expr = finishCall(expr);
            }
            else
            {
                break;
            }
        }
        return expr;
    }

    private LoxExpressionBase finishCall(LoxExpressionBase callee)
    {
        var arguments = new List<LoxExpressionBase>();
        if (check(LoxTokenTypes.RIGHT_PAREN) == false)
        {
            do
            {
                if (arguments.Count >= 255)
                {
                    error(peek(), "Cannot have more than 255 arguments.");
                }
                arguments.Add(expression());
            } while (match(LoxTokenTypes.COMMA));
        }
        var paren = consume(LoxTokenTypes.RIGHT_PAREN, "Expect ')' after arguments.");
        return new LoxCallExpression(callee, paren, arguments);
    }

    private LoxExpressionBase primary()
    {
        if (match(LoxTokenTypes.FALSE))
        {
            return new LoxLiteralExpression(previous() with { Literal = false });
        }
        if (match(LoxTokenTypes.TRUE))
        {
            return new LoxLiteralExpression(previous() with { Literal = true });
        }
        if (match(LoxTokenTypes.NUMBER, LoxTokenTypes.STRING, LoxTokenTypes.NIL))
        {
            return new LoxLiteralExpression(previous());
        }
        if (match(LoxTokenTypes.IDENTIFIER))
        {
            return new LoxVariableExpression(previous());
        }
        if (match(LoxTokenTypes.LEFT_PAREN))
        {
            var expr = expression();
            consume(LoxTokenTypes.RIGHT_PAREN, "Expect ')' after expression.");
            return new LoxGroupingExpression(expr);
        }

        throw error(peek(), "Expect expression.");
    }
}