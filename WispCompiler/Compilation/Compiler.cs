using System.Diagnostics;

namespace WispAOT
{
    public static class Compiler
    {
        /// <summary>
        /// Compiles Wisp source code into assembly, returning the assembly
        /// </summary>
        /// <param name="source">Wisp source code</param>
        /// <returns></returns>
        public static string Compile(string source, CompilerSettings settings)
        {
            Stopwatch timer = new Stopwatch();
            if(settings == null) settings = new();

            Error.hasError = false;
            
            List<Token> tokens = new();
            List<Node> nodes = new();

            timer.Reset();
            timer.Start();

            Tokeniser.Tokenise(ref tokens, source, settings.Log);           
            Parser.GenerateNodes(ref nodes, tokens);
            Analyser.Analyse(ref nodes);
            string wsa = CodeGen.Generate(nodes, settings.PrettyPrintAssembly);
            timer.Stop();
            Console.WriteLine($"Compiled in: {timer.ElapsedMilliseconds}ms");
            return wsa;
        }
        /// <summary>
        /// Compiles Wisp source code into assembly and puts it in a file
        /// </summary>
        /// <param name="source">Wisp source code</param>
        /// <param name="fileName">File name (without extension which is .wsa for Wisp Source Assembly)</param>
        public static void CompileToFile(string source, string fileName, CompilerSettings settings)
        {
            string wsa = Compile(source, settings);
            File.WriteAllText(fileName+".wsa", wsa);
        }
    }
}

