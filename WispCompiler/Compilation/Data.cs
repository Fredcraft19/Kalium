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
            {"bool",        new VariableData{bytes=1, type=DT.Uint}},

            {"uint",        new VariableData{bytes=4, type=DT.Uint}},
            {"int",         new VariableData{bytes=4, type=DT.Int}},
            {"float",       new VariableData{bytes=4, type=DT.Float}},

            {"void", new VariableData{bytes=0, type=DT.Void}}
        };

        public static Dictionary<string, VariableData> customTypes = new()
        {
            // Developer-Made Types  
        };

        public static List<string> keywords = new()
        {
            // i wish...
            "if",
            "while",
            "return",
            "class",
            "struct"
        };
    }
    public class VariableData
    {
        public uint bytes;
        public DT type;
    }
    public enum DT
    {
        Uint,
        Int,
        Float,
        Void
    }
}