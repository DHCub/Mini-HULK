namespace HULK;

class Activation_Record
{
    Activation_Record parent;
    Dictionary<string, object> Data;

    static Dictionary<string, object> BuiltIns;
    static bool BuiltInsDefined = false;
    bool isBuiltins;

    public Activation_Record(Activation_Record parent = null)
    {
        this.parent = parent;
        Data = new Dictionary<string, object>();
        isBuiltins = false;
        if (!BuiltInsDefined)
        {
            BuiltIns = new Dictionary<string, object>();

            BuiltIns.Add("PI", Math.PI);
            BuiltIns.Add("E", Math.E);

            BuiltInsDefined = true;
        }
    }


    public object Lookup(string name)
    {
        if (BuiltIns.ContainsKey(name)) return BuiltIns[name];
        
        if (!Data.ContainsKey(name))
        {
            if (parent != null) return parent.Lookup(name);
            else return null;
        }
        else return Data[name];
    }

    public void Store(string name, object value)
    {
        this.Data[name] = value;
    }

}