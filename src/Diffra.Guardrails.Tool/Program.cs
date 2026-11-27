// The diffra-guardrails CLI. The test-structure commands arrive in phase 4 of PLAN.md.
using Diffra.Guardrails.Tool;

if (args.Length > 0 && args[0] == "deps")
{
    return DepsCommand.Run(args[1..], Console.Out, Console.Error);
}

Console.Error.WriteLine(DepsCommand.Usage);
return 2;
