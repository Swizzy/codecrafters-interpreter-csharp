using System.Globalization;
using CommunityToolkit.HighPerformance.Buffers;

internal class LoxScanner
{
    private readonly string _source;
    private int _start, _startLine, _startColumn; // These are to keep track of where the token starts
    private int _current, _currentLine = 1, _currentColumn = 1; // These are to keep track of where we are right now
    private readonly Dictionary<string, LoxTokenTypes>.AlternateLookup<ReadOnlySpan<char>> _keywordLookup;
    private readonly Dictionary<string, LoxTokenTypes> _keywords = new()
    {
        { "and", LoxTokenTypes.AND },
        { "class", LoxTokenTypes.CLASS },
        { "else", LoxTokenTypes.ELSE },
        { "false", LoxTokenTypes.FALSE },
        { "for", LoxTokenTypes.FOR },
        { "fun", LoxTokenTypes.FUN },
        { "if", LoxTokenTypes.IF },
        { "nil", LoxTokenTypes.NIL },
        { "or", LoxTokenTypes.OR },
        { "print", LoxTokenTypes.PRINT },
        { "return", LoxTokenTypes.RETURN },
        { "super", LoxTokenTypes.SUPER },
        { "this", LoxTokenTypes.THIS },
        { "true", LoxTokenTypes.TRUE },
        { "var", LoxTokenTypes.VAR },
        { "while", LoxTokenTypes.WHILE },
        { "break", LoxTokenTypes.BREAK },
        { "continue", LoxTokenTypes.CONTINUE },
    };

    private bool IsAtEnd => _current >= _source.Length;
    private ReadOnlySpan<char> Lexeme => _source.AsSpan(_start, _current - _start); // The lexeme of the current token

    public event EventHandler<(int Line, int Column, string Message)>? Error;

    public LoxScanner(string source)
    {
        _keywordLookup = _keywords.GetAlternateLookup<ReadOnlySpan<char>>();
        _source = source;
    }

    public IEnumerable<LoxToken> ScanTokens()
    {
        while (IsAtEnd == false)
        {
            // Scan the next token
            var token = ScanToken();
            if (token is not null)
            {
                yield return token;
            }
        }
        StartToken();
        yield return CreateToken(LoxTokenTypes.EOF);

    }

    private void StartToken()
    {
        _start = _current;
        _startLine = _currentLine;
        _startColumn = _currentColumn;
    }

    private LoxToken? ScanToken()
    {
        StartToken();
        var c = Advance();
        switch (c)
        {
            case '(': return CreateToken(LoxTokenTypes.LEFT_PAREN);
            case ')': return CreateToken(LoxTokenTypes.RIGHT_PAREN);
            case '{': return CreateToken(LoxTokenTypes.LEFT_BRACE);
            case '}': return CreateToken(LoxTokenTypes.RIGHT_BRACE);
            case ',': return CreateToken(LoxTokenTypes.COMMA);
            case '.': return CreateToken(LoxTokenTypes.DOT);
            case '-': return CreateToken(LoxTokenTypes.MINUS);
            case '+': return CreateToken(LoxTokenTypes.PLUS);
            case ';': return CreateToken(LoxTokenTypes.SEMICOLON);
            case '*': return CreateToken(LoxTokenTypes.STAR);
            case '!': return CreateToken(Match('=') ? LoxTokenTypes.BANG_EQUAL : LoxTokenTypes.BANG);
            case '=': return CreateToken(Match('=') ? LoxTokenTypes.EQUAL_EQUAL : LoxTokenTypes.EQUAL);
            case '<': return CreateToken(Match('=') ? LoxTokenTypes.LESS_EQUAL : LoxTokenTypes.LESS);
            case '>': return CreateToken(Match('=') ? LoxTokenTypes.GREATER_EQUAL : LoxTokenTypes.GREATER);
            case '/':
                if (Match('/'))
                {
                    // A comment goes until the end of the line
                    while (Peek != '\n' && IsAtEnd == false)
                    {
                        Advance();
                    }
                }
                else if (Match('*'))
                {
                    HandleBlockComment();
                }
                else
                {
                    return CreateToken(LoxTokenTypes.SLASH);
                }
                break;
            case ' ':
            case '\r':
            case '\t':
                break; // Ignore whitespace
            case '\n':
                _currentLine++;
                _currentColumn = 1; // Reset column to 1 at the start of a new line
                break;
            case '"': return HandleString();
            default:
                if (IsDigit(c))
                {
                    return HandleNumber();
                }

                if (IsAlpha(c))
                {
                    return HandleIdentifier();
                }

                HandleError($"Unexpected character: {c}");

                break;
        }

        return null;
    }

    private void HandleBlockComment()
    {
        while (IsAtEnd == false)
        {
            if (Peek == '*' && PeekNext == '/')
            {
                Advance(); // Consume the '*'
                Advance(); // Consume the '/'
                return; // Exit the block comment
            }
            // Handle nested block comments
            if (Peek == '/' && PeekNext == '*')
            {
                Advance(); // Consume the '/'
                Advance(); // Consume the '*'
                HandleBlockComment(); // Recursively handle the nested comment. After returning from recursive call, continue scanning for the current comment's end
                continue;
            }
            if (Peek == '\n')
            {
                _currentLine++;
                _currentColumn = 1; // Reset column to 1 at the start of a new line
            }
            Advance(); // Consume the current character inside the comment
        }
        HandleError("Unterminated block comment.");
    }

    private LoxToken HandleNumber()
    {
        while (IsDigit(Peek) && IsAtEnd == false)
        {
            Advance();
        }

        if (Peek == '.' && IsDigit(PeekNext))
        {
            Advance(); // Consume the '.'
            while (IsDigit(Peek) && IsAtEnd == false)
            {
                Advance();
            }
        }
        return CreateToken(LoxTokenTypes.NUMBER, double.Parse(Lexeme, NumberFormatInfo.InvariantInfo));
    }

    private LoxToken? HandleString()
    {
        while (Peek != '"' && IsAtEnd == false)
        {
            if (Peek == '\n')
            {
                _currentLine++;
                _currentColumn = 1; // Reset column to 1 at the start of a new line
            }
            Advance();
        }

        if (IsAtEnd)
        {
            HandleError("Unterminated string.");
            return null;
        }

        Advance(); // Consume the closing '"'
        var value = StringPool.Shared.GetOrAdd(Lexeme.Slice(1, Lexeme.Length - 2));
        return CreateToken(LoxTokenTypes.STRING, value);
    }

    private LoxToken HandleIdentifier()
    {
        while (IsAlphaNumeric(Peek))
        {
            Advance();
        }

        // Check if this is a known keyword, if so - use that token type
        if (_keywordLookup.TryGetValue(Lexeme, out var tokenType))
        {
            return CreateToken(tokenType);
        }
        return CreateToken(LoxTokenTypes.IDENTIFIER);
    }

    private char Advance()
    {
        _currentColumn++;
        return _source[_current++];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd)
        {
            return false; // We're at the end of the file, there is no next character
        }

        if (_source[_current] != expected)
        {
            return false; // The next character is not the one we were looking for
        }

        Advance(); // Consume the character
        return true;
    }

    private char Peek
    {
        get
        {
            if (IsAtEnd)
            {
                return '\0'; // Return null character if we are at the end of the file
            }

            return _source[_current];
        }
    }

    private char PeekNext
    {
        get
        {
            if (_current + 1 >= _source.Length)
            {
                return '\0'; // Return null character if we are at the end of the file
            }

            return _source[_current + 1];
        }
    }

    private bool IsDigit(char c) => c is >= '0' and <= '9';

    private bool IsAlpha(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';

    private bool IsAlphaNumeric(char c) => IsDigit(c) || IsAlpha(c);

    private void HandleError(string message) => Error?.Invoke(this, (_currentLine, _currentColumn, message));

    private LoxToken CreateToken(LoxTokenTypes tokenType, object? literal = null)
    {
        var text = StringPool.Shared.GetOrAdd(Lexeme);
        return new LoxToken(tokenType, text, literal, _startLine, _startColumn);
    }
}