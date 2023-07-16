namespace HULK;

class Semantic_Analizer
{
    Context Current_Context;
    Context Global_Context;
    
    public Semantic_Analizer()
    {
        this.Global_Context = new Context();
        this.Current_Context = Global_Context;
    }

    public void RevertToGlobal() {Current_Context = Global_Context;}

    #region Function definition handling and type inference
    public void Define_Function(Function_Declaration_Node declaration)
    {
        SimpleType.Reset_Names();
        
        var original_global_context = Current_Context;
        Current_Context = new Context(original_global_context);
        
        var function_Symbol = getFunction_Symbol(declaration);
        Current_Context.Define(function_Symbol);
        
        var return_type = function_Symbol.Return_Type;
        var function_name = function_Symbol.Name;
        
        AddParametersToCurrentContext(function_Symbol);

        if (ContainsTypeVariables(function_Symbol)) 
        {
            function_Symbol = getType_Inferred_Function_Symbol(declaration, function_Symbol);
            
            Current_Context = new Context(original_global_context);
            Current_Context.Define(function_Symbol);
            AddParametersToCurrentContext(function_Symbol);
            return_type = function_Symbol.Return_Type;
        }

        

        var actual_return = typecheck(declaration.Body);

        if (return_type != actual_return)
            throw new Exception($"Expected {return_type} as return type, {actual_return} obtained instead");


        function_Symbol.Body = declaration.Body;
        
        Current_Context = Global_Context;
        Global_Context.Define(function_Symbol);

    }

    Function_Symbol getFunction_Symbol(Function_Declaration_Node declaration)
    {
        var provisional_Context = new Context();

        var name = declaration.Name_variable.VarToken.Value;
        var return_type = (declaration.Return_Type is null) ? SimpleType.New_Type_Variable() : declaration.Return_Type;

        var Parameters = new List<Variable_Symbol>();
        for(int i = 0; i < declaration.Parameters.Count; i++)
        {
            var param_type = (declaration.Type_Specifiers[i] is null) ? SimpleType.New_Type_Variable() : declaration.Type_Specifiers[i];
            var param_name = declaration.Parameters[i].VarToken.Value;

            var param_Symbol = new Variable_Symbol(param_name, param_type!);
            provisional_Context.Define(param_Symbol, declaration.Parameters[i].VarToken.position);

            Parameters.Add(param_Symbol);
        }

        return new Function_Symbol(name, return_type, Parameters);
    }

    Function_Symbol getType_Inferred_Function_Symbol(Function_Declaration_Node declaration, Function_Symbol unresolved_symbol)
    {
        var return_type = unresolved_symbol.Return_Type;
        var function_name = unresolved_symbol.Name;
        var Parameters = unresolved_symbol.Parameters;

        var constraints = new Equation_System();
        InferTypes(declaration.Body, return_type, constraints);

        System.Console.WriteLine("Type Inference:");
        return_type = (!return_type.isLiteral()) ? Equation_System.ResolveType($"Return type of {function_name}", return_type, constraints) : return_type;
        System.Console.WriteLine("Return Type: " + return_type);

        var Resolved_Parameters = new List<Variable_Symbol>();
        for (int i = 0; i < declaration.Parameters.Count; i++)
        {
            var paramteter_declaration = declaration.Parameters[i];
            var param_name = paramteter_declaration.VarToken.Value;
            var param_type = (declaration.Type_Specifiers[i] is null) ? Equation_System.ResolveType($"Type of Parameter #{i + 1}, {param_name}", Parameters[i].Return_Type, constraints) : declaration.Type_Specifiers[i];

            var param_Symbol = new Variable_Symbol(param_name, param_type!);
            System.Console.WriteLine($"Parameter {param_name} type: " + param_type);

            Resolved_Parameters.Add(param_Symbol);
        }

        return new Function_Symbol(function_name, return_type, Resolved_Parameters);
    }

    bool ContainsTypeVariables(Function_Symbol symbol)
    {
        if (!symbol.Return_Type.isLiteral()) return true;
        for (int i = 0; i < symbol.Parameters.Count; i++)
        {
            if (!symbol.Parameters[i].Return_Type.isLiteral()) return true;
        }

        return false;
    }

    void AddParametersToCurrentContext(Function_Symbol symbol)
    {
        foreach(var parameter in symbol.Parameters)
        {
            var param_name = parameter.Name;
            var param_type = parameter.Return_Type;

            Current_Context.Define(new Variable_Symbol(param_name, param_type));
        }
    }

    void InferTypes(AST_Treenode node, SimpleType Expected, Equation_System Constraints)
    {
        if (node is NUMBER_Node) 
        {
            if (Expected.isLiteral() && Expected != SimpleType.NUMBER())
            {
                int pos = ((NUMBER_Node)node).position;
                throw new Exception(SEMANTIC_ERROR + Type_Error(pos, Token.NUMBER));
            }
            
            Constraints.Add(Expected, SimpleType.NUMBER());
        }
        else if (node is STRING_Node) 
        {
            if (Expected.isLiteral() && Expected != SimpleType.STRING())
            {
                int pos = ((NUMBER_Node)node).position;
                throw new Exception(SEMANTIC_ERROR + Type_Error(pos, Token.STRING));
            }

            Constraints.Add(Expected, SimpleType.STRING());
        }
        else if (node is BOOLEAN_Node) 
        {
            if (Expected.isLiteral() && Expected != SimpleType.BOOLEAN())
            {
                int pos = ((BOOLEAN_Node)node).position;
                throw new Exception(SEMANTIC_ERROR + Type_Error(pos, Token.STRING));
            }

            Constraints.Add(Expected, SimpleType.BOOLEAN());
        }
        else if (node is Variable_Node)
        {
            var variable = (Variable_Node)node;
            var name = variable.VarToken.Value;
            var symbol = Current_Context.Lookup(name);

            if (symbol == null)
                throw new Exception(SEMANTIC_ERROR + Undefined_Variable_Use(name, variable.VarToken.position));
            
            if (symbol is not Variable_Symbol)
                throw new Exception(SEMANTIC_ERROR + Function_Used_as_Variable(name, variable.VarToken.position));

            var variable_symbol = (Variable_Symbol)symbol;

            Constraints.Add(Expected, variable_symbol.Return_Type);
        }
        else if (node is Function_Call_Node)
        {
            var call = (Function_Call_Node)node;
            var name = call.Name_node.VarToken.Value;
            var symbol = Current_Context.Lookup(name);

            if (symbol == null)
                throw new Exception(SEMANTIC_ERROR + Undefined_Function_Use(name, call.Name_node.VarToken.position));

            if (symbol is not Function_Symbol)
                throw new Exception(SEMANTIC_ERROR + Variable_Used_as_Function(name, call.Name_node.VarToken.position));

            var func_symbol = (Function_Symbol)symbol;

            if (func_symbol.Parameters.Count != call.Arguments.Count)
                throw new Exception(SEMANTIC_ERROR + Incorrect_Number_of_Arguments(name, call.Name_node.VarToken.position, func_symbol.Parameters.Count, call.Arguments.Count));

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                InferTypes(call.Arguments[i], func_symbol.Parameters[i].Return_Type, Constraints);
            }

            Constraints.Add(Expected, func_symbol.Return_Type);
        }
        else if (node is UnOp_Node)
        {
            var op = (UnOp_Node)node;
            if (Expected.isLiteral() && Expected != SimpleType.NUMBER())
                throw new Exception(SEMANTIC_ERROR + Type_Error(op.Operator.position, Token.NUMBER));

            Constraints.Add(Expected, SimpleType.NUMBER());
            InferTypes(op.right, SimpleType.NUMBER(), Constraints);
        }
        else if (node is BinOp_Node)
        {
            var op = (BinOp_Node)node;

            switch(op.Operator.Value)
            {
            case Token.PLUS:
            case Token.MINUS:
            case Token.TIMES:
            case Token.DIVISION:
            case Token.MODULO:
            case Token.POWER:
                if (Expected.isLiteral() && Expected != SimpleType.NUMBER())
                    throw new Exception(SEMANTIC_ERROR + Type_Error(op.Operator.position, Token.NUMBER));
                
                Constraints.Add(Expected, SimpleType.NUMBER());
                InferTypes(op.left, SimpleType.NUMBER(), Constraints);
                InferTypes(op.right, SimpleType.NUMBER(), Constraints);
                
                return;
            case Token.EQUAL_EQUAL:
                if (Expected.isLiteral() && Expected != SimpleType.BOOLEAN())
                    throw new Exception(SEMANTIC_ERROR + Type_Error(op.Operator.position, Token.BOOLEAN));
                
                Constraints.Add(Expected, SimpleType.BOOLEAN());
                InferTypes(op.left, SimpleType.ANY(), Constraints);
                InferTypes(op.right, SimpleType.ANY(), Constraints);

                return;
            case Token.SMALLER:
            case Token.GREATER:
            case Token.GREATER_EQUAL:
            case Token.SMALLER_EQUAL:
                if (Expected.isLiteral() && Expected != SimpleType.BOOLEAN())
                    throw new Exception(SEMANTIC_ERROR + Type_Error(op.Operator.position, Token.BOOLEAN));

                Constraints.Add(Expected, SimpleType.BOOLEAN());
                InferTypes(op.left, SimpleType.NUMBER(), Constraints);
                InferTypes(op.right, SimpleType.NUMBER(), Constraints);

                return;

            case Token.AT_OPERATOR:
                if (Expected.isLiteral() && Expected != SimpleType.STRING())
                    throw new Exception(SEMANTIC_ERROR + Type_Error(op.Operator.position, Token.STRING));
                
                Constraints.Add(Expected, SimpleType.STRING());
                InferTypes(op.left, SimpleType.ANY(), Constraints);
                InferTypes(op.right, SimpleType.ANY(), Constraints);

                return;
            
            case KeyWords.OR:
            case KeyWords.AND:
                if (Expected.isLiteral() && Expected != SimpleType.BOOLEAN())
                    throw new Exception(SEMANTIC_ERROR + Type_Error(op.Operator.position, Token.BOOLEAN));
                
                Constraints.Add(Expected, SimpleType.BOOLEAN());
                InferTypes(op.left, SimpleType.BOOLEAN(), Constraints);
                InferTypes(op.right, SimpleType.BOOLEAN(), Constraints);

                return;
            }

            throw new Exception("UNSUPPORTED OPERATOR");
        }
        else if (node is If_Else_Node)
        {
            var if_else = (If_Else_Node)node;

            for (int i = 0; i < if_else.Conditions.Count; i++)
            {
                InferTypes(if_else.Conditions[i], SimpleType.BOOLEAN(), Constraints);
                InferTypes(if_else.Clauses[i], Expected, Constraints);
            }

            InferTypes(if_else.Else_Clause, Expected, Constraints);
        }    
        else if (node is Let_In_Node)
        {
            var temp = Current_Context;
            Current_Context = new Context(Current_Context);


            var let_in = (Let_In_Node)node;

            foreach(var declaration in let_in.Declarations)
            {
                var symbol = new Variable_Symbol(declaration.Variable_Node.VarToken.Value, SimpleType.New_Type_Variable());
                Current_Context.Define(symbol);

                InferTypes(declaration.Value, symbol.Return_Type, Constraints);
            }

            InferTypes(let_in.Statement, Expected, Constraints);

            Current_Context = temp;
        }
        else throw new Exception("TYPE INFERENCE UNEXPECTED NODE");
    }

    #endregion

    #region Static Type Checking
    public SimpleType typecheck(AST_Treenode node)
    {
        if (node is NUMBER_Node) return SimpleType.NUMBER();
        else if (node is STRING_Node) return SimpleType.STRING();
        else if (node is BOOLEAN_Node) return SimpleType.BOOLEAN();
        else if (node is Variable_Node)
        {
            var variable = (Variable_Node)node;
            var name = variable.VarToken.Value;
            var symbol = Current_Context.Lookup(name);

            if (symbol == null)
                throw new Exception(SEMANTIC_ERROR + Undefined_Variable_Use(name, variable.VarToken.position));
            
            if (symbol is not Variable_Symbol)
                throw new Exception(SEMANTIC_ERROR + Function_Used_as_Variable(name, variable.VarToken.position));

            var variable_symbol = (Variable_Symbol)symbol;

            return variable_symbol.Return_Type;
        }
        else if (node is Function_Call_Node)
        {
            var call = (Function_Call_Node)node;
            var name = call.Name_node.VarToken.Value;
            var symbol = Current_Context.Lookup(name);

            if (symbol == null)
                throw new Exception(SEMANTIC_ERROR + Undefined_Function_Use(name, call.Name_node.VarToken.position));

            if (symbol is not Function_Symbol)
                throw new Exception(SEMANTIC_ERROR + Variable_Used_as_Function(name, call.Name_node.VarToken.position));

            var func_symbol = (Function_Symbol)symbol;

            if (func_symbol.Parameters.Count != call.Arguments.Count)
                throw new Exception(SEMANTIC_ERROR + Incorrect_Number_of_Arguments(name, call.Name_node.VarToken.position, func_symbol.Parameters.Count, call.Arguments.Count));

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                var actualType = typecheck(call.Arguments[i]);
                if (actualType != func_symbol.Parameters[i].Return_Type && func_symbol.Parameters[i].Return_Type != SimpleType.ANY())
                {
                    var func_name = call.Name_node.VarToken.Value;
                    int pos = call.Name_node.VarToken.position;
                    var expected = func_symbol.Parameters[i].Return_Type;
            
                    throw new Exception(SEMANTIC_ERROR + Incorrect_Type_of_Argument(func_name, pos, expected, actualType, i));
                }
            }

            call.Symbol = func_symbol;

            return func_symbol.Return_Type;
        }
        else if (node is UnOp_Node)
        {
            var op = (UnOp_Node)node;
            var operand_type = typecheck(op.right);
            if (operand_type != SimpleType.NUMBER())
                throw new Exception(SEMANTIC_ERROR + Incorrect_Operand_Type_UnOp(operand_type, op.Operator));
            
            return SimpleType.NUMBER();
        }
        else if (node is BinOp_Node)
        {
            var op = (BinOp_Node)node;

            var left_T = typecheck(op.left);
            var right_T = typecheck(op.right);
                       
            switch(op.Operator.Value)
            {
            case Token.PLUS:
            case Token.MINUS:
            case Token.TIMES:
            case Token.DIVISION:
            case Token.MODULO:
            case Token.POWER:
                if (left_T != SimpleType.NUMBER() || right_T != left_T)
                    throw new Exception(SEMANTIC_ERROR + Incorrect_Operand_Types_BinOp(left_T, op.Operator, right_T));
                
                return SimpleType.NUMBER();
            
            case Token.EQUAL_EQUAL:
                // any types work here

                return SimpleType.BOOLEAN();
            
            case Token.SMALLER:
            case Token.GREATER:
            case Token.GREATER_EQUAL:
            case Token.SMALLER_EQUAL:
                if (left_T != SimpleType.NUMBER() || right_T != left_T)
                    throw new Exception(SEMANTIC_ERROR + Incorrect_Operand_Types_BinOp(left_T, op.Operator, right_T));

                return SimpleType.BOOLEAN();

            case Token.AT_OPERATOR:
                // anything works here too

                return SimpleType.STRING();
            
            case KeyWords.OR:
            case KeyWords.AND:
                if (left_T != SimpleType.BOOLEAN() || right_T != left_T)
                    throw new Exception(SEMANTIC_ERROR + Incorrect_Operand_Types_BinOp(left_T, op.Operator, right_T));

                return SimpleType.BOOLEAN();
            
            default:
                throw new Exception("UNSUPPORTED OPERATOR");   
            }
        }
        else if (node is If_Else_Node)
        {
            var if_else = (If_Else_Node)node;

            var condition_T = typecheck(if_else.Conditions[0]); // the node must have at least one condition and clause
            if (condition_T != SimpleType.BOOLEAN())
                throw new Exception(SEMANTIC_ERROR + $"{Token.BOOLEAN}, expected in first condition of if-else at {if_else.BeginPos}, {condition_T} passed instead");
            
            var Clause_T = typecheck(if_else.Clauses[0]);

            for (int i = 1; i < if_else.Clauses.Count; i++)
            {
                var newCondition_T = typecheck(if_else.Conditions[i]);
                if (newCondition_T != SimpleType.BOOLEAN())
                    throw new Exception(SEMANTIC_ERROR + $"{Token.BOOLEAN} expected in condition #{i + 1} of if-else statement at {if_else.BeginPos}, {newCondition_T} passed instead");

                var newClause_T = typecheck(if_else.Clauses[i]);
                if (newClause_T != Clause_T)
                    throw new Exception(SEMANTIC_ERROR + $"Clause #{i + 1} of if-else statement at {if_else.BeginPos} has return type ({newClause_T}), different from first clause return type ({Clause_T})");
            
            }
            
            var Else_Clause = typecheck(if_else.Else_Clause);

            if (Clause_T != Else_Clause)
                throw new Exception(SEMANTIC_ERROR + $"Else clause of if-else statement at {if_else.BeginPos} has different return type ({Else_Clause}) than rest of clauses ({Clause_T})");

            return Clause_T;
        }
        else if (node is Let_In_Node)
        {
            var original_context = Current_Context;
            
            Current_Context = new Context(Current_Context);

            var let_in = (Let_In_Node)node;
            foreach(var declaration in let_in.Declarations)
            {
                var Name = declaration.Variable_Node.VarToken.Value;
                var type = typecheck(declaration.Value);

                var symbol = new Variable_Symbol(Name, type);
                Current_Context.Define(symbol);
            }

            var answ = typecheck(let_in.Statement);

            Current_Context = original_context;

            return answ;
        }
    

        else throw new Exception("TYPE Checking UNEXPECTED NODE" + node.GetType());        
    }

    #endregion

    #region Errors
    public const string SEMANTIC_ERROR = "! SEMATIC ERROR: ";
    
    string Type_Error(int pos, string Expected)
        => $"Expected type {Expected} at {pos}";

    string Undefined_Variable_Use(string name, int pos)
        => $"Variable {name} at {pos} is undefined in this scope";

    string Variable_Used_as_Function(string name, int pos)
        => $"Variable {name} is used as a function at {pos}";

    string Undefined_Function_Use(string name, int pos)
        => $"Function {name} at {pos} is undefined in this scope";

    string Function_Used_as_Variable(string name, int pos)
        => $"Function {name} used as variable at {pos}";

    string Incorrect_Number_of_Arguments(string name, int pos, int Expected, int Actual)
        => $"Function {name} takes {Expected} Parameter(s), {Actual} passed instead at {pos}";

    string Incorrect_Type_of_Argument(string function_name, int pos, SimpleType Expected, SimpleType Actual, int zero_index_param_Number)
        => $"Function {function_name} takes {Expected} as #{zero_index_param_Number + 1} parameter, {Actual} passed istead at {pos}";

    string Incorrect_Operand_Types_BinOp(SimpleType left, Token op, SimpleType right)
        => $"Incorrect Operand types {left} and {right} for binary operator {op.Value} at {op.position}";

    string Incorrect_Operand_Type_UnOp(SimpleType operand, Token op)
        => $"Incorrect Operand type {operand} for unary operator {op.Value} at {op.position}"; 

    #endregion

}


