using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Diffra.Guardrails.CSharp;
using Xunit;

namespace Diffra.Guardrails.Tests;

public class ConformanceTests
{
    private static readonly string CasesDirectory = FindCasesDirectory();

    private static string FindCasesDirectory()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (current != null)
        {
            var candidate = Path.Combine(current, "conformance", "cases");
            if (Directory.Exists(candidate))
                return candidate;
            current = Directory.GetParent(current)?.FullName;
        }
        throw new DirectoryNotFoundException("Could not find conformance/cases directory.");
    }

    public static TheoryData<string> GetAllCases()
    {
        var data = new TheoryData<string>();
        foreach (var dir in Directory.GetDirectories(CasesDirectory))
        {
            data.Add(Path.GetFileName(dir));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(GetAllCases))]
    public void Conformance_case_reports_expected_csharp_findings(string caseName)
    {
        var caseDir = Path.Combine(CasesDirectory, caseName);
        var expectedJsonPath = Path.Combine(caseDir, "expected.json");
        var csharpFilePath = Path.Combine(caseDir, "CaseTests.cs");

        Assert.True(File.Exists(expectedJsonPath), $"Missing expected.json in {caseName}");
        Assert.True(File.Exists(csharpFilePath), $"Missing CaseTests.cs in {caseName}");

        var expectedJson = JsonNode.Parse(File.ReadAllText(expectedJsonPath))!.AsObject();
        var expectedFindings = expectedJson["findings"]!.AsArray();

        var sourceText = File.ReadAllText(csharpFilePath);
        var actualFindings = CSharpChecker.CheckText(sourceText, csharpFilePath);

        var expectedCSharpRules = expectedFindings
            .Select(f => (
                Rule: f!["rule"]!.GetValue<string>(),
                Line: f["line"]!["csharp"]!.GetValue<int>()))
            .OrderBy(f => f.Line)
            .ThenBy(f => f.Rule)
            .ToList();

        var actualCSharpRules = actualFindings
            .Select(f => (
                Rule: f.Rule,
                Line: f.Line))
            .OrderBy(f => f.Line)
            .ThenBy(f => f.Rule)
            .ToList();

        Assert.Equal(expectedCSharpRules.Count, actualCSharpRules.Count);
        for (var i = 0; i < expectedCSharpRules.Count; i++)
        {
            Assert.Equal(expectedCSharpRules[i].Rule, actualCSharpRules[i].Rule);
            Assert.Equal(expectedCSharpRules[i].Line, actualCSharpRules[i].Line);
        }
    }
}
