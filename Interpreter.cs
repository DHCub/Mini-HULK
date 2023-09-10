namespace HULK;
// no type safety taken into account, the semantic analyzer needs to take care of that
// it also needs to put the bodies of function declarations in their calls, by filling
// the symbol field

static class Interpreter
{
    static Activation_Record CurRecord = null!;
    public static object? Evaluate(AST_Treenode Statement)
    {
        CurRecord = new Activation_Record();

        return eval(Statement);
    }

    static object? eval(AST_Treenode node) // might return null when calling print for example
    {
        if (node is NUMBER_Node) return ((NUMBER_Node)node).Value;
        else if (node is STRING_Node) return ((STRING_Node)node).Value;
        else if (node is BOOLEAN_Node) return ((BOOLEAN_Node)node).Value;
        // the following will never return null, if it could, an error should have already been
        // thrown by the semantic analyzer
        else if (node is Variable_Node) return CurRecord.Lookup(((Variable_Node)node).VarToken.Value)!;
        else if (node is Function_Call_Node)
        {
            var call = (Function_Call_Node)node;
            var name = call.Name_node.VarToken.Value;
            if (name == Context.PRINT)
            {
                var argument = eval(call.Arguments[0]);
                if (argument is double) System.Console.WriteLine((double)argument);
                if (argument is string) System.Console.WriteLine((string)argument);
                if (argument is bool) System.Console.WriteLine((bool)argument);

                return null;
            }
            else if (name == Context.COS)
            {
                var argument = eval(call.Arguments[0]);
                return Math.Cos((double)argument!); // semantic analyzer ensured non-null
            }
            else if (name == Context.SIN)
            {
                var argument = eval(call.Arguments[0]);
                return Math.Sin((double)argument!); // semantic analyzer ensured non-null

            }
            else if (name == Context.SQRT)
            {
                var argument = (double)eval(call.Arguments[0])!; // semantic analyzer ensured non-null
                if (argument < 0) throw new Exception($"Square Root of negative number detected");
                return Math.Sqrt(argument);

            }
            else
            {
                var original_Record = CurRecord;
                var callRecord = new Activation_Record();

                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    // the symbol must have been filled out by the semantic analyzer
                    // the argument must return something, ensured by the analyzer as well
                    callRecord.Store(call.Symbol!.Parameters[i].Name, eval(call.Arguments[i])!);
                }

                CurRecord = callRecord;

                var answ = eval(call.Symbol!.Body!);

                CurRecord = original_Record;
                return answ;
            }
            
        }
        else if (node is UnOp_Node)
        {
            var op = (UnOp_Node)node;
            var right = (double)eval(op.right)!;

            if (op.Operator.Value == Token.MINUS) return -right;
            else return right; // +
        }
        else if (node is BinOp_Node)
        {
            var op = (BinOp_Node)node;
            var left = eval(op.left)!;
            var right = eval(op.right)!;

            switch(op.Operator.Value)
            {
            case Token.PLUS:
            case Token.MINUS:
            case Token.TIMES:
            case Token.DIVISION:
            case Token.MODULO:
            case Token.POWER:
                return Arithmetic_Binop_Operate((double)left, op.Operator, (double)right);
            
            case Token.EQUAL_EQUAL:
                if (left.GetType() == right.GetType())
                {
                    if (left is double) return (double)left == (double)right;
                    if (left is string) return (string)left == (string)right;
                    if (left is bool) return (bool)left == (bool)right;
                    throw new Exception("UNEXPECTED TYPE Eval");
                }
                return false;
            case Token.NOT_EQUAL:
                if (left.GetType() != right.GetType()) return true;
                else
                {
                    if (left is double) return (double)left != (double)right;
                    if (left is string) return (string)left != (string)right;
                    if (left is bool) return (bool)left != (bool)right;
                    throw new Exception("UNEXPECTED TYPE Eval");   
                }
            
            case Token.SMALLER:
            case Token.GREATER:
            case Token.GREATER_EQUAL:
            case Token.SMALLER_EQUAL:
                return Compare_Doubles((double)left, op.Operator, (double)right);

            case Token.AT_OPERATOR:
                string L = "";
                if (left is string) L = (string)left;
                else if (left is double) L = ((double)left).ToString();
                else if (left is bool) L = ((bool)left).ToString();
                else throw new Exception();

                string R = "";
                if (right is string) R = (string)right;
                else if (right is double) R = ((double)right).ToString();
                else if (right is bool) R = ((bool)right).ToString();
                else throw new Exception();

                return L + R;
            
            case KeyWords.OR: return (bool)left || (bool)right;
            case KeyWords.AND: return (bool)left && (bool)right;
                            
            default: throw new Exception("UNSUPPORTED OPERATOR");   
            }
        }
        else if (node is If_Else_Node)
        {
            var if_else = (If_Else_Node)node;

            for (int i = 0; i < if_else.Clauses.Count; i++)
            {
                if ((bool)eval(if_else.Conditions[i])!) return eval(if_else.Clauses[i]);
            }
            return eval(if_else.Else_Clause);
        }
        else if (node is Let_In_Node)
        {
            var let_in = (Let_In_Node)node;

            var original_Record = CurRecord;

            CurRecord = new Activation_Record(CurRecord);
            
            foreach(var declaration in let_in.Declarations)
            {
                CurRecord.Store(declaration.Variable_Node.VarToken.Value, eval(declaration.Value)!);
            }

            var answ = eval(let_in.Statement);

            CurRecord = original_Record;

            return answ;
        }
    
        throw new Exception("UNREACHABLE eval");
    }

    static double Arithmetic_Binop_Operate(double left, Token op, double right)
    {
        switch(op.Value)
        {
        case Token.PLUS: return left + right;
        case Token.MINUS: return left - right;
        case Token.TIMES: return left * right;
        case Token.DIVISION: 
            if (right == 0) throw new Exception(Zero_Division());
            return left / right;
        case Token.MODULO: 
            if (right == 0) throw new Exception(Zero_Division());
            return left % right;
        case Token.POWER: return Math.Pow(left, right);
        default: throw new Exception("UNREACHABLE Arithmetic_Binop_Operate");
        }
    }

    static bool Compare_Doubles(double a, Token op, double b)
    {
        switch(op.Value)
        {
        case Token.GREATER: return a > b;
        case Token.SMALLER: return a < b;
        case Token.GREATER_EQUAL: return a >= b;
        case Token.SMALLER_EQUAL: return a <= b;
        default: throw new Exception("UNREACHABLE Compare");
        }
    }


    static string Zero_Division()
        => $"Zero dvivision detected";
}

