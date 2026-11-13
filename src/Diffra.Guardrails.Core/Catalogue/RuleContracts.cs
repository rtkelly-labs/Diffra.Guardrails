namespace Diffra.Guardrails.Core.Catalogue;

public static class RuleContracts
{
    // DFG1xx - Test Shape & Structure
    public static class DFG101
    {
        public const string Rule = "DFG101";
        public const string Message = "This test has no Arrange/Act/Assert or Given/When/Then markers.";
        public const string Fix = "Add Arrange, Act and Assert markers (or Given, When and Then).";
    }

    public static class DFG102
    {
        public const string Rule = "DFG102";
        public static string SpellingFix(string marker) => $"Change the comment to exactly '{marker}'.";
        public const string OrderFix = "Reorder the blocks to Arrange, Act, Assert (or Given, When, Then).";
        public const string MixedFix = "Use one set of markers in each test.";
    }

    public static class DFG103
    {
        public const string Rule = "DFG103";
        public const string Fix = "Move setup into Arrange, or split this test in two.";
    }

    public static class DFG104
    {
        public const string Rule = "DFG104";
        public const string Fix = "Move this assertion under 'Assert'.";
    }

    public static class DFG105
    {
        public const string Rule = "DFG105";
        public const string Fix = "Split this test in two, one per behaviour, or compare both values in one assertion.";
    }

    public static class DFG106
    {
        public const string Rule = "DFG106";
        public const string Fix = "Add a message that says what behaviour this assertion protects.";
        public const string WarningFix = "Use an assertion form that takes a message, or wrap this check in a registered helper that does.";
    }

    public static class DFG108
    {
        public const string Rule = "DFG108";
        public const string LoopFix = "Replace the loop with a data-driven test: one case per input.";
        public const string ConditionalFix = "Split into two tests, or test the condition directly without branching in the test body.";
    }

    // DFG2xx - Functional Architecture & Clean Code
    public static class DFG201
    {
        public const string Rule = "DFG201";
        public static string Message(string property, string violation) =>
            $"Property getter '{property}' contains non-trivial behavior ({violation}); property getters must be lightweight, allocation-free accessors.";
        public static string Fix(string property) =>
            $"Convert '{property}' to a method, or precompute and store the value in an init-only property or field.";
    }

    public static class DFG202
    {
        public const string Rule = "DFG202";
        public static string Message(string method, string target) =>
            $"In-place mutation '{method}' detected on collection '{target}'; steer toward immutable collections and pure transformations.";
        public static string Fix(string method) =>
            $"Replace '{method}' with a functional transformation, collection expression (e.g., '[.. items, item]'), or immutable collection.";
    }

    public static class DFG203
    {
        public const string Rule = "DFG203";
        public static string Message(string type) =>
            $"Catch block catches broad '{type}' without an exception filter or rethrow; catch-all exception swallowing is disallowed.";
        public const string Fix =
            "Catch a specific exception type, add an exception filter ('when (...)'), or rethrow with 'throw;' to preserve diagnostic flow.";
    }

    public static class DFG204
    {
        public const string Rule = "DFG204";
        public const string Message =
            "Lambda or local function does not capture outer variables and should be declared 'static' to guarantee zero closure allocation.";
        public const string Fix = "Add the 'static' modifier to the lambda or local function.";
    }

    public static class DFG205
    {
        public const string Rule = "DFG205";
        public static string Message(string variable) =>
            $"Variable '{variable}' is declared and subsequently reassigned across conditional branches; prefer single assignment via a switch expression, ternary expression, or early return.";
        public static string Fix(string variable) =>
            $"Initialize '{variable}' directly using a switch expression, ternary expression, or return early.";
    }

    // DFG3xx - Contract & Data Purity (Pure Data vs Behaviour Boundaries)
    public static class DFG301
    {
        public const string Rule = "DFG301";
        public static string Message(string type, string method) =>
            $"Contract type '{type}' declares method '{method}'; pure contract types must contain only data schemas and no behavior.";
        public static string Fix(string method) =>
            $"Move '{method}' to a standalone domain function or extension method.";
    }

    public static class DFG302
    {
        public const string Rule = "DFG302";
        public static string Message(string type, string property) =>
            $"Contract property '{property}' in '{type}' contains computed behavior; contract properties must be pure auto-properties with '{{ get; init; }}' or '{{ get; }}'.";
        public static string Fix(string property) =>
            $"Convert '{property}' to an auto-property, or move computation to a domain function.";
    }

    public static class DFG303
    {
        public const string Rule = "DFG303";
        public static string MutableSetterMessage(string property) =>
            $"Contract property '{property}' declares a mutable setter 'set;'; contract properties must be immutable ('init;' or get-only).";
        public const string MutableSetterFix = "Change 'set;' to 'init;' or remove setter.";

        public static string MutableCollectionMessage(string property, string type) =>
            $"Contract property '{property}' uses mutable collection type '{type}'; use immutable or read-only collections ('IReadOnlyList<T>', 'ImmutableArray<T>').";
        public static string MutableCollectionFix(string type) =>
            $"Replace '{type}' with 'IReadOnlyList<T>', 'IReadOnlyDictionary<K, V>', or 'ImmutableArray<T>'.";
    }

    public static class DFG304
    {
        public const string Rule = "DFG304";
        public static string Message(string type, string kind) =>
            $"Type '{type}' is declared as '{kind}'; contract types must be 'record class', 'record struct', 'enum', 'interface', or a static constants container.";
        public static string Fix(string type) =>
            $"Convert '{type}' to a record or interface, or move non-contract declarations outside the contract boundary.";
    }

    // DFG4xx - Deep Immutability & Type Safety
    public static class DFG401
    {
        public const string Rule = "DFG401";
        public static string Message(string record, string property, string type) =>
            $"Record '{record}' has property '{property}' of mutable reference type '{type}'; records must be deeply immutable.";
        public const string Fix = "Make type an immutable record or value type, or use an immutable collection.";
    }

    public static class DFG402
    {
        public const string Rule = "DFG402";
        public static string Message(string property, string type) =>
            $"Property '{property}' in domain type '{type}' declares a mutable setter 'set;'; domain properties must be immutable ('init;' or get-only).";
        public const string Fix = "Replace 'set;' with 'init;' or remove setter.";
    }

    public static class DFG403
    {
        public const string Rule = "DFG403";
        public static string Message(string symbol, string reason) =>
            $"Use of banned symbol '{symbol}': {reason}.";
        public static string Fix(string alternative) =>
            $"Use '{alternative}' instead.";
    }

    // DFG5xx - Algebraic Types & Pattern Matching
    public static class DFG501
    {
        public const string Rule = "DFG501";
        public static string Message(string type) =>
            $"Switch expression over closed union type '{type}' uses a wildcard discard '_' which masks unhandled variants; pattern matching must explicitly handle all cases.";
        public static string Fix(string type) =>
            $"Remove the wildcard discard and explicitly match all variants of '{type}'.";
    }

    public static class DFG502
    {
        public const string Rule = "DFG502";
        public static string Message(string method, string type) =>
            $"Domain method '{method}' in '{type}' is non-static or writes to state; domain operations must be pure static functions.";
        public const string Fix = "Make method static and return a new state instead of mutating fields.";
    }
}
