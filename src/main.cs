#if !DEBUG
var supportedCommands = new Dictionary<string, string>
{
    {"tokenize", "Prints how the interpreter reads the script, one token per line."},
    {"parse", "Parses the script as an expression and prints the parse tree."},
    {"parse2", "Parses the script as statements and prints the parse tree."},
    {"evaluate", "Evaluates the script and prints the result"},
    {"run", "Runs the script."},
};

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ./your_program.sh <command> <filename>");
    foreach (var supportedCommand in supportedCommands)
    {
        Console.Error.WriteLine($" - {supportedCommand.Key} : {supportedCommand.Value}");
    }
    Environment.Exit(1);
}

string command = args[0];
string filename = args[1];

if (supportedCommands.ContainsKey(command) == false)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Environment.Exit(1);
}

string fileContents = File.ReadAllText(filename);
#else
string command = "run";
string fileContents = "for (var world = 0; world < 3; world = world + 1) { print world; }";
#endif

bool hadSyntaxError = false;
bool hadRuntimeError = false;

var scanner = new LoxScanner(fileContents);
scanner.Error += (_, args) =>
{
    var (line, _, message) = args;
    hadSyntaxError = true;
    Console.Error.WriteLine($"[line {line}] Error: {message}");
};

var tokens = scanner.ScanTokens().ToList();

var parser = new LoxParser(tokens);
parser.Error += (_, args) =>
{
    var (token, message) = args;
    hadSyntaxError = true;
    if (token.TokenType == LoxTokenTypes.EOF)
    {
        Console.Error.WriteLine($"[line {token.Line}] Error: {message}");
    }
    else
    {
        Console.Error.WriteLine($"[line {token.Line}] Error at '{token.Lexeme}': {message}");
    }
};

var interpreter = new LoxInterpreter();
interpreter.Error += (_, args) =>
{
    var (line, column, message) = args;
    hadRuntimeError = true;
    Console.Error.WriteLine($"[line {line} column {column}] Error: {message}");
};

if (command == "tokenize")
{
    foreach (var loxToken in tokens)
    {
        Console.WriteLine(loxToken);
    }
}
else if (command == "parse")
{
    var astPrinter = new AstPrinter();
    var expression = parser.ParseExpression();
    if (hadSyntaxError == false)
    {
        Console.WriteLine(astPrinter.Print(expression!));
    }
}
else if (command == "parse2")
{
    var astPrinter = new AstPrinter();
    var statements = parser.Parse();
    if (hadSyntaxError == false)
    {
        foreach (var statement in statements)
        {
            Console.WriteLine(astPrinter.Print(statement));

        }
    }
}
else if (command == "evaluate")
{
    var expression = parser.ParseExpression();
    if (hadSyntaxError == false)
    {
        interpreter.Interpret(expression!);
    }
}
else if (command == "run")
{
    var statements = parser.Parse();
    if (hadSyntaxError == false)
    {
        var resolver = new LoxResolver(interpreter);
        resolver.Error += (_, args) =>
        {
            var (line, column, message) = args;
            hadSyntaxError = true;
            Console.Error.WriteLine($"[line {line} column {column}] Error: {message}");
        };

        resolver.Resolve(statements);

        if (hadSyntaxError == false) // We already know we're not going to be able to run this, so just ignore it.
        {
            interpreter.Interpret(statements);
        }
    }
}

if (hadSyntaxError)
{
    return 65;
}
if (hadRuntimeError)
{
    return 70;
}
return 0;