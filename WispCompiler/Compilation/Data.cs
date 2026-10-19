namespace WispAOT
{
    public static class Data
    {
        public static Dictionary<string, VariableData> primativeTypes = new()
        {
            {"uint8",       new VariableData{bytes=1, type=DT.Uint}},
            {"uint16",      new VariableData{bytes=2, type=DT.Uint}},
            {"uint32",      new VariableData{bytes=4, type=DT.Uint}},
            {"uint64",      new VariableData{bytes=8, type=DT.Uint}},

            {"int8",        new VariableData{bytes=1, type=DT.Int}},
            {"int16",       new VariableData{bytes=2, type=DT.Int}},
            {"int32",       new VariableData{bytes=4, type=DT.Int}},
            {"int64",       new VariableData{bytes=8, type=DT.Int}},

            {"float32",     new VariableData{bytes=4, type=DT.Float}},
            {"float64",     new VariableData{bytes=8, type=DT.Float}},

            // 'Shortcut' data types -> Copies with easier names
            {"bool",        new VariableData{bytes=1, type=DT.Bool}},

            {"uint",        new VariableData{bytes=4, type=DT.Uint}},
            {"int",         new VariableData{bytes=4, type=DT.Int}},
            {"float",       new VariableData{bytes=4, type=DT.Float}},

            {"void", new VariableData{bytes=0, type=DT.Void}}
        };

        public static VariableData MakePointer(VariableData pointee) => new()
        {
            bytes = 8,
            type = DT.Pointer,
            pointeeType = pointee
        };

        public static Dictionary<string, VariableData> customTypes = new()
        {
            // Developer-Made Types  
        };

        public static List<string> keywords = new()
        {
            "if",
            "else",
            "while",
            "for",
            "return"
        };
        public static void PrintToken(Token tok)
        {
            Console.WriteLine($"{tok.type}('{tok.value}')");
        }
        public static string FormatToken(Token tok)
        {
            return $"{tok.type}('{tok.value}')";
        }
    }
    public class VariableData
    {
        public uint bytes;
        public DT type;
        public bool isPointer = false;
        public VariableData? pointeeType = null;
    }
    public enum DT
    {
        Uint,
        Int,
        Float,
        Bool,
        Void,

        Pointer
    }

    public class CompilerSettings
    {
        public bool Log = false;
        public bool PrettyPrintAssembly = false;
    }

    public class Symbol
    {
        public VariableData type = null!;
        public uint offset;
    }
    public class MethodData
    {
        public VariableData returnType = null!;
        public List<(string name, VariableData data)> parameters = new();
    }
}
