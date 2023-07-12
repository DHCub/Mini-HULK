namespace HULK;

class SymbolTable
{
    SymbolTable parent;
    Dictionary<string, Symbol> table;
    public SymbolTable(SymbolTable parent = null)
    {
        this.parent = parent;
        this.table = new Dictionary<string, Symbol>();
        Define_BulitIns();
    }

    public bool ContainsSymbol(Symbol symbol)
    {
        return table.ContainsValue(symbol);
    }

    public void Define_BulitIns()
    {
        define_print();
        define_cos();
        define_sin();      
        define_sqrt();
        define_PI();
    }

    bool define_print()
    {
        var Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol(null, "ToPrint"));
        
        var print = new Function_Symbol(Token.VOID, "print", Parameters);

        return this.Define(print);
    }

    bool define_cos()
    {
        var Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol(Token.NUMBER, "x"));

        var cos = new Function_Symbol(Token.NUMBER, "cos", Parameters);

        return this.Define(cos);
    }

    bool define_sin()
    {
        var Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol(Token.NUMBER, "x"));

        var sin = new Function_Symbol(Token.NUMBER, "sin", Parameters);

        return this.Define(sin);
    }

    bool define_sqrt()
    {
        var Parameters = new List<Variable_Symbol>();
        Parameters.Add(new Variable_Symbol(Token.NUMBER, "x"));

        var sqrt = new Function_Symbol(Token.NUMBER, "sqrt", Parameters);

        return this.Define(sqrt);
    }

    bool define_PI()
    {
        return this.Define(new Variable_Symbol(Token.NUMBER, "PI"));
    }

    public bool Define(Symbol symbol)
    {
        if (table.ContainsKey(symbol.Name)) return false;

        table[symbol.Name] = symbol;
        return true;
    }

    public Symbol Lookup(string symbol_name)
    {
        if (!table.ContainsKey(symbol_name))
        {
            if (parent != null) return parent.Lookup(symbol_name);
            else return null;
        }
        else return table[symbol_name];
    }

    public Symbol Lookup_Local(string symbol_name)
    {
        if (!table.ContainsKey(symbol_name)) return null;
        else return table[symbol_name];
    }

}

abstract class Symbol
{
    public string Name;
    public string TYPE_SPEC;
    public Symbol(string TYPE_SPEC, string Name)
    {
        this.TYPE_SPEC = TYPE_SPEC;
        this.Name = Name;
    }
}

class Variable_Symbol : Symbol
{
    public Variable_Symbol(string TYPE_SPEC, string Name) : base(TYPE_SPEC, Name){}
}

class Function_Symbol : Symbol
{
    public List<Variable_Symbol> Parameters;
    public AST_Treenode Body;
    public Function_Symbol(string Return_Type, string Name, List<Variable_Symbol> Parameters) : base(Return_Type, Name)
    {
        this.Parameters = Parameters;
        Body = null; // filled on semantic analysis
    }
}