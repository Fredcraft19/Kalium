namespace WispAOT
{
    public static class Compiler
    {
        /// <summary>
        /// Compiles Wisp source code into assembly, returning the assembly
        /// </summary>
        /// <param name="source">Wisp source code</param>
        /// <returns></returns>
        public static string Compile(string source)
        {
            List<Token> tokens = new();
            List<Node> nodes = new();

            Tokeniser.Tokenise(ref tokens, source);
            Parser.GenerateNodes(ref nodes, tokens);
            Analyser.Analyse(ref nodes);
            return CodeGen.Generate(nodes);
        }
        /// <summary>
        /// Compiles Wisp source code into assembly and puts it in a file
        /// </summary>
        /// <param name="source">Wisp source code</param>
        /// <param name="fileName">File name (without extension which is .wsa for Wisp Source Assembly)</param>
        public static void CompileToFile(string source, string fileName)
        {
            string wsa = Compile(source);
            File.WriteAllText(fileName+".wsa", wsa);
        }
    }
}

