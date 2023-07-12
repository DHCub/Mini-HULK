namespace HULK;

class Program
{
    static void Main()
    {
        var global_symbol_table = new SymbolTable();
        while(true)
        {
            var line = Console.ReadLine();
            var lexer = new Lexer(line);
            var parser = new Parser(lexer);

            var ast = parser.Get_Command_AST();
        }
    }
}