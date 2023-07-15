namespace HULK;

class Semantic_Analizer
{
    Context Current_Context;
    
    public Semantic_Analizer()
    {
        this.Current_Context = new Context();
    }

    #region Function definition handling and type inference
    public void Define_Function(Function_Declaration_Node declaration)
    {
        SimpleType.Reset_Names();
        var constraints = new Equation_System();
        
        var original_global_context = Current_Context;

        Current_Context = new Context(Current_Context);
        var func_name = declaration.Name_variable.VarToken.Value;
        var func_Return_var = SimpleType.New_Type_Variable();

        var Parameters = new List<Variable_Symbol>();
        
        foreach(var parameter_declaration in declaration.Parameters)
        {
            var name = parameter_declaration.VarToken.Value;
            var type_var = SimpleType.New_Type_Variable();
            var param_Symbol = new Variable_Symbol(name, type_var);

            Current_Context.Define(param_Symbol, parameter_declaration.VarToken.position);
            
            Parameters.Add(param_Symbol);
        }

        var function_Symbol = new Function_Symbol(func_name, func_Return_var, Parameters);

        Current_Context.Define(function_Symbol);

        InferTypes(declaration.Body, func_Return_var, constraints);

        System.Console.WriteLine("Type Inference:");
        var return_type = Equation_System.ResolveType($"Return type of {func_name}", func_Return_var, constraints);
        System.Console.WriteLine("Return Type: " + return_type);

        Current_Context = new Context(original_global_context);

        var Resolved_Parameters = new List<Variable_Symbol>();
        for (int i = 0; i < declaration.Parameters.Count; i++)
        {
            var paramteter_declaration = declaration.Parameters[i];
            var name = paramteter_declaration.VarToken.Value;
            var param_type = Equation_System.ResolveType($"Type of Parameter #{i + 1}, {name}", Parameters[i].Return_Type, constraints);

            var param_Symbol = new Variable_Symbol(name, param_type);
            System.Console.WriteLine($"Parameter {name} type: " + param_type);
            Resolved_Parameters.Add(param_Symbol);
            Current_Context.Define(param_Symbol);
        }

        var func_symbol = new Function_Symbol(func_name, return_type, Resolved_Parameters);
        func_symbol.Body = declaration.Body;

        Current_Context.Define(func_symbol);

        var actual_return = typecheck(declaration.Body);

        if (return_type != actual_return)
            throw new Exception($"Return type of {func_name} is inconsistent with type inference and type checking");

        Current_Context = original_global_context;

        Current_Context.Define(func_symbol);

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

            InferTypes(if_else.Condition, SimpleType.BOOLEAN(), Constraints);
            InferTypes(if_else.True_Clause, Expected, Constraints);
            InferTypes(if_else.False_Clause, Expected, Constraints);
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

            var condition_T = typecheck(if_else.Condition);
            if (condition_T != SimpleType.BOOLEAN())
                throw new Exception(SEMANTIC_ERROR + $"{Token.BOOLEAN} expected in condition of if-else at {if_else.BeginPos}");
            
            var True_Clause = typecheck(if_else.True_Clause);
            var False_Clause = typecheck(if_else.False_Clause);

            if (True_Clause != False_Clause)
                throw new Exception(SEMANTIC_ERROR + $"If-else statement at {if_else.BeginPos} has two different possible return types {True_Clause} and {False_Clause}");

            return True_Clause;
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
        => $"Function {name} takes {Expected} Parameters, {Actual} passed instead at {pos}";

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

    public Equation Extract_Equation(SimpleType member)
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
        SimpleType answ = null;
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