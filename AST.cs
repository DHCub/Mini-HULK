namespace HULK;

class AST_Treenode {}

class Command_Node : AST_Treenode
{
    public AST_Treenode Statement;
    public Command_Node(AST_Treenode statement)
    {
        this.Statement = statement;
    }
}

class Let_In_Node : AST_Treenode
{
    public int BeginPos;
    public List<Variable_Declaration_Node> Declarations;
    public AST_Treenode Statement;
    public Let_In_Node(int BeginPos, List<Variable_Declaration_Node> declaration_Nodes, AST_Treenode Statement)
    {
        this.BeginPos = BeginPos;
        this.Declarations = declaration_Nodes;
        this.Statement = Statement;
    }
}

class Variable_Declaration_Node : AST_Treenode
{
    public Variable_Node Variable_Node;
    public AST_Treenode Value;
    public Variable_Declaration_Node(Variable_Node variable_Node, AST_Treenode Value)
    {
        this.Variable_Node = variable_Node;
        this.Value = Value;
    }
}

class If_Else_Node : AST_Treenode
{
    public int BeginPos;
    public AST_Treenode Condition;
    public AST_Treenode True_Clause;
    public AST_Treenode False_Clause;
    public If_Else_Node(int BeginPos, AST_Treenode Condition, AST_Treenode True_Clause, AST_Treenode False_Clause)
    {
        this.BeginPos = BeginPos;
        this.Condition = Condition;
        this.True_Clause = True_Clause;
        this.False_Clause = False_Clause;
    }
}

class BinOp_Node : AST_Treenode
{
    public Token Operator;
    public AST_Treenode left;
    public AST_Treenode right;
    public BinOp_Node(AST_Treenode left, Token OPERATOR, AST_Treenode right)
    {
        this.left = left;
        this.right = right;
        this.Operator = OPERATOR;
    }
}

class UnOp_Node : AST_Treenode
{
    public Token Operator;
    public AST_Treenode right;
    public UnOp_Node(Token Operator, AST_Treenode right)
    {
        this.Operator = Operator;
        this.right = right;
    }
}

class Function_Declaration_Node : AST_Treenode
{
    public Variable_Node Name_variable;
    public List<Variable_Node> Parameters;
    public List<SimpleType> Type_Specifiers;
    public SimpleType Return_Type;
    public AST_Treenode Body;
    public Function_Declaration_Node(Variable_Node Name_variable, List<Variable_Node> Parameters, List<SimpleType> Type_Specifiers, SimpleType Return_Type, AST_Treenode Body)
    {
        this.Parameters = Parameters;
        this.Type_Specifiers = Type_Specifiers;
        this.Name_variable = Name_variable;
        this.Return_Type = Return_Type;
        this.Body = Body;
    }
}

class Function_Call_Node : AST_Treenode
{
    public Variable_Node Name_node;
    public List<AST_Treenode> Arguments;
    public Function_Symbol Symbol; // filled by Semantic Analyzer
    public Function_Call_Node(Variable_Node Name_node, List<AST_Treenode> Arguments)
    {
        this.Name_node = Name_node;
        this.Arguments = Arguments;
    }
}

class STRING_Node : AST_Treenode
{
    public string Value;
    public int position;
    public STRING_Node(string Value, int position)
    {
        this.position = position;
        this.Value = Value;
    }
}

class NUMBER_Node : AST_Treenode
{
    public double Value;
    public int position;
    public NUMBER_Node(double Value, int position)
    {
        this.position = position;
        this.Value = Value;
    }
}

class BOOLEAN_Node : AST_Treenode
{
    public bool Value;
    public int position;
    public BOOLEAN_Node(bool Value, int position)
    {
        this.Value = Value;
        this.position = position;
    }
}

class Variable_Node : AST_Treenode
{
    public Token VarToken;
    public Variable_Node(Token VarToken)
    {
        this.VarToken = VarToken;
    }
}
