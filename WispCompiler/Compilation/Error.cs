namespace WispAOT
{
    public static class Error
    {
        public static void Log(string message, ConsoleColor color = ConsoleColor.Red)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{message}");
            Console.ResetColor();
        }
    }
}