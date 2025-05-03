using System.Globalization;

internal class LoxScanner
{
    private readonly string _source;
    private readonly List<LoxToken> _tokens = [];
    private int _start, _startLine, _startColumn; // These are to keep track of where the token starts
    private int _current, _currentLine = 1, _currentColumn = 1; // These are to keep track of where we are right now
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
    private string Text => _source[_start.._current]; // The text of the current token

    public event EventHandler<(int Line, int Column, string Message)>? Error;

    public LoxScanner(string source) => _source = source;

    public List<LoxToken> ScanTokens()
    {
        while (IsAtEnd == false)
        {
            // Scan the next token
            ScanToken();
        }
        StartToken();
        // Add EOF token at the end
        AddToken(LoxTokenTypes.EOF);
        return _tokens;
    }

    private void StartToken()
    {
        _start = _current;
        _startLine = _currentLine;
        _startColumn = _currentColumn;
    }

    private void ScanToken()
    {
        StartToken();
        var c = Advance();
        switch (c)
        {
            case '(': AddToken(LoxTokenTypes.LEFT_PAREN); break;
            case ')': AddToken(LoxTokenTypes.RIGHT_PAREN); break;
            case '{': AddToken(LoxTokenTypes.LEFT_BRACE); break;
            case '}': AddToken(LoxTokenTypes.RIGHT_BRACE); break;
            case ',': AddToken(LoxTokenTypes.COMMA); break;
            case '.': AddToken(LoxTokenTypes.DOT); break;
            case '-': AddToken(LoxTokenTypes.MINUS); break;
            case '+': AddToken(LoxTokenTypes.PLUS); break;
            case ';': AddToken(LoxTokenTypes.SEMICOLON); break;
            case '*': AddToken(LoxTokenTypes.STAR); break;
            case '!': AddToken(Match('=') ? LoxTokenTypes.BANG_EQUAL : LoxTokenTypes.BANG); break;
            case '=': AddToken(Match('=') ? LoxTokenTypes.EQUAL_EQUAL : LoxTokenTypes.EQUAL); break;
            case '<': AddToken(Match('=') ? LoxTokenTypes.LESS_EQUAL : LoxTokenTypes.LESS); break;
            case '>': AddToken(Match('=') ? LoxTokenTypes.GREATER_EQUAL : LoxTokenTypes.GREATER); break;
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
                    Advance(); // Consume the '*'
                    HandleBlockComment();
                }
                else
                {
                    AddToken(LoxTokenTypes.SLASH);
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
            case '"': HandleString(); break;
            default:
                if (IsDigit(c))
                {
                    HandleNumber();
                }
                else if (IsAlpha(c))
                {
                    HandleIdentifier();
                }
                else
                {
                    HandleError($"Unexpected character: {c}");
                }
                break;
        }
    }

    private void HandleBlockComment()
    {
        while (IsAtEnd == false)
        {
            if (Peek == '*' && PeekNext == '/')
            {
                Advance(); // Consume the '*'
                Advance(); // Consume the '/'
                return;
            }
            if (Peek == '/' && PeekNext == '*')
            {
                Advance(); // Consume the '/'
                Advance(); // Consume the '*'
                HandleBlockComment();
            }
            if (Peek == '\n')
            {
                _currentLine++;
                _currentColumn = 1; // Reset column to 1 at the start of a new line
            }
            Advance();
        }
        HandleError("Unterminated block comment.");
    }

    private void HandleNumber()
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
        AddToken(LoxTokenTypes.NUMBER, double.Parse(Text, NumberFormatInfo.InvariantInfo));
    }

    private void HandleString()
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
            return;
        }

        Advance();
        var value = _source[(_start + 1)..(_current - 1)];
        AddToken(LoxTokenTypes.STRING, value);
    }

    private void HandleIdentifier()
    {
        while (IsAlphaNumeric(Peek))
        {
            Advance();
        }

        if (_keywords.TryGetValue(Text, out var tokenType))
        {
            AddToken(tokenType);
            return;
        }
        AddToken(LoxTokenTypes.IDENTIFIER);
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

    private void AddToken(LoxTokenTypes tokenType, object? literal = null)
    {
        string text = Text;
        _tokens.Add(new LoxToken(tokenType, text, literal, _startLine, _startColumn));
    }
}