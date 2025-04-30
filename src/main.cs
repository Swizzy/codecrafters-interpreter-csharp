if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ./your_program.sh tokenize <filename>");
    Environment.Exit(1);
}

string command = args[0];
string filename = args[1];

if (command != "tokenize")
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Environment.Exit(1);
}

string fileContents = File.ReadAllText(filename);

bool hadSyntaxError = false;

var scanner = new LoxScanner(fileContents);
scanner.Error += (_, args) =>
{
    var (line, column, message) = args;
    hadSyntaxError = true;
    Console.Error.WriteLine($"[line {line}] Error: {message}");

};

foreach (var loxToken in scanner.ScanTokens())
{
    Console.WriteLine(loxToken);
}

if (hadSyntaxError)
{
    return 65;
}
return 0;