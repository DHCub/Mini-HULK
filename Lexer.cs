namespace HULK;

class Token
{
    public int position;
    public string Type;
    public string Value;
    
    public Token(string Type, string Value, int position)
    {
        this.position = position;
        this.Type = Type;
        this.Value = Value;
    }

    public const string EOF = "EOF";

    public const string EQUAL_EQUAL = "==";

    public const string ASSIGN = "=";

    public const string OPEN_PARENTHESIS = "(";
    public const string CLOSE_PARENTHESIS = ")";

    public const string SEMICOLON = ";";
    public const string COMMA = ",";
    public const string GREATER = ">";
    public const string SMALLER = "<";

    public const string GREATER_EQUAL = ">=";
    public const string SMALLER_EQUAL = "<=";

    public const string ARROW = "=>";

    public const string AT_OPERATOR = "@";

    public const string PLUS = "+";
    public const string MINUS = "-";

    public const string TIMES = "*";
    public const string DIVISION = "/";
    public const string MODULO = "%";

    public const string POWER = "^";

    public const string NUMBER = "NUMBER";
    public const string STRING = "STRING";
    public const string ID = "IDENTIFIER";
    public const string BOOLEAN = "BOOLEAN";
    // public const string ANY_TYPE = "ANY_TYPE";
    public const string VOID = "VOID";
}

class Lexer
{
    string text;
    int pos;
    public Token curToken {get; private set;}
    char curCharacter; //{get; private set;}
    
    public Lexer(string text)
    {
        this.text = text;
        this.pos = 0;
        this.curCharacter = text[pos];

        getNextToken();
    }

    public Lexer(Lexer other)
    {
        this.text = other.text;
        this.pos = other.pos;
        this.curToken = other.curToken;
        this.curCharacter = other.curCharacter;
    }

    void advance()
    {
        if (pos == text.Length - 1) curCharacter = '\0';
        else
        {
            pos++;
            curCharacter = text[pos];
        }
    }

    void jumpSpaces()
    {
        while(curCharacter == ' ') advance();
    }

    char peek()
    {
        if (pos == text.Length - 1) return '\0';
        else return text[pos + 1];
    }

    void getNextToken()
    {
        jumpSpaces();
        switch(curCharacter)
        {
        case '\0':
            curToken = new Token(Token.EOF, Token.EOF, pos + 1);
            return;
        case '+':
            curToken = new Token(Token.PLUS, Token.PLUS, pos);
            advance();
            return;
        case '-':
            curToken = new Token(Token.MINUS, Token.MINUS, pos);
            advance();
            return;
        case '*':
            curToken = new Token(Token.TIMES, Token.TIMES, pos);
            advance();
            return;
        case '/':
            curToken = new Token(Token.DIVISION, Token.DIVISION, pos);
            advance();
            return;
        case '%':
            curToken = new Token(Token.MODULO, Token.MODULO, pos);
            advance();
            return;
        case '@':
            curToken = new Token(Token.AT_OPERATOR, Token.AT_OPERATOR, pos);
            advance();
            return;
        case '>':
            if (peek() == '=')
            {
                curToken = new Token(Token.GREATER_EQUAL, ">=", pos);
                advance();
                advance();
            }
            else
            {
                curToken = new Token(Token.GREATER, Token.GREATER, pos);
                advance();
            }
            return;
        case '<':
            if (peek() == '=')
            {
                curToken = new Token(Token.SMALLER_EQUAL, "<=", pos);
                advance();
                advance();
            }
            else
            {
                curToken = new Token(Token.SMALLER, Token.SMALLER, pos);
                advance();
            }
            return;
        case '=':
            if (peek() == '=')
            {
                curToken = new Token(Token.EQUAL_EQUAL, "==", pos);
                advance();
                advance();
            }
            else if (peek() == '>')
            {
                curToken = new Token(Token.ARROW, Token.ARROW, pos);
                advance();
                advance();
            }
            else
            {
                curToken = new Token(Token.ASSIGN, Token.ASSIGN, pos);
                advance();
            }
            return;
        case ';':
            curToken = new Token(Token.SEMICOLON, Token.SEMICOLON, pos);
            advance();
            return;
        case ',':
            curToken = new Token(Token.COMMA, Token.COMMA, pos);
            advance();
            return;
        case '(':
            curToken = new Token(Token.OPEN_PARENTHESIS, Token.OPEN_PARENTHESIS, pos);
            advance();
            return;
        case ')':
            curToken = new Token(Token.CLOSE_PARENTHESIS, Token.CLOSE_PARENTHESIS, pos);
            advance();
            return;
        case '\"':
            int tokenStart = pos;
            advance();
            var text = new List<char>();
            while(curCharacter != '\"')
            {
                if (curCharacter == '\0')
                    throw new Exception(SYNTATCIC_ERROR + "Unbalanced quotation marks");
                text.Add(curCharacter);
                advance();
            }

            advance();

            curToken = new Token(Token.STRING, new string(text.ToArray()), tokenStart);
            return;
        }
    
        if (char.IsDigit(curCharacter)) curToken = _numberToken();
        else if (char.IsLetter(curCharacter) || curCharacter == '_') curToken = _idToken();

        else throw new Exception($"!Lexical Error, Invalid character {curCharacter} at {pos}");
    }

    Token _numberToken()
    {
        var digList = new List<char>();
        var startPos = pos;
        while(char.IsDigit(curCharacter))
        {
            digList.Add(curCharacter);
            advance();
        }
        if (curCharacter == '.')
        {
            digList.Add(curCharacter);
            advance();
            while(char.IsDigit(curCharacter))
            {
                digList.Add(curCharacter);
                advance();
            }
        }

        return new Token(Token.NUMBER, new string(digList.ToArray()), startPos);
    }

    Token _idToken()
    {
        var letterList = new List<char>();
        var startPos = pos;
        // we already know the first character is not a digit
        while(char.IsLetterOrDigit(curCharacter) || curCharacter == '_')
        {
            letterList.Add(curCharacter);
            advance();
        }

        var word = new string(letterList.ToArray());
        var keyword = KeyWords.GetToken(word, startPos);

        if (keyword == null) return new Token(Token.ID, word, startPos);
        else return keyword;
    }

    public void Syntatctic_Error(Token problem, string expected)
    {
        throw new Exception(SYNTATCIC_ERROR + $"{expected} expected at {problem.position}");
    }

    public void eat(string expectedType)
    {
        if (curToken.Type == expectedType) getNextToken();
        else Syntatctic_Error(curToken, expectedType);
    }

    const string SYNTATCIC_ERROR = "!Syntactic Error: ";

}


static class KeyWords
{
    public const string LET = "let";
    public const string IN = "in";
    
    public const string IF = "if";
    public const string ELSE = "else";

    public const string TRUE = "true";
    public const string FALSE = "false";

    public const string OR = "or";
    public const string AND = "and";

    public const string FUNCTION = "function";

    public static Token GetToken(string word, int pos)
    {
        if (word == LET  || word == IN    ||
            word == IF   || word == ELSE  ||
            word == TRUE || word == FALSE ||
            word == OR   || word == AND || word == FUNCTION)
            
            return new Token(word, word, pos);

        else return null; 
    }

}
