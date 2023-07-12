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
    public AST_Treenode Body;
    public Function_Declaration_Node(Variable_Node Name_variable, List<Variable_Node> Parameters, AST_Treenode Body)
    {
        this.Parameters = Parameters;
        this.Name_variable = Name_variable;
        this.Body = Body;
    }
}

class Function_Call_Node : AST_Treenode
{
    public Variable_Node Name_node;
    public Function_Symbol Symbol;
    public List<AST_Treenode> Arguments;
    public Function_Call_Node(Variable_Node Name_node, List<AST_Treenode> Arguments)
    {
        this.Name_node = Name_node;
        this.Arguments = Arguments;
        Symbol = null;
    }
}

class STRING_Node : AST_Treenode
{
    public string Value;
    public STRING_Node(string Value)
    {
        this.Value = Value;
    }
}

class NUMBER_Node : AST_Treenode
{
    public double Value;
    public NUMBER_Node(double Value)
    {
        this.Value = Value;
    }
}

class BOOLEAN_Node : AST_Treenode
{
    public bool Value;
    public BOOLEAN_Node(bool Value)
    {
        this.Value = Value;
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
