namespace HULK;


class Parser
{
    Lexer lexer;
    public Parser(Lexer lexer)
    {
        this.lexer = lexer;
    }

    public Command_Node Get_Command_AST()
    {
        if (lexer.curToken.Type == KeyWords.FUNCTION) return new Command_Node(function_declaration());
        
        var node = statement();
        if (node is null)
            throw new Exception($"Expression Expected as input");
            
        lexer.eat(Token.SEMICOLON);


        if(lexer.curToken.Type != Token.EOF)
            throw new Exception("End of input expected after ';' token");

        return new Command_Node(node);
    }

    AST_Treenode? statement()
    {
        if (lexer.curToken.Type == KeyWords.LET) return let_in_statement();
        else if (lexer.curToken.Type == KeyWords.IF) return if_else_statement();
        return expression();
    }

    Let_In_Node let_in_statement()
    {
        var BeginPos = lexer.curToken.position;
        lexer.eat(KeyWords.LET);
        var declarations_Node = variable_declaration_list(BeginPos);

        var inPos = lexer.curToken.position;
        lexer.eat(KeyWords.IN);
        
        var statement_Node = statement();
        if (statement_Node is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expression expected after in keyword at {inPos} in let-in statement");

        return new Let_In_Node(BeginPos, declarations_Node, statement_Node);
    }

    List<Variable_Declaration_Node> variable_declaration_list(int BeginPos)
    {
        var declaration_Nodes = new List<Variable_Declaration_Node>();
        var name  = variable();
        if (name is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + $"Variable declaration expected after let in let-in statement at {BeginPos}");

        lexer.eat(Token.ASSIGN);

        var expr = statement();
        if (expr is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expression expected to assign to variable at {name.VarToken.position} in let-in statement");

        declaration_Nodes.Add(new Variable_Declaration_Node(name, expr));

        while(lexer.curToken.Type == Token.COMMA)
        {
            var commaPos = lexer.curToken.position;
            lexer.eat(Token.COMMA);

            name = variable();
            if (name is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Variable declaration expected after ',' token at {commaPos} in let-in statement");

            lexer.eat(Token.ASSIGN);

            expr = statement();
            if (expr is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expression expected to assign to variable at {name.VarToken.position} in let-in statement");

            declaration_Nodes.Add(new Variable_Declaration_Node(name, expr));
        }

        return declaration_Nodes;
    }

    If_Else_Node if_else_statement()
    {
        var startPos = lexer.curToken.position;
        lexer.eat(KeyWords.IF);
        lexer.eat(Token.OPEN_PARENTHESIS);
        var Conditions = new List<AST_Treenode>();
        var Clauses = new List<AST_Treenode>();

        var condition = statement();
        if (condition is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + $"Proposition expected in first Condition in if-else statement at {startPos}");
        Conditions.Add(condition);


        var pos = lexer.curToken.position;
        lexer.eat(Token.CLOSE_PARENTHESIS);

        var clause = statement();
        if (clause is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expression expected after ')' token at {pos} in if-else statement");
        Clauses.Add(clause);
        
        while (lexer.curToken.Type == KeyWords.ELIF)
        {
            lexer.eat(KeyWords.ELIF);
            pos = lexer.curToken.position;
            lexer.eat(Token.OPEN_PARENTHESIS);
            
            condition = statement();
            if (condition is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Proposition expected after '(' token at {pos} in if-else statement");

            pos = lexer.curToken.position;    
            lexer.eat(Token.CLOSE_PARENTHESIS);
        
            Conditions.Add(condition);

            clause = statement();
            if (clause is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expression expected after ')' token at {pos} in if-else statement");
            
            Clauses.Add(clause);
        }

        pos = lexer.curToken.position;
        lexer.eat(KeyWords.ELSE);

        var Else_Clause = statement();
        
        if (Else_Clause is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expression expected after else at {pos} in if-else statement");

        return new If_Else_Node(startPos, Conditions, Clauses, Else_Clause);
    }
    Function_Declaration_Node function_declaration()
    {
        lexer.eat(KeyWords.FUNCTION);

        var varToken = variable();
        
        if (varToken is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + "Function name expected after function keword");

        lexer.eat(Token.OPEN_PARENTHESIS);
        (List<Variable_Node> , List<SimpleType?> ) Parameters;
        
        if (lexer.curToken.Type != Token.CLOSE_PARENTHESIS)
            Parameters = parameter_list();
        else Parameters = (new List<Variable_Node>(), new List<SimpleType?>());
        
        lexer.eat(Token.CLOSE_PARENTHESIS);

        SimpleType? Return_Type;
        if (lexer.curToken.Type == Token.COLON)
        {
            lexer.eat(Token.COLON);
            var type_spec = type_specifier();
            if (type_spec is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + "Type specifier expected as return type after ':' token");
            Return_Type = type_spec;
        }
        else Return_Type = null;

        lexer.eat(Token.ARROW);

        var body = statement();

        if (body is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + "Statement expected as body of function declaration");

        return new Function_Declaration_Node(varToken, Parameters.Item1, Parameters.Item2, Return_Type, body);
    }

    (List<Variable_Node> parameter_list, List<SimpleType?> Type_Specifiers) parameter_list()
    {
        var Parameters = new List<Variable_Node>();
        var Type_Specifiers = new List<SimpleType?>();
        
        var parameter = variable();
        
        if (parameter is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + "Parameter declaration expected after (");
        
        Parameters.Add(parameter);
        if (lexer.curToken.Type == Token.COLON)
        {
            lexer.eat(Token.COLON);
            var type_spec = type_specifier();
            
            if (type_spec is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + "Type specifier expected after : token in parameter declaration");
            if (type_spec == SimpleType.VOID())
                throw new Exception(Semantic_Analizer.SEMANTIC_ERROR + "VOID is not a valid type for a function Parameter");

            Type_Specifiers.Add(type_spec);   
        }
        else Type_Specifiers.Add(null);

        while(lexer.curToken.Type == Token.COMMA)
        {
            lexer.eat(Token.COMMA);
            parameter = variable();
            if (parameter is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + "Parameter declaration expected after ',' token");

            Parameters.Add(parameter);
            if(lexer.curToken.Type == Token.COLON)
            {
                lexer.eat(Token.COLON);
                var type_spec = type_specifier();

                if (type_spec is null)
                    throw new Exception(Lexer.SYNTATCIC_ERROR + "Type specifier expected after : token in parameter declaration");
                if (type_spec == SimpleType.VOID())
                    throw new Exception(Semantic_Analizer.SEMANTIC_ERROR + "VOID is not a valid type for a function Parameter");
                
                Type_Specifiers.Add(type_spec);

            }
            else Type_Specifiers.Add(null);            
        }

        return (Parameters, Type_Specifiers);
    }


    AST_Treenode? expression()
    {
        var node = conjunction();
        if (node is null) return null;

        while(lexer.curToken.Type == KeyWords.OR)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            var right = conjunction();
            if (right is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Boolean expression expected after {op.Value} operator at {op.position}"
                + forgot_Parentheses());

            node = new BinOp_Node(node, op, right);
        }

        return node;
    }

    AST_Treenode? conjunction()
    {
        var node = proposition();
        if (node is null) return null;


        while(lexer.curToken.Type == KeyWords.AND)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            var right = proposition();
            if (right is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Boolean expression expected after {op.Value} operator at {op.position}"
                + forgot_Parentheses());

            node = new BinOp_Node(node, op, right);
        }

        return node;
    }
    
    AST_Treenode? proposition()
    {
        var node = member();
        if (node is null) return null;

        while(lexer.curToken.Type == Token.EQUAL_EQUAL   ||
              lexer.curToken.Type == Token.NOT_EQUAL     ||
              lexer.curToken.Type == Token.GREATER       ||
              lexer.curToken.Type == Token.SMALLER       ||
              lexer.curToken.Type == Token.GREATER_EQUAL ||
              lexer.curToken.Type == Token.SMALLER_EQUAL)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            var right = member();
            if (right is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Boolean literal, string, or arithmetic expression expected after {op.Value} at {op.position}"
                + forgot_Parentheses());

            node = new BinOp_Node(node, op, right);
        }

        return node;
    }

    AST_Treenode? member()
    {
        var node = arit_string_bool();
        if (node is null) return null;

        while(lexer.curToken.Type == Token.AT_OPERATOR)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            var right = arit_string_bool();
            if (right is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Boolean literal, string, or arithmetic expression expected after {op.Value} at {op.position}"
                + forgot_Parentheses());

            node = new BinOp_Node(node, op, right);
        }

        return node;
    }

    AST_Treenode? arit_string_bool()
    {
        if (lexer.curToken.Type == Token.STRING) return _string();
        if (lexer.curToken.Type == KeyWords.TRUE || lexer.curToken.Type == KeyWords.FALSE) return boolean();
        
        // arithmetic expression expected

        var node = term();
        if (node is null) return null;

        while(lexer.curToken.Type == Token.PLUS || lexer.curToken.Type == Token.MINUS)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            var right = term();
            if (right is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Arithmetic expression expected after {op.Value} at {op.position}"
                + forgot_Parentheses());

            node = new BinOp_Node(node, op, right);
        }

        return node;
    }

    AST_Treenode? term()
    {
        var node = factor();
        if (node is null) return null;

        while(lexer.curToken.Type == Token.TIMES || lexer.curToken.Type == Token.DIVISION || lexer.curToken.Type == Token.MODULO)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            var right = factor();
            if (right is null)
               throw new Exception(Lexer.SYNTATCIC_ERROR + $"Arithmetic expression expected after {op.Value} at {op.position}"
                + forgot_Parentheses());

            node = new BinOp_Node(node, op, right);
        }

        return node;
    }

    AST_Treenode? factor()
    {
        var node = power();
        if (node is null) return null;
        
        if(lexer.curToken.Type == Token.POWER)
        {
            var op = lexer.curToken;
            lexer.eat(Token.POWER);

            var right = factor();
            if (right is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Arithmetic expression expected after {op.Value} at {op.position}"
                + forgot_Parentheses());

            return new BinOp_Node(node, op, right);
        }

        return node;
    }

    AST_Treenode? power()
    {
        if (lexer.curToken.Type == Token.NUMBER) return number();
        if (lexer.curToken.Type == Token.ID)
        {
            var explorer = new Lexer(lexer);
            explorer.eat(Token.ID);
            if (explorer.curToken.Type == Token.OPEN_PARENTHESIS) return function_call();
            else return variable()!; // we know there's an identifier
        }
        if (lexer.curToken.Type == Token.OPEN_PARENTHESIS)
        {
            var pos = lexer.curToken.position;
            
            lexer.eat(Token.OPEN_PARENTHESIS);
            
            var expression = statement();
            if (expression is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expression expected after '(' token at {pos}");
            
            lexer.eat(Token.CLOSE_PARENTHESIS);

            return expression;
        }
        if (lexer.curToken.Type == Token.PLUS || lexer.curToken.Type == Token.MINUS)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            var right = power();
            if (right is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Arithmetic expression expected after {op.Value} at {op.position}"
                + forgot_Parentheses());

            return new UnOp_Node(op, right);
        } 

        return null;
        // lexer.Syntatctic_Error(lexer.curToken, Token.NUMBER);
        // throw new Exception(UNREACHABLE);
    }

    Function_Call_Node? function_call()
    {
        // this is only called when an identifier is detected followed by parenthesis, not null
        var Name_variable = variable()!; 
        
        var pos = lexer.curToken.position;
        lexer.eat(Token.OPEN_PARENTHESIS);
        
        List<AST_Treenode> Arguments;
        if (lexer.curToken.Type != Token.CLOSE_PARENTHESIS)
            Arguments = argument_list(pos);

        else Arguments = new List<AST_Treenode>();

        lexer.eat(Token.CLOSE_PARENTHESIS);

        return new Function_Call_Node(Name_variable, Arguments);
    }

    List<AST_Treenode> argument_list(int firtParenthesisPos)
    {
        var Arguments = new List<AST_Treenode>();
        var expression = statement();
        if (expression is null)
            throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expected expression after '(' token at {firtParenthesisPos} as argument in function call");

        Arguments.Add(expression);

        while(lexer.curToken.Type == Token.COMMA)
        {
            var commaPos = lexer.curToken.position;
            lexer.eat(Token.COMMA);
            expression = statement();
            if (expression is null)
                throw new Exception(Lexer.SYNTATCIC_ERROR + $"Expected expression after ',' token at {commaPos} as argument in function call");

            Arguments.Add(expression);
        }

        return Arguments;
    }

    SimpleType? type_specifier()
    {
        if (lexer.curToken.Type == KeyWords.BOOLEAN) {lexer.eat(KeyWords.BOOLEAN); return SimpleType.BOOLEAN();}
        else if (lexer.curToken.Type == KeyWords.STRING) {lexer.eat(KeyWords.STRING); return SimpleType.STRING();}
        else if (lexer.curToken.Type == KeyWords.NUMBER) {lexer.eat(KeyWords.NUMBER); return SimpleType.NUMBER();}
        else if (lexer.curToken.Type == KeyWords.VOID) {lexer.eat(KeyWords.VOID); return SimpleType.VOID();}
        else return null;
    }

    BOOLEAN_Node boolean()
    {
        if (lexer.curToken.Type == KeyWords.TRUE) 
        {
            var pos = lexer.curToken.position;
            lexer.eat(KeyWords.TRUE);
            return new BOOLEAN_Node(true, pos);
        }
        else if (lexer.curToken.Type == KeyWords.FALSE)
        {
            var pos = lexer.curToken.position;
            lexer.eat(KeyWords.FALSE);
            return new BOOLEAN_Node(false, pos);
        }

        lexer.Syntatctic_Error(lexer.curToken, "BOOLEAN");

        throw new Exception(UNREACHABLE);
    }

    NUMBER_Node number()
    {
        var number = lexer.curToken;
        var pos = lexer.curToken.position;
        lexer.eat(Token.NUMBER);

        return new NUMBER_Node(double.Parse(number.Value), pos);
    }

    STRING_Node _string()
    {
        Token s = lexer.curToken;
        lexer.eat(Token.STRING);

        return new STRING_Node(s.Value, s.position);                
    }
    
    
    Variable_Node? variable()
    {
        var varToken = lexer.curToken;
        
        if (lexer.curToken.Type == Token.ID)
        {
            lexer.eat(Token.ID);
            return new Variable_Node(varToken);
        }
        else return null;

    }



    const string UNREACHABLE = "\n **********UNREACHABLE********** \n";
    string forgot_Parentheses()
    {
        return ((lexer.curToken.Type == KeyWords.IF || lexer.curToken.Type == KeyWords.LET) ? "\n Maybe you forgot '(' before let-in or if-else statement" : "");
    }
}
