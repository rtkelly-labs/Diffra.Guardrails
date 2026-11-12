using System.Text.Json;
using Reqnroll;

[Binding]
public sealed class Steps(ScenarioContext context) : IDisposable
{
    private readonly string invocation = Guid.NewGuid().ToString();
    private readonly List<string> events = [];
    private readonly List<object> assertions = [];
    private string mode = "";
    private bool observer;
    private int attempts;
    private string[] rows = [];

    [BeforeScenario(Order = 0)]
    public void Before() => events.Add("before-scenario");

    [AfterScenario(Order = 1000)]
    public void After()
    {
        events.Add("after-scenario");
        var output = Path.Combine(Environment.GetEnvironmentVariable("PROBE_OUTPUT")!, "reqnroll");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, invocation + ".json"), JsonSerializer.Serialize(new
        {
            runner = "reqnroll", nativeId = context.ScenarioInfo.Title,
            invocation, @case = mode, observer, completed = true, events, assertions
        }));
    }

    [Given("the experiment case {string}")]
    public void Configure(string value)
    {
        mode = value;
        if (mode == "setup-fail") throw new InvalidOperationException("Deliberate setup failure");
        observer = mode != "no-observer";
    }

    [When("the public reader returns rows through the recording transport")]
    public void Read()
    {
        events.Add("operation");
        attempts = mode == "zero" ? 0 : mode == "over-budget" ? 4 : 2;
        rows = mode == "functional-fail" ? ["wrong"] : ["A", "B"];
    }

    private void Record(string obligation, string api, object observed, object expected, bool passed)
    {
        events.Add(api);
        assertions.Add(new { obligation, api, observed, expected, passed });
        if (!passed) throw new InvalidOperationException($"{obligation}: expected {JsonSerializer.Serialize(expected)}, observed {JsonSerializer.Serialize(observed)}");
    }

    [Then("the rows match the functional requirement")]
    public void Functional()
    {
        if (mode != "missing-functional")
            Record("Functional.RowsMatch", "rows_match", rows, new[] { "A", "B" }, rows.SequenceEqual(["A", "B"]));
    }

    [Then("the transport attempts meet the retry budget")]
    public void Policy()
    {
        if (mode == "missing-policy") return;
        if (mode == "unreachable")
        {
            if (attempts < 0) RetryBudget();
            return;
        }
        if (mode == "misclassified")
        {
            Record("Resilience.RetryBudget", "rows_match", rows, new[] { "A", "B" }, true);
            return;
        }
        RetryBudget();
    }

    private void RetryBudget() => Record("Resilience.RetryBudget", "retry_budget", attempts, 3, attempts <= 3);

    public void Dispose()
    {
        events.Add("fixture-cleanup");
        var output = Path.Combine(Environment.GetEnvironmentVariable("PROBE_OUTPUT")!, "reqnroll");
        File.WriteAllText(Path.Combine(output, invocation + ".cleanup"), JsonSerializer.Serialize(events));
    }
}
