# C# Roslyn Analyzer Architecture Plan: Diffra.Guardrails.CSharp

Status: Architecture Plan, November 2026.
Target Project: `src/Diffra.Guardrails.CSharp/`

---

## 1. Executive Summary

`Diffra.Guardrails.CSharp` implements the Roslyn analyzers and code fixes for Diffra Guardrails. It
operates in two environments:

1. **In-IDE / In-Build Analyzer:** Packaged as an analyzer NuGet package (`Diffra.Guardrails`) that runs
   live inside Visual Studio, VS Code (via C# Dev Kit / OmniSharp), and `dotnet build`, producing
   standard Roslyn diagnostics with edit-time code fix providers.
2. **Command-Line & CI Pipeline:** Hosted by `src/Diffra.Guardrails.Tool/` for sub-second PR checks,
   producing finding JSON, agent-formatted remediation advice, and Diffra metrics payloads.

This document plans the implementation of the **DFG2xx** rule family (Functional Architecture &
Clean Code) inside `Diffra.Guardrails.CSharp`.

---

## 2. Project Structure & Dependencies

```text
Diffra.Guardrails.slnx
├── src/
│   ├── Diffra.Guardrails.Core/          # Finding model, RuleCatalogue, DiffraPayloadWriter
│   ├── Diffra.Guardrails.CSharp/        # Roslyn DiagnosticAnalyzers & CodeFixProviders
│   │   ├── Analyzers/
│   │   │   ├── DFG201PropertyGetterBehaviorAnalyzer.cs
│   │   │   ├── DFG202CollectionMutationAnalyzer.cs
│   │   │   ├── DFG203DefensiveExceptionAnalyzer.cs
│   │   │   ├── DFG204ZeroAllocationClosureAnalyzer.cs
│   │   │   └── DFG205SingleAssignmentAnalyzer.cs
│   │   ├── CodeFixes/
│   │   │   ├── DFG201ConvertPropertyToMethodCodeFixProvider.cs
│   │   │   ├── DFG202FunctionalCollectionExpressionCodeFixProvider.cs
│   │   │   ├── DFG203AddFilterOrRethrowCodeFixProvider.cs
│   │   │   ├── DFG204AddStaticModifierCodeFixProvider.cs
│   │   │   └── DFG205ConvertToSwitchOrTernaryCodeFixProvider.cs
│   │   ├── Diagnostics/
│   │   │   ├── DiagnosticDescriptors.cs
│   │   │   └── RuleContracts.cs
│   │   └── Diffra.Guardrails.CSharp.csproj
│   └── Diffra.Guardrails.Tool/          # CLI check and payload commands
└── tests/
    └── Diffra.Guardrails.CSharp.Tests/  # Analyzer and CodeFix verifier tests
```

### Framework & Target Dependencies

- **TargetFramework:** `netstandard2.0` (required for Roslyn analyzer packages to run seamlessly
  inside Visual Studio and msbuild) with `LangVersion: 13.0` and nullable enabled.
- **Dependencies:**
  - `Microsoft.CodeAnalysis.CSharp` (version `4.8.0` or newer for C# 12/13 feature support:
    collection expressions, primary constructors).
  - `Microsoft.CodeAnalysis.CSharp.Workspaces` (for CodeFixProviders).
- **Test Dependencies:**
  - `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit`.
  - `Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit`.

---

## 3. Analyzer Design & Implementation Details

Each analyzer inherits from `DiagnosticAnalyzer` and overrides `Initialize(AnalysisContext context)`.

### 3.1 DFG201: No Behavior in Property Getters

- **Target Nodes:** `SyntaxKind.PropertyDeclaration`.
- **Inspection Logic:**
  1. Inspect the property's accessors: expression-body `ArrowExpressionClauseSyntax` or
     `GetAccessorDeclarationSyntax`.
  2. Traverse descendant nodes within the getter body:
     - Check for `InvocationExpressionSyntax`: use semantic model to inspect if the invoked method
       belongs to `System.Linq.Enumerable` or returns `IEnumerable<T>`.
     - Check for `QueryExpressionSyntax` (LINQ query comprehension).
     - Check for collection creation: `CollectionExpressionSyntax` (`[...]`),
       `ArrayCreationExpressionSyntax`, `ImplicitArrayCreationExpressionSyntax`, or
       `ObjectCreationExpressionSyntax` where the instantiated type implements `IEnumerable`.
     - Check for control flow statements: `IfStatementSyntax`, `SwitchStatementSyntax`, loops, or
       `TryStatementSyntax`.
  3. Report diagnostic on the property identifier or offending expression.

### 3.2 DFG202: Immutable Collections & Pure Transformations

- **Target Nodes:** `SyntaxKind.InvocationExpression`.
- **Inspection Logic:**
  1. Check member access method name against mutation blacklist:
     `Add`, `AddRange`, `Remove`, `RemoveAll`, `RemoveAt`, `Clear`, `Insert`, `InsertRange`,
     `Reverse`, `Sort`.
  2. Use `SemanticModel.GetSymbolInfo(invocation)` to resolve the method symbol.
  3. Inspect the containing type of the method: does it implement `System.Collections.ICollection`,
     `System.Collections.Generic.ICollection<T>`, `System.Collections.Generic.IList<T>`, or
     `System.Collections.Generic.IDictionary<TKey, TValue>`?
  4. Ensure the receiver is not an immutable builder (e.g., `ImmutableArray<T>.Builder`) or
     explicitly exempted.
  5. Report diagnostic on the invocation node.

### 3.3 DFG203: Defensive Exception Discipline

- **Target Nodes:** `SyntaxKind.CatchClause`.
- **Inspection Logic:**
  1. If `catchClause.Declaration` is null (bare `catch { ... }`), report violation immediately.
  2. If `catchClause.Declaration.Type` resolves to `System.Exception` or `System.Object`:
     - Check if `catchClause.Filter` (`when (...)`) is present. If present, allow.
     - Check the statements in `catchClause.Block`: inspect all control flow exit paths. If any exit
       path exits without a bare `ThrowStatementSyntax` (`throw;`), report diagnostic.
  3. Report diagnostic on the catch keyword and declaration.

### 3.4 DFG204: Zero-Allocation Closures

- **Target Nodes:** `SyntaxKind.SimpleLambdaExpression`, `SyntaxKind.ParenthesizedLambdaExpression`,
  `SyntaxKind.LocalFunctionStatement`.
- **Inspection Logic:**
  1. If node contains `SyntaxKind.StaticKeyword` in its modifiers, skip.
  2. Call `context.SemanticModel.AnalyzeDataFlow(node)`.
  3. Check `DataFlowAnalysis.Captured`:
     - If `Captured` is empty AND no member access resolves to an instance member of the enclosing
       `this`, the closure is pure and captures nothing.
  4. Report diagnostic recommending the `static` modifier.

### 3.5 DFG205: Single Assignment & Expression-Oriented Flow

- **Target Nodes:** `SyntaxKind.LocalDeclarationStatement`.
- **Inspection Logic:**
  1. Variable declaration has no initializer or is initialized to a default literal (`null`,
     `default`, `0`, `""`).
  2. Inspect the next statement in the enclosing `BlockSyntax`:
     - Must be an `IfStatementSyntax`.
     - The `IfStatementSyntax` must have an `ElseClauseSyntax`.
     - All execution branches must contain a direct assignment (`SimpleAssignmentExpression`) to the
       declared variable.
     - No intervening statements exist between the declaration and the conditional block.
  3. Report diagnostic recommending refactoring to a switch expression, ternary expression, or
     early return.

---

## 4. Code Fix Providers

All code fixes inherit from `CodeFixProvider` and export `ExportCodeFixProvider(LanguageNames.CSharp)`.

| Rule | Code Fix Provider | Transformation Performed |
| --- | --- | --- |
| **DFG201** | `DFG201ConvertPropertyToMethodCodeFixProvider` | Transforms property `T Prop => expr;` into `T GetProp() => expr;` |
| **DFG202** | `DFG202FunctionalCollectionExpressionCodeFixProvider` | Replaces `list.Add(x)` with collection expression assignment `list = [.. list, x]` |
| **DFG203** | `DFG203AddFilterOrRethrowCodeFixProvider` | Inserts `throw;` into the catch block or adds a `when (...)` clause |
| **DFG204** | `DFG204AddStaticModifierCodeFixProvider` | Inserts the `static` token into the lambda or local function declaration |
| **DFG205** | `DFG205ConvertToSwitchOrTernaryCodeFixProvider` | Collapses declaration and if/else into `var x = condition ? a : b;` or `switch` |

---

## 5. Diagnostic Descriptors & Public Contracts

Centralized in `RuleContracts.cs`:

```csharp
public static class RuleContracts
{
    public static class DFG201
    {
        public const string Id = "DFG201";
        public const string Title = "No Behavior in Property Getters";
        public const string MessageFormat = "Property getter '{0}' contains non-trivial behavior ({1}); property getters must be lightweight, allocation-free accessors.";
        public const string Fix = "Convert '{0}' to a method, or precompute and store the value in an init-only property or field.";
    }

    public static class DFG202
    {
        public const string Id = "DFG202";
        public const string Title = "Immutable Collections & Pure Transformations";
        public const string MessageFormat = "In-place mutation '{0}' detected on collection '{1}'; steer toward immutable collections and pure transformations.";
        public const string Fix = "Replace '{0}' with a functional transformation, collection expression (e.g., '[.. {1}, item]'), or immutable collection.";
    }

    public static class DFG203
    {
        public const string Id = "DFG203";
        public const string Title = "Defensive Exception Discipline";
        public const string MessageFormat = "Catch block catches broad '{0}' without an exception filter or rethrow; catch-all exception swallowing is disallowed.";
        public const string Fix = "Catch a specific exception type, add an exception filter ('when (...)'), or rethrow with 'throw;' to preserve diagnostic flow.";
    }

    public static class DFG204
    {
        public const string Id = "DFG204";
        public const string Title = "Zero-Allocation Closures";
        public const string MessageFormat = "Lambda or local function does not capture outer variables and should be declared 'static' to guarantee zero closure allocation.";
        public const string Fix = "Add the 'static' modifier to the lambda or local function.";
    }

    public static class DFG205
    {
        public const string Id = "DFG205";
        public const string Title = "Single Assignment & Expression Flow";
        public const string MessageFormat = "Variable '{0}' is declared and subsequently reassigned across conditional branches; prefer single assignment via a switch expression, ternary expression, or early return.";
        public const string Fix = "Initialize '{0}' directly using a switch expression, ternary expression, or return early.";
    }
}
```

---

## 6. Verification & Test Strategy

Testing uses the official Roslyn test harness:

1. **Unit Verification (`CSharpAnalyzerVerifier`):**
   - Negative cases: Clean code that does not trigger false positives (e.g., trivial getters, static
     lambdas, pure expressions).
   - Positive cases: Code with violations, verifying exact diagnostic ID, span (line/column), and
     formatted message.
2. **Code Fix Verification (`CSharpCodeFixVerifier`):**
   - Verifies that applying the code fix produces the exact expected refactored source text.
3. **Conformance Corpus Integration:**
   - The test suite executes each analyzer against the C# files in `conformance/cases/` and asserts
     that the reported findings match `expected.json` byte-for-byte.

---

## 7. Multi-Language Boundaries, Framework & Language Version Gating

### 7.1 Cross-Language Boundaries

Roslyn DiagnosticAnalyzers are inherently CLR-language specific. In Roslyn:

- An analyzer declares supported languages via the `[DiagnosticAnalyzer(LanguageNames.CSharp, ...)]` attribute.
- Roslyn cannot execute across F#, TypeScript, or Python.
- **Diffra Guardrails Architecture:** Native linters and AST visitors run in each ecosystem (Roslyn for C#, `FSharp.Analyzers.SDK` for F#, ESLint for TypeScript, AST linters for Python), while the Diffra CLI tool (`diffra-guardrails check`) normalizes all diagnostics into the unified `DFG` JSON schema.

### 7.2 Framework Version Gating

An analyzer can gate its execution based on the compilation's target framework using three distinct patterns:

1. **Symbol / Type Probe in `CompilationStartAction` (Recommended):**
   Probe for types that were introduced in specific framework releases:

   ```csharp
   context.RegisterCompilationStartAction(compilationContext =>
   {
       // Gate on .NET 8+ by verifying TimeProvider exists in metadata
       var timeProviderSymbol = compilationContext.Compilation.GetTypeByMetadataName("System.TimeProvider");
       if (timeProviderSymbol is null)
       {
           return; // Early exit: do not register DFG403 TimeProvider checks on older frameworks
       }

       compilationContext.RegisterSyntaxNodeAction(...);
   });
   ```

2. **Assembly TargetFrameworkAttribute Inspection:**
   Read the `TargetFrameworkAttribute` emitted by the compiler into the target assembly:

   ```csharp
   var targetFramework = compilationContext.Compilation.Assembly.GetAttributes()
       .FirstOrDefault(a => a.AttributeClass?.Name == "TargetFrameworkAttribute")
       ?.ConstructorArguments.FirstOrDefault().Value?.ToString();
   // e.g. ".NETCoreApp,Version=v11.0"
   ```

3. **MSBuild Package Gating:**
   In the analyzer's NuGet `.targets` file, conditionally disable specific diagnostics on older target frameworks:

   ```xml
   <ItemGroup Condition="'$([MSBuild]::VersionLessThan($(TargetFrameworkVersion), 11.0))' == 'true'">
     <DisabledAnalyzers Include="DFG501" />
   </ItemGroup>
   ```

### 7.3 Language Version Gating

In .NET, **Target Framework Version and Language Version are decoupled**:

- A project targeting `.NET 10` may set `<LangVersion>14</LangVersion>` or `<LangVersion>preview</LangVersion>`.
- A project targeting `.NET 11` may pin `<LangVersion>11</LangVersion>`.

To prevent invalid diagnostics or generating code fixes that fail to compile, analyzers inspect `CSharpParseOptions.LanguageVersion`:

```csharp
private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
{
    var parseOptions = (CSharpParseOptions)context.Node.SyntaxTree.Options;
    if (parseOptions.LanguageVersion < LanguageVersion.CSharp14)
    {
        return; // Do not enforce C# 14 union syntax rules on projects running C# 13 or lower
    }

    // Proceed with language-version specific analysis
}
```