#region Type Equations and Type Equation system
class Equation_System
{
    List<Equation> equations;
    public Equation_System()
    {
        this.equations = new List<Equation>();
    }

    public Equation_System(Equation_System other)
    {
        this.equations = new List<Equation>(other.equations);
    }

    public Equation? Extract_Equation(SimpleType member)
    {
        for (int i = equations.Count - 1; i >= 0; i--)
        {
            if (equations[i].ContainsMember(member))
            {
                var eq = equations[i];
                if (member == eq.Right) eq.Invert();
                equations.RemoveAt(i);
                
                return eq;
            }
        }

        return null;
    }

    public void Substitute(Equation eq, bool first = true)
    {
        if (!first) eq.Invert();

        foreach(var equation in equations)
        {
            equation.SubstituteFirst(eq);
        }
    }

    public void Add(Equation eq)
    {
        if (eq.isTrivial()) return;
        foreach(var equation in equations)
        {
            if (eq == equation) return;
        }

        equations.Add(eq);
    }

    public void Add(SimpleType Left, SimpleType Right)
    {
        var eq = new Equation(Left, Right);
        Add(eq);
    }

    public static SimpleType ResolveType(string Name_Of_Type, SimpleType type_variable, Equation_System constraints)
    {
        var temp_System = new Equation_System(constraints);
        SimpleType answ = null!; // if we do not find a value for answ, an exception will be thrown
        while(true)
        {
            var equation = temp_System.Extract_Equation(type_variable);
            if (equation is null)
            {
                if (answ is null)
                    throw new Exception(Semantic_Analizer.SEMANTIC_ERROR + $"{Name_Of_Type} could not be resolved");
                else break;
            }
            else if (equation.Right.isLiteral())
            {
                if (answ is not null)
                {
                    if (answ != equation.Right)
                        throw new Exception(Semantic_Analizer.SEMANTIC_ERROR + $"{Name_Of_Type} used as {answ} and as {equation.Right}");
                }
                else answ = equation.Right;
            }
            else
            {
                constraints.Substitute(equation);
            }
        }

        return answ;
    }

}

class Equation
{
    public SimpleType Right {get; private set;}
    public SimpleType Left {get; private set;}

    public Equation(SimpleType left, SimpleType right)
    {
        this.Left = left;
        this.Right = right;
    }

    public static bool operator==(Equation a, Equation b)
    {
        if (a.Left == b.Left)
        {
            if (a.Right == b.Right) return true;
        }
        else if (a.Left == b.Right)
        {
            if (a.Right == b.Left) return true;
        }

        return false;
    }

    public bool isTrivial() => this.Left == this.Right;

    public static bool operator!= (Equation a, Equation b) => !(a == b);

    public void SubstituteFirst(Equation eq)
    {
        if (this != eq)
        {
            if (eq.Right == this.Left)
            {
                this.Left = eq.Left;
            }
            else if (eq.Right == this.Right)
            {
                this.Right = eq.Left;
            }
        }
    }

    public void Invert()
    {
        var temp = this.Left;
        this.Left = this.Right;
        this.Right = temp;
    }

    public bool ContainsMember(SimpleType member)
    {
        return member == Right || member == Left;
    }

}

#endregion