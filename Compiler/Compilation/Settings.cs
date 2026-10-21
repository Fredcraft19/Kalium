namespace WispAOT
{
    // Global
    public static class Settings
    {
        public static bool Debug = false;
        public static bool PrettyPrintAssembly = false;
        public static void Extract(CompilerSettings s)
        {
            Debug = s.Debug;
            PrettyPrintAssembly = s.PrettyPrintAssembly;
        }
    }

    // To be passed around
    public class CompilerSettings
    {
        public bool Debug = false;
        public bool PrettyPrintAssembly = false;
    }
}