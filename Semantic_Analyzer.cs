namespace HULK;

static class Semantic_Analyzer
{
    public static void Analyze_Define(Command_Node command, SymbolTable global)
    {
        if (command.Statement is Function_Declaration_Node)
        {
            var function_declaration = (Function_Declaration_Node)command.Statement;

            
        }
        else type_check(command, global);
    }


    // let b = (let c = 3 in c), c = 3 in b + c;
    #region Statement Type Checking
    static string type_check(AST_Treenode node, SymbolTable table)
    {
        if (node is Let_In_Node) 
        {
            var let_in_statement = (Let_In_Node)node;
            var curTable = new SymbolTable(table);

            foreach(var declaration in let_in_statement.Declarations)
            {
                var name = declaration.Variable_Node.VarToken.Value;
                var type = type_check(declaration.Value, table);

                var symbol = new Variable_Symbol(type, name);
                if (!curTable.Define(symbol))
                    throw new Exception(Variable_Already_Defined_Error(declaration));
            }

            return type_check(let_in_statement.Statement, curTable);
        }
        else if (node is If_Else_Node)
        {
            var if_else = (If_Else_Node)node;
            var type = type_check(if_else.Condition, table);
            if (type_check(if_else.Condition, table) != Token.BOOLEAN)
                throw new Exception(SEMANTIC_ERROR + $"BOOLEAN expected in if-else statement at {if_else.BeginPos} condition, {type} passed instead");
        
            var true_Type = type_check(if_else.True_Clause, table);
            var false_Type = type_check(if_else.False_Clause, table);

            if (true_Type != false_Type)
                throw new Exception(SEMANTIC_ERROR + $"if-else statement at {if_else.BeginPos} has two different possible return types: {true_Type}, {false_Type}");

            // all good

            return true_Type;
        }
        else if (node is BinOp_Node)
        {
            var op = (BinOp_Node)node;

            var left_T = type_check(op.left, table);
            var right_T = type_check(op.right, table);

            switch(op.Operator.Value)
            {
            case Token.PLUS:
            case Token.MINUS:
            case Token.TIMES:
            case Token.DIVISION:
            case Token.MODULO: // we'll have to wait till runtime to see if the operands are integers
            case Token.POWER:
                if (left_T != Token.NUMBER || right_T != Token.NUMBER)
                    throw new Exception(SEMANTIC_ERROR + Invalid_Operands_Binary_Error(left_T, op.Operator, right_T));
                return Token.NUMBER;
            
            case Token.AT_OPERATOR:
                if (left_T != Token.STRING && right_T != Token.STRING || left_T == Token.VOID || right_T == Token.VOID)
                    throw new Exception(SEMANTIC_ERROR + Invalid_Operands_Binary_Error(left_T, op.Operator, right_T));
                return Token.STRING;

            case Token.GREATER:
            case Token.SMALLER:
            case Token.GREATER_EQUAL:
            case Token.SMALLER_EQUAL:
                if (left_T != Token.NUMBER || right_T != Token.NUMBER)
                    throw new Exception(SEMANTIC_ERROR + Invalid_Operands_Binary_Error(left_T, op.Operator, right_T));
                return Token.BOOLEAN;
            
            case KeyWords.OR:
            case KeyWords.AND:
                if (left_T != Token.BOOLEAN || right_T != Token.BOOLEAN)
                    throw new Exception(SEMANTIC_ERROR + Invalid_Operands_Binary_Error(left_T, op.Operator, right_T));
                return Token.BOOLEAN;

            case Token.EQUAL_EQUAL:
                if (left_T != right_T || left_T == Token.VOID || right_T == Token.VOID)
                    throw new Exception(SEMANTIC_ERROR + Invalid_Operands_Binary_Error(left_T, op.Operator, right_T));
                return Token.BOOLEAN;

            default: throw new NotImplementedException(op.GetType().ToString());
            }
        }
        else if (node is UnOp_Node)
        {
            var op = (UnOp_Node)node;
            
            var right_T = type_check(op.right, table);

            if (right_T != Token.NUMBER)
                throw new Exception(SEMANTIC_ERROR + $"Invalid operand of Type {right_T} for unary Operator {op.Operator.Value} at {op.Operator.position}");
            
            return Token.NUMBER;
        }
        else if (node is Function_Call_Node)
        {
            var call = (Function_Call_Node)node;
            var Name = call.Name_node.VarToken.Value;
            var pos = call.Name_node.VarToken.position;

            var symbol = table.Lookup(Name);

            if (symbol == null)
                throw new Exception(Use_Of_Undefined_Function_Error(Name, pos));
            
            if (symbol is not Function_Symbol)
                throw new Exception(Variable_Used_as_Function_Error(Name, pos));

            var function_symbol = (Function_Symbol)symbol;

            if (call.Arguments.Count != function_symbol.Parameters.Count)
            { 
                var required = function_symbol.Parameters.Count;
                var passed = call.Arguments.Count;
                throw new Exception(SEMANTIC_ERROR + $"Function {Name} takes {required} arguments, {passed} passed instead");            
            }
            

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                var argument_T = type_check(call.Arguments[i], table);
                var required = function_symbol.Parameters[i].TYPE_SPEC;

                if (argument_T != required)
                    throw new Exception(SEMANTIC_ERROR + $"Function {Name} takes {required} as parameter #{i + 1}, {argument_T} passed instead");
            }
            
            call.Symbol = function_symbol;

            return function_symbol.TYPE_SPEC;
        }
        else if (node is NUMBER_Node) return Token.NUMBER;
        else if (node is STRING_Node) return Token.STRING;
        else if (node is BOOLEAN_Node) return Token.BOOLEAN;
        else if (node is Variable_Node)
        {
            var variable = (Variable_Node)node;
            var name = variable.VarToken.Value;
            var pos = variable.VarToken.position;

            var symbol = table.Lookup(name);

            if (symbol == null)
                throw new Exception(Use_Of_Undefined_Variable_Error(name, pos));
            
            if (symbol is not Variable_Symbol)
                throw new Exception(Function_Used_as_Variable_Error(name, pos));

            return ((Variable_Symbol)symbol).TYPE_SPEC;
        }
        else throw new NotImplementedException("TypeCheck: " + node.GetType().ToString());
    }

    #endregion
    
    #region Function declarations and type inference 

    static void declare_function(Function_Declaration_Node declaration, SymbolTable global)
    {
        var function_name = declaration.Name_variable.VarToken.Value;
        
        var curTable = new SymbolTable(global);
        foreach(var variable in declaration.Parameters)
        {
            var symbol = new Variable_Symbol(null, variable.VarToken.Value);
            
            if (!curTable.Define(symbol))
            {
                var Name = variable.VarToken.Value;
                var pos = variable.VarToken.position;
                throw new Exception(SEMANTIC_ERROR + $"{Name} at {pos} is already used in the global scope");
            }
        }

        var Return_Type = get_Expected_Return_Type(declaration.Body, global);
        if (Return_Type == null) 
            throw new Exception(SEMANTIC_ERROR + $"Function {function_name} Return type cannot be resolved");
    }

    // function f(n) => if (false) (if (true) f(n) else f(n)) else 3;

    static string get_Expected_Return_Type(AST_Treenode node, SymbolTable table)
    {
        if (node is Let_In_Node)
        {
            var curTable = new SymbolTable(table);
            var let_in_statement = (Let_In_Node)node;

            foreach(var declaration in let_in_statement.Declarations)
            {
                var name = declaration.Variable_Node.VarToken.Value;
                var symbol = new Variable_Symbol(get_Expected_Return_Type(declaration.Value, table), name);

                if (!curTable.Define(symbol))
                    throw new Exception(Variable_Already_Defined_Error(declaration));
            }

            return get_Expected_Return_Type(let_in_statement.Statement, curTable);
        }
        else if (node is If_Else_Node)
        {
            var if_else = (If_Else_Node)node;

            var true_Type = get_Expected_Return_Type(if_else.True_Clause, table);
            var false_Type = get_Expected_Return_Type(if_else.False_Clause, table);

            if (true_Type == null) return false_Type;
            if (false_Type == null) return true_Type;

            if (false_Type != true_Type)
            {
                var pos = if_else.BeginPos;
                throw new Exception(SEMANTIC_ERROR + $"if-else statement at {pos} has two different return types");
            }

            // they are the same
            return true_Type;
        }
        else if (node is BinOp_Node)
        {
            var op = ((BinOp_Node)node).Operator.Type;

            switch(op)
            {
            case Token.PLUS:
            case Token.MINUS:
            case Token.TIMES:
            case Token.DIVISION:
            case Token.MODULO: // we'll have to wait till runtime to see if the operands are integers
            case Token.POWER:
                return Token.NUMBER;

            case Token.AT_OPERATOR:
                return Token.STRING;

            case Token.GREATER:
            case Token.SMALLER:
            case Token.GREATER_EQUAL:
            case Token.SMALLER_EQUAL:
            case Token.EQUAL_EQUAL:
            case KeyWords.OR:
            case KeyWords.AND:
                return Token.BOOLEAN;

            default: throw new NotImplementedException("Return_Type inference");
            }
        }
        else if (node is UnOp_Node) return Token.NUMBER;
        else if (node is Function_Call_Node)
        {
            var call = (Function_Call_Node)node;
            var name = call.Name_node.VarToken.Value;

            var symbol = table.Lookup(name);
            if (symbol == null)
                throw new Exception(Use_Of_Undefined_Function_Error(name, call.Name_node.VarToken.position));
            
            if (symbol is not Function_Symbol)
                throw new Exception(Variable_Used_as_Function_Error(name, call.Name_node.VarToken.position));

            return ((Function_Symbol)symbol).TYPE_SPEC;
            
        }
        else if (node is NUMBER_Node) return Token.NUMBER;
        else if (node is STRING_Node) return Token.STRING;
        else if (node is BOOLEAN_Node) return Token.BOOLEAN;
        else if (node is Variable_Node)
        {
            var variable = (Variable_Node)node;
            var name = variable.VarToken.Value;
            var pos = variable.VarToken.position;

            var symbol = table.Lookup(name);

            if (symbol == null)
                throw new Exception(Use_Of_Undefined_Variable_Error(name, pos));
            
            if (symbol is not Variable_Symbol)
                throw new Exception(Function_Used_as_Variable_Error(name, pos));

            return ((Variable_Symbol)symbol).TYPE_SPEC;
        }

        else throw new NotImplementedException("get_Expected_Return_Type: " + node.GetType().ToString());
    }

    // function f(n) => n == 3;
    // function f(n) => let a = n in a == 4

    static void Infer_Parameter_Types(AST_Treenode node, SymbolTable table, string Expected_Type, List<string> Parameters_in_Current_Scope)
    {
        if (node is Let_In_Node)
        {
            var let_in_statement = (Let_In_Node)node;

            var curTable = new SymbolTable(table);
            foreach(var declaration in let_in_statement.Declarations)
            {
                var name = 
            }
        }
    }

#endregion

    #region  Errors
    const string SEMANTIC_ERROR = "!Semantic Error: ";
    static string Invalid_Operands_Binary_Error(string left_T, Token op, string right_T)
    {
        return $"Invalid operands of type {left_T} and {right_T} for binary Operator {op.Value} at {op.position}";
    }
    static string Variable_Already_Defined_Error(Variable_Declaration_Node declaration)
    {
        return SEMANTIC_ERROR + $"Variable {declaration.Variable_Node.VarToken.Value} at {declaration.Variable_Node.VarToken.position} already defined in this scope";   
    }

    static string Use_Of_Undefined_Function_Error(string Name, int pos)
    {
        return SEMANTIC_ERROR + $"Use of undefined Function {Name} at {pos}";
    }

    static string Variable_Used_as_Function_Error(string Name, int pos)
    {
        return SEMANTIC_ERROR + $"Variable {Name} used as a function at {pos}";
    }

    static string Use_Of_Undefined_Variable_Error(string Name, int pos)
    {
        return SEMANTIC_ERROR + $"Use of undefined Variable {Name} at {pos}";
    }

    static string Function_Used_as_Variable_Error(string Name, int pos)
    {
        return SEMANTIC_ERROR + $"Function {Name} used as a variable at {pos}";
    }

#endregion
}