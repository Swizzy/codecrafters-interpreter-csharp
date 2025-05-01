internal class LoxParser
{
    private readonly List<LoxToken> _tokens;
    private int _current;

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

    public List<LoxStatementBase>? Parse()
    {
        var statements = new List<LoxStatementBase>();
        while (!isAtEnd())
        {
            statements.Add(declaration());
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

    private LoxStatementBase declaration()
    {
        try
        {
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

    private LoxStatementBase varDeclaration()
    {
        var name = consume(LoxTokenTypes.IDENTIFIER, "Expect variable name.");

        LoxExpressionBase initializer = null;
        if (match(LoxTokenTypes.EQUAL))
        {
            initializer = expression();
        }
        consume(LoxTokenTypes.SEMICOLON, "Expect ';' after variable declaration.");
        return new LoxVarStatement(name, initializer);
    }

    private LoxStatementBase statement()
    {
        if (match(LoxTokenTypes.PRINT))
        {
            return printStatement();
        }

        if (match(LoxTokenTypes.LEFT_BRACE))
        {
            var brace = previous();
            return new LoxBlockStatement(brace, block());
        }
        return expressionStatement();
    }

    private List<LoxStatementBase> block()
    {
        var statements = new List<LoxStatementBase>();

        while (!isAtEnd() && !check(LoxTokenTypes.RIGHT_BRACE))
        {
            statements.Add(declaration());
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
        var expr = equality();

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
        return primary();
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