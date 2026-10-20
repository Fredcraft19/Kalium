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
            {"char",        new VariableData{bytes=1, type=DT.Uint}},

            {"uint",        new VariableData{bytes=4, type=DT.Uint}},

            {"int",         new VariableData{bytes=4, type=DT.Int}},
            {"float",       new VariableData{bytes=4, type=DT.Float}},

            {"void", new VariableData{bytes=0, type=DT.Void}}
        };

        public static Dictionary<string, CustomObject> customObjects = new()
        {
            // developer made  
        };

        public static VariableData MakePointer(VariableData pointee) => new()
        {
            bytes = 8,
            type = DT.Pointer,
            isPointer = true,
            pointeeType = pointee
        };
        public static VariableData MakeReference(VariableData pointee) => new()
        {
            bytes = 8,
            type = DT.Pointer,
            isReference = true,
            pointeeType = pointee
        };

        public static Dictionary<string, VariableData> customTypes = new()
        {
            // Developer-Made Types  
        };

        public static List<string> keywords = new()
        {
            // SUPRISINGLY EASY STUFF
            "if",
            "else",
            "while",
            "for",
            "return",
            // Pointer Joy
            "new",           // its getting rough..
            "delete",        // really rough......... 
            "nullptr",       // im tired boss.. 

            // OOP FUNNN! ^_^
            "class",        // oh no.
            "struct"        // no.. really?! no
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
        public bool isReference = false;
        public VariableData? pointeeType = null;
    }
    public enum DT
    {
        Uint,
        Int,
        Float,
        Bool,
        Void,

        Pointer,

        Custom
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
    public class ObjectVariable
    {
        public string? name;
        public VariableData type = null!;
        public uint offset;         // how big the singular variable is in memory
        public uint classOffset;    // the memory offset from classAddress + classOffset to get the members address for read/writing
    }

    public class CustomObject
    {
        // name not needed as its the key in Dictionary

        public uint totalMemory;
        public List<ObjectVariable> variables = new();

    }
}