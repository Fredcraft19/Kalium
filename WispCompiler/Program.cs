using System.Diagnostics;

using WispAOT;

bool fullCompile = true;

CompilerSettings settings = new CompilerSettings
{
    Debug = true,
    PrettyPrintAssembly = true
};

if(fullCompile){

Compiler.CompileToFile(File.ReadAllText("source.wisp"), "assembly", settings);

if (!Error.hasError)
{
    Assembler.AssembleToFile(File.ReadAllText("assembly.wsa"), "bytecode");

    if (!Error.hasError)
    {
        var _psi = new ProcessStartInfo("vm.exe")
        {
            UseShellExecute = false,
        };

        using Process _vm = Process.Start(_psi)!;
        _vm.WaitForExit();
        Console.WriteLine($"VM exited with code {_vm.ExitCode}");
    }
}
else
{
    Console.WriteLine("Compiler refused to generate code with errors");
}


}
else
{
    Assembler.AssembleToFile(File.ReadAllText("assembly.wsa"), "bytecode");

    if (!Error.hasError)
    {
        var _psi = new ProcessStartInfo("vm.exe")
        {
            UseShellExecute = false,
        };

        using Process _vm = Process.Start(_psi)!;
        _vm.WaitForExit();
        Console.WriteLine($"VM exited with code {_vm.ExitCode}");
    }
}
Console.ReadLine();


