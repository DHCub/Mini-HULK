namespace HULK;

class Program
{
    static void Main()
    {
        var analyzer = new Semantic_Analizer();

        while(true)
        {
            System.Console.Write('>');
            string prog = Console.ReadLine();
            var lexer = new Lexer(prog);
            var parser = new Parser(lexer);

            var tree = parser.Get_Command_AST();

            if (tree.Statement is Function_Declaration_Node)
            {
                try
                {
                    analyzer.Define_Function((Function_Declaration_Node)tree.Statement);
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }
            }
            else
            {
                try
                {
                    analyzer.typecheck(tree.Statement);
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }
            }
        }
    }
}