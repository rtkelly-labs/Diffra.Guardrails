using System;
using System.IO;
using System.Linq;
using Diffra.Guardrails.CSharp;

namespace Diffra.Guardrails.Tool;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            Console.WriteLine("diffra-guardrails - Structural and Functional Guardrails Engine");
            Console.WriteLine("Usage: diffra-guardrails check [path] [options]");
            return 0;
        }

        var command = args[0];
        if (command == "check")
        {
            var targetPath = args.Length > 1 ? args[1] : ".";
            var files = Directory.Exists(targetPath)
                ? Directory.GetFiles(targetPath, "*.cs", SearchOption.AllDirectories)
                : File.Exists(targetPath) ? new[] { targetPath } : Array.Empty<string>();

            var totalViolations = 0;
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                var findings = CSharpChecker.CheckText(content, file);
                foreach (var f in findings)
                {
                    totalViolations++;
                    Console.WriteLine($"{f.Path}({f.Line}): {f.Severity} {f.Rule}: {f.Message}");
                    Console.WriteLine($"    Fix: {f.Fix}");
                }
            }

            return totalViolations > 0 ? 1 : 0;
        }

        Console.WriteLine($"Unknown command: {command}");
        return 1;
    }
}
