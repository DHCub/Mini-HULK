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
        lexer.eat(Token.SEMICOLON);

        return new Command_Node(node);
    }

    AST_Treenode statement()
    {
        if (lexer.curToken.Type == KeyWords.LET) return let_in_statement();
        else if (lexer.curToken.Type == KeyWords.IF) return if_else_statement();
        return expression();
    }

    Let_In_Node let_in_statement()
    {
        var BeginPos = lexer.curToken.position;
        lexer.eat(KeyWords.LET);
        var declarations_Node = variable_declaration_list();

        lexer.eat(KeyWords.IN);
        var statement_Node = statement();

        return new Let_In_Node(BeginPos, declarations_Node, statement_Node);
    }

    List<Variable_Declaration_Node> variable_declaration_list()
    {
        var declaration_Nodes = new List<Variable_Declaration_Node>();
        declaration_Nodes.Add(variable_declaration());

        while(lexer.curToken.Type == Token.COMMA)
        {
            lexer.eat(Token.COMMA);
            declaration_Nodes.Add(variable_declaration());
        }

        return declaration_Nodes;
    }

    Variable_Declaration_Node variable_declaration()
    {
        var var_node = variable();

        lexer.eat(Token.ASSIGN);

        var value_node = statement();

        return new Variable_Declaration_Node(var_node, value_node);
    }

    If_Else_Node if_else_statement()
    {
        var startPos = lexer.curToken.position;
        lexer.eat(KeyWords.IF);
        lexer.eat(Token.OPEN_PARENTHESIS);

        var condition = statement();

        lexer.eat(Token.CLOSE_PARENTHESIS);

        var True_Clause = statement();

        lexer.eat(KeyWords.ELSE);

        var False_Clause = statement();

        return new If_Else_Node(startPos, condition, True_Clause, False_Clause);
    }

    Variable_Node variable()
    {
        var varToken = lexer.curToken;
        lexer.eat(Token.ID);

        return new Variable_Node(varToken);
    }

    Function_Declaration_Node function_declaration()
    {
        lexer.eat(KeyWords.FUNCTION);

        var varToken = variable();

        lexer.eat(Token.OPEN_PARENTHESIS);
        var Parameters = new List<Variable_Node>();
        
        if (lexer.curToken.Type != Token.CLOSE_PARENTHESIS)
            Parameters = parameter_list();

        lexer.eat(Token.CLOSE_PARENTHESIS);

        lexer.eat(Token.ARROW);

        var body = statement();

        return new Function_Declaration_Node(varToken, Parameters, body);
    }

    List<Variable_Node> parameter_list()
    {
        var Parameters = new List<Variable_Node>();
        Parameters.Add(variable());

        while(lexer.curToken.Type == Token.COMMA)
        {
            lexer.eat(Token.COMMA);
            Parameters.Add(variable());
        }

        return Parameters;
    }

    AST_Treenode expression()
    {
        var node = conjunction();

        while(lexer.curToken.Type == KeyWords.OR)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            node = new BinOp_Node(node, op, conjunction());
        }

        return node;
    }

    AST_Treenode conjunction()
    {
        var node = proposition();

        while(lexer.curToken.Type == KeyWords.AND)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            node = new BinOp_Node(node, op, proposition());
        }

        return node;
    }
    
    AST_Treenode proposition()
    {
        var node = member();

        while(lexer.curToken.Type == Token.EQUAL_EQUAL   ||
              lexer.curToken.Type == Token.GREATER       ||
              lexer.curToken.Type == Token.SMALLER       ||
              lexer.curToken.Type == Token.GREATER_EQUAL ||
              lexer.curToken.Type == Token.SMALLER_EQUAL)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            node = new BinOp_Node(node, op, member());
        }

        return node;
    }

    AST_Treenode member()
    {
        var node = arit_string_bool();

        while(lexer.curToken.Type == Token.AT_OPERATOR)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            node = new BinOp_Node(node, op, arit_string_bool());
        }

        return node;
    }

    AST_Treenode arit_string_bool()
    {
        if (lexer.curToken.Type == Token.STRING) return _string();
        if (lexer.curToken.Type == KeyWords.TRUE || lexer.curToken.Type == KeyWords.FALSE) return boolean();
        
        // arithmetic expression expected

        var node = term();

        while(lexer.curToken.Type == Token.PLUS || lexer.curToken.Type == Token.MINUS)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            node = new BinOp_Node(node, op, term());
        }

        return node;
    }

    AST_Treenode term()
    {
        var node = factor();

        while(lexer.curToken.Type == Token.TIMES || lexer.curToken.Type == Token.DIVISION || lexer.curToken.Type == Token.MODULO)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);
            node = new BinOp_Node(node, op, factor());
        }

        return node;
    }

    AST_Treenode factor()
    {
        var node = power();
        
        if(lexer.curToken.Type == Token.POWER)
        {
            var op = lexer.curToken;
            lexer.eat(Token.POWER);
            return new BinOp_Node(node, op, factor());
        }

        return node;
    }

    AST_Treenode power()
    {
        if (lexer.curToken.Type == Token.NUMBER) return number();
        if (lexer.curToken.Type == Token.ID)
        {
            var explorer = new Lexer(lexer);
            explorer.eat(Token.ID);
            if (explorer.curToken.Type == Token.OPEN_PARENTHESIS) return function_call();
            else return variable();
        }
        if (lexer.curToken.Type == Token.OPEN_PARENTHESIS)
        {
            lexer.eat(Token.OPEN_PARENTHESIS);
            var expression = statement();
            lexer.eat(Token.CLOSE_PARENTHESIS);

            return expression;
        }
        if (lexer.curToken.Type == Token.PLUS || lexer.curToken.Type == Token.MINUS)
        {
            var op = lexer.curToken;
            lexer.eat(op.Type);

            return new UnOp_Node(op, power());
        } 

        lexer.Syntatctic_Error(lexer.curToken, Token.NUMBER);
        throw new Exception(UNREACHABLE);
    }

    Function_Call_Node function_call()
    {
        var Name_variable = variable();
        
        lexer.eat(Token.OPEN_PARENTHESIS);
        
        List<AST_Treenode> Arguments;
        if (lexer.curToken.Type != Token.CLOSE_PARENTHESIS)
            Arguments = argument_list();

        else Arguments = new List<AST_Treenode>();

        lexer.eat(Token.CLOSE_PARENTHESIS);

        return new Function_Call_Node(Name_variable, Arguments);
    }

    List<AST_Treenode> argument_list()
    {
        var Arguments = new List<AST_Treenode>();

        Arguments.Add(statement());

        while(lexer.curToken.Type == Token.COMMA)
        {
            lexer.eat(Token.COMMA);
            Arguments.Add(statement());
        }

        return Arguments;
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
    
    const string UNREACHABLE = "\n **********UNREACHABLE********** \n";
}
