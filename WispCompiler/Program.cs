using System.Diagnostics;

using WispAOT;

Console.WriteLine("Compiling..");
Compiler.CompileToFile(File.ReadAllText("source.wisp"), "assembly");

Console.WriteLine("Assembling..");
Assembler.AssembleToFile(File.ReadAllText("assembly.wsa"), "bytecode");

Console.WriteLine("Running VM..");
var psi = new ProcessStartInfo("vm.exe")
{
    UseShellExecute = false,
};

using Process vm = Process.Start(psi)!;
vm.WaitForExit();
Console.WriteLine($"VM exited with code {vm.ExitCode}");

Console.ReadLine();


