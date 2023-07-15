namespace HULK;

class Program
{
    static void Main()
    {
        var analyzer = new Semantic_Analizer();

        while(true)
        {
            System.Console.Write('>');
            string prog;
            Lexer lexer;
            Parser parser;
            Command_Node tree = null;
            
            try
            {
                prog = Console.ReadLine();
                lexer = new Lexer(prog);
                parser = new Parser(lexer);
                tree = parser.Get_Command_AST();

                if (tree.Statement is Function_Declaration_Node)
                {
                    
                    analyzer.Define_Function((Function_Declaration_Node)tree.Statement);
                }
                else
                {
                    analyzer.typecheck(tree.Statement);
                    var output = Interpreter.Evaluate(tree.Statement);
                    if (output is double) System.Console.WriteLine((double)output);
                    if (output is bool) System.Console.WriteLine((bool)output);
                    if (output is string) System.Console.WriteLine((string)output);
                    //else (output is null)
                }
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
                analyzer.RevertToGlobal();
                continue;
            }
        }
    }
}