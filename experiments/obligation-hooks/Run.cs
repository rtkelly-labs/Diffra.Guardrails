// Isolated hook experiment. No DFG adapter, rule or Diffra payload is implemented here.
using System.Diagnostics;
using System.Text.Json.Nodes;

var source = Path.GetFullPath("experiments/obligation-hooks");
if (!Directory.Exists(source)) throw new InvalidOperationException("Run from the repository root.");
string[] runners = ["pytest-bdd", "pytest-plain", "behave", "cucumber-js", "reqnroll"];
var selected = args.Length == 0 ? runners : args;
if (selected.Any(r => !runners.Contains(r))) throw new ArgumentException("Choose pytest-bdd, pytest-plain, behave, cucumber-js or reqnroll.");
var work = Path.GetFullPath(Path.Combine("temp", "obligation-hooks", Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(work);
Copy(source, Path.Combine(work, "fixtures"));
var fixture = Path.Combine(work, "fixtures");
var output = Path.Combine(work, "evidence");
Directory.CreateDirectory(output);
foreach (var runner in new[] { "python", "behave", "typescript", "reqnroll" })
    File.Copy(Path.Combine(fixture, "contract.feature"), Path.Combine(fixture, runner, "contract.feature"));
File.Copy(Path.Combine(fixture, "python", "probe.py"), Path.Combine(fixture, "behave", "probe.py"));
Console.WriteLine($"Experiment artifacts: {work}");

if (selected.Contains("pytest-bdd") || selected.Contains("pytest-plain") || selected.Contains("behave"))
{
    await Command("uv", ["venv", Path.Combine(work, ".venv"), "--python", "python3"], work, 0);
    var python = Path.Combine(work, ".venv", OperatingSystem.IsWindows() ? "Scripts/python.exe" : "bin/python");
    await Command("uv", ["pip", "sync", "--python", python, Path.Combine(fixture, "python", "requirements.txt")], work, 0);
    if (selected.Contains("pytest-bdd"))
        await Command(python, ["-m", "pytest", "test_contract.py", "-q", "--cucumberjson=runner.json"], Path.Combine(fixture, "python"), 1);
    if (selected.Contains("pytest-plain"))
        await Command(python, ["-m", "pytest", "test_plain.py", "-q"], Path.Combine(fixture, "python"), 1);
    if (selected.Contains("behave"))
        await Command(python, ["-m", "behave", ".", "--format", "json", "--outfile", "runner.json"], Path.Combine(fixture, "behave"), 1);
}
if (selected.Contains("cucumber-js"))
{
    var ts = Path.Combine(fixture, "typescript");
    await Command("npm", ["ci", "--ignore-scripts", "--no-audit", "--no-fund"], ts, 0);
    var cucumber = Path.Combine(ts, "node_modules", "@cucumber", "cucumber", "bin", "cucumber.js");
    await Command("node", [cucumber, "contract.feature", "--import", "steps.ts", "--dry-run", "--format", "message:discovery.ndjson"], ts, 0);
    var featureText = File.ReadAllText(Path.Combine(ts, "contract.feature"));
    File.WriteAllText(Path.Combine(ts, "undefined.feature"), featureText.Replace("the rows match the functional requirement", "a deliberately undefined outcome"));
    await Command("node", [cucumber, "undefined.feature", "--import", "steps.ts", "--dry-run", "--format", "message:undefined.ndjson"], ts, 0);
    if (!File.ReadAllText(Path.Combine(ts, "undefined.ndjson")).Contains("UNDEFINED"))
        throw new InvalidOperationException("Undefined-step discovery did not expose UNDEFINED.");
    if (Directory.Exists(Path.Combine(output, "cucumber-js")))
        throw new InvalidOperationException("Dry run unexpectedly executed evidence hooks.");
    await Command("node", [Path.Combine(ts, "node_modules", "gherkin-lint", "dist", "main.js"), "-c", "gherkin-lint.json", "contract.feature"], ts, 0);
    File.WriteAllText(Path.Combine(ts, "invalid-tags.feature"), featureText.Replace("@remote_io", "@remote_oi"));
    await Command("node", [Path.Combine(ts, "node_modules", "gherkin-lint", "dist", "main.js"), "-c", "gherkin-lint.json", "invalid-tags.feature"], ts, 1);
    await Command("node", [cucumber, "contract.feature", "--import", "steps.ts", "--format", "message:messages.ndjson", "--format", "summary"], ts, 1);
}
if (selected.Contains("reqnroll"))
{
    var cs = Path.Combine(fixture, "reqnroll");
    await Command("dotnet", ["restore", "Probe.csproj", "--locked-mode"], cs, 0);
    await Command("dotnet", ["test", "Probe.csproj", "--no-restore", "-m:2", "--logger", "trx;LogFileName=runner.trx"], cs, 1);
}

var expected = JsonNode.Parse(File.ReadAllText(Path.Combine(fixture, "cases.json")))!.AsArray();
var report = new JsonArray();
var defects = new List<string>();
foreach (var runner in selected)
{
    var directory = Path.Combine(output, runner);
    var documents = Directory.Exists(directory)
        ? Directory.GetFiles(directory, "*.json").Select(p => JsonNode.Parse(File.ReadAllText(p))!).ToArray()
        : [];
    var expectedNames = expected.Select(c => c!["case"]!.GetValue<string>()).ToHashSet();
    if (documents.Any(d => !expectedNames.Contains(d["case"]?.GetValue<string>() ?? "")))
        defects.Add($"{runner}: unexpected or unattributed scenario evidence");
    if (documents.Select(d => d["invocation"]!.GetValue<string>()).Distinct().Count() != documents.Length)
        defects.Add($"{runner}: duplicate invocation identity");
    foreach (var item in expected)
    {
        var name = item!["case"]!.GetValue<string>();
        var matches = documents.Where(d => d["case"]?.GetValue<string>() == name).ToArray();
        var doc = matches.Length == 1 ? matches[0] : null;
        if (matches.Length != 1) defects.Add($"{runner}/{name}: expected exactly one sealed invocation, found {matches.Length}");
        var functional = Result(doc, "Functional.RowsMatch", "rows_match", false);
        var policy = Result(doc, "Resilience.RetryBudget", "retry_budget", true);
        if (functional != item["functional"]!.GetValue<string>() || policy != item["policy"]!.GetValue<string>())
            defects.Add($"{runner}/{name}: expected {item["functional"]}/{item["policy"]}, got {functional}/{policy}");
        var events = doc?["events"]?.AsArray().Select(e => e!.GetValue<string>()).ToArray() ?? [];
        if (events.Length < 2 || events[0] != "before-scenario" || events[^1] != "after-scenario")
            defects.Add($"{runner}/{name}: scenario lifecycle not sealed");
        var cleanupPath = Path.Combine(directory, (doc?["invocation"]?.GetValue<string>() ?? "missing") + ".cleanup");
        var cleanup = File.Exists(cleanupPath) ? JsonNode.Parse(File.ReadAllText(cleanupPath))!.AsArray() : [];
        if (cleanup.Count <= events.Length || cleanup[^1]!.GetValue<string>() != "fixture-cleanup" ||
            cleanup[events.Length - 1]!.GetValue<string>() != "after-scenario")
            defects.Add($"{runner}/{name}: expected fixture cleanup after the scenario seal");
        report.Add((JsonNode)new JsonObject { ["runner"] = runner, ["case"] = name, ["functional"] = functional, ["policy"] = policy });
        Console.WriteLine($"{runner,-12} {name,-20} functional={functional,-16} policy={policy}");
    }
}
File.WriteAllText(Path.Combine(work, "summary.json"), report.ToJsonString(new() { WriteIndented = true }));
foreach (var defect in defects) Console.Error.WriteLine(defect);
Console.WriteLine($"{report.Count} scenario results; {defects.Count} experiment expectation failures.");
return defects.Count == 0 ? 0 : 1;

static string Result(JsonNode? doc, string obligation, string api, bool needsObserver)
{
    if (doc == null || doc["completed"]?.GetValue<bool>() != true ||
        string.IsNullOrWhiteSpace(doc["nativeId"]?.GetValue<string>())) return "missing-evidence";
    var assertions = doc["assertions"]!.AsArray().Where(a => a!["obligation"]!.GetValue<string>() == obligation).ToArray();
    if (assertions.Length != 1 || assertions[0]!["api"]!.GetValue<string>() != api ||
        (needsObserver && doc["observer"]?.GetValue<bool>() != true)) return "missing-evidence";
    var assertion = assertions[0]!;
    // Validate the oracle's input as well as the recorded assertion result. This is a tiny,
    // fixed experiment registration table, not a semantic type checker or security boundary.
    bool met;
    if (needsObserver)
    {
        if (assertion["expected"]?.ToJsonString() != "3" ||
            assertion["observed"] is not JsonValue value || !value.TryGetValue<int>(out var attempts) || attempts < 0)
            return "missing-evidence";
        met = attempts <= 3;
    }
    else
    {
        var rows = JsonNode.Parse("[\"A\",\"B\"]");
        if (!JsonNode.DeepEquals(assertion["expected"], rows)) return "missing-evidence";
        met = JsonNode.DeepEquals(assertion["observed"], rows);
    }
    return met && assertion["passed"]?.GetValue<bool>() == true ? "pass" : "fail";
}

static void Copy(string from, string to)
{
    Directory.CreateDirectory(to);
    foreach (var file in Directory.GetFiles(from)) File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
    foreach (var directory in Directory.GetDirectories(from))
        if (Path.GetFileName(directory) is not ("node_modules" or "bin" or "obj" or "__pycache__"))
            Copy(directory, Path.Combine(to, Path.GetFileName(directory)));
}

async Task Command(string executable, string[] arguments, string cwd, int expectedExit)
{
    Console.WriteLine($"{executable} {string.Join(' ', arguments)}");
    var info = new ProcessStartInfo(executable) { WorkingDirectory = cwd, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var argument in arguments) info.ArgumentList.Add(argument);
    info.Environment["PROBE_OUTPUT"] = output;
    info.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
    using var process = Process.Start(info)!;
    var stdout = process.StandardOutput.ReadToEndAsync();
    var stderr = process.StandardError.ReadToEndAsync();
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
    try { await process.WaitForExitAsync(timeout.Token); }
    catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }
    var log = await stdout + await stderr;
    var logPath = Path.Combine(work, $"command-{Guid.NewGuid():N}.log");
    File.WriteAllText(logPath, $"{executable} {string.Join(' ', arguments)}\nexit={process.ExitCode}; expected={expectedExit}\n{log}");
    if (process.ExitCode != expectedExit)
        throw new InvalidOperationException($"Expected exit {expectedExit}, got {process.ExitCode}. See {logPath}\n{log}");
}
