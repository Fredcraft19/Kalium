namespace WispAOT
{
    public static class Error
    {
        public static bool hasError = false;
        public static void Log(string message, ConsoleColor color = ConsoleColor.Red)
        {
            hasError = true;
            Console.ForegroundColor = color;
            Console.WriteLine($"{message}");
            Console.ResetColor();
        }
    }
}