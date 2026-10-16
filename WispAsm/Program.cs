using WispAOT;

// .wsa -> Wisp Source Assembly

string source = File.ReadAllText("assembly.wsa");

Assembler.AssembleToFile(source, "bytecode");

Console.ReadLine();

