namespace HULK;


class Context
{
    Dictionary<string, Symbol> Assignments;
    static Context BuiltIns = new Context(true);
    static bool BuiltInsDefined = false;
    bool isBuiltins;
    Context? enclosing;

    public Context(Context? enclosing = null)
    {
        this.enclosing = enclosing;
        Assignments = new Dictionary<string, Symbol>();
        isBuiltins = false;
        
        if (!BuiltInsDefined) 
        {
            define_Builtins(BuiltIns);
            BuiltInsDefined = true;
        }
    }

    Context (bool builtIn)
    {
        this.enclosing = null;
        Assignments = new Dictionary<string, Symbol>();
        isBuiltins = true;
        
    }

    public Symbol? Lookup(string Name)
    {
        var builtIn = (isBuiltins) ? null : BuiltIns.Lookup(Name);
        
        if (builtIn != null) return builtIn;
        else if (Assignments.ContainsKey(Name))
            return Assignments[Name];
        else if (enclosing != null)
            return enclosing.Lookup(Name);
        else return null;
    }

    public void Define(Symbol symbol)
    {

        if (BuiltIns.Assignments.ContainsKey(symbol.Name) || Assignments.ContainsKey(symbol.Name))
            throw new Exception(Semantic_Analizer.SEMANTIC_ERROR + $"{symbol.Name} already defined in this Context");
        else Assignments[symbol.Name] = symbol;
    }

    public void Define(Symbol symbol, int pos)
    {

        if (BuiltIns.Assignments.ContainsKey(symbol.Name) || Assignments.ContainsKey(symbol.Name))
            throw new Exception(Semantic_Analizer.SEMANTIC_ERROR + $"{symbol.Name} at {pos} already defined in this Context");
        else Assignments[symbol.Name] = symbol;
    }

    static void define_Builtins(Context C)
    {
        var PI_Symbol = new Variable_Symbol(PI, SimpleType.NUMBER());
        C.Assignments[PI] = PI_Symbol;

        var E_Symbol = new Variable_Symbol(E, SimpleType.NUMBER());
        C.Assignments[E] = E_Symbol;

        var Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol("x", SimpleType.NUMBER()));
        var Return_Type = SimpleType.NUMBER();
        var sin_Symbol = new Function_Symbol(SIN, Return_Type, Parameters);
        C.Assignments[SIN] = sin_Symbol;

        Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol("x", SimpleType.NUMBER()));
        Return_Type = SimpleType.NUMBER();
        var cos_Symbol = new Function_Symbol(COS, Return_Type, Parameters);
        C.Assignments[COS] = cos_Symbol;

        Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol("ToWrite", SimpleType.ANY()));
        Return_Type = SimpleType.VOID();
        var print_symbol = new Function_Symbol(PRINT, Return_Type, Parameters);
        C.Assignments[PRINT] = print_symbol;

        Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol("x", SimpleType.NUMBER()));
        Return_Type = SimpleType.NUMBER();
        var sqrt_Symbol = new Function_Symbol(SQRT, Return_Type, Parameters);
        C.Assignments[SQRT] = sqrt_Symbol;

        Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol("x", SimpleType.NUMBER()));
        Return_Type = SimpleType.NUMBER();
        var log_symbol = new Function_Symbol(LOG, Return_Type, Parameters);
        C.Assignments[LOG] = log_symbol;

    }   

    public const string PRINT = "print";
    public const string SIN = "sin";
    public const string COS = "cos";
    public const string SQRT = "sqrt";
    public const string LOG = "log";

    public const string PI = "PI";
    public const string E = "E";
}

abstract class Symbol 
{
    public string Name {get; private set;}
    public SimpleType Return_Type {get; private set;}

    public Symbol (string Name, SimpleType Type)
    {
        this.Name = Name;
        this.Return_Type = Type;
    }
}

class Variable_Symbol : Symbol
{
    public Variable_Symbol(string Name, SimpleType Type) : base(Name, Type) {}
}

class Function_Symbol : Symbol
{
    public List<Variable_Symbol> Parameters;
    public AST_Treenode? Body; // this will be filled in semantic analysis
    public Function_Symbol(string Name, SimpleType Return_Type, List<Variable_Symbol> Parameters) : base(Name, Return_Type)
    {
        this.Parameters = Parameters;
        Body = null;
    }
}

class SimpleType
{

    public int TypeNo {get; private set;}
    static int variable_Number = 0;
    
    SimpleType(int varNo)
    {
        TypeNo = varNo;
    }

    public override string ToString()
    {
        if (TypeNo == _NUMBER) return Token.NUMBER;
        if (TypeNo == _STRING) return Token.STRING;
        if (TypeNo == _VOID) return Token.VOID;
        if (TypeNo == _BOOLEAN) return Token.BOOLEAN;
        if (TypeNo == _ANY) return "ANY TYPE";
        else return $"t_{TypeNo}";
    }

    public bool isLiteral() => TypeNo < 0 && TypeNo != _ANY;

    public static SimpleType ANY() => new SimpleType(_ANY);
    public static SimpleType STRING() => new SimpleType(_STRING);
    public static SimpleType NUMBER() => new SimpleType(_NUMBER);
    public static SimpleType BOOLEAN() => new SimpleType(_BOOLEAN);
    public static SimpleType VOID() => new SimpleType(_VOID);

    public static SimpleType New_Type_Variable()
    {
        var num = variable_Number;
        variable_Number++;
        return new SimpleType(num);
    }

    public static void Reset_Names()
    {
        variable_Number = 0;
    }

    static public bool operator==(SimpleType a, SimpleType b) => a.TypeNo == b.TypeNo;
    static public bool operator!=(SimpleType a, SimpleType b) => !(a == b);
    
    const int _ANY = -5;
    const int _VOID = -4;
    const int _NUMBER = -3;
    const int _STRING = -2;
    const int _BOOLEAN = -1;
}