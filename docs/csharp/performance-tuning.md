# Vendoring and Performance Tuning Roslyn Analyzers

Status: Strategy & Architecture Guide, November 2026.
Target: `src/Diffra.Guardrails.CSharp/` and adapter implementations.

---

## 1. Executive Summary & Motivation

Many community and open-source Roslyn analyzers address aspects of clean code, exception discipline,
immutability, and resource management. However, pulling them directly into a production IDE
environment or CI gate often introduces significant pain:

1. **License Incompatibilities:** Some analyzers are licensed under copyleft licenses (e.g.,
   LGPL-3.0), which are incompatible with permissive embedding in client repositories or NuGet
   libraries.
2. **Severe Performance Cliffs:** Many third-party analyzers were written without zero-allocation
   discipline. They run heavy semantic queries (`SemanticModel.GetSymbolInfo`,
   `AnalyzeDataFlow`) on every syntax node, allocate LINQ chains inside hot analyzer callbacks,
   and cause typing stutter in Visual Studio / VS Code.
3. **Contract Drift:** Upstream analyzers format diagnostics for human reading and drift over time.
   Diffra Guardrails requires a deterministic, machine-readable public contract for `message` and
   `fix` text so autonomous agents and Diffra metrics ratchet budgets can act reliably.

Where licenses permit, Diffra Guardrails **vendors in, rewrites, and performance-tunes** proven check
logic, wrapping it in the stable DFG contract.

---

## 2. Upstream Ecosystem & License Audit

Before any analyzer logic is vendored into `Diffra.Guardrails.CSharp`, its license must be verified:

| Upstream Project | Primary License | Permissible to Vendor? | Relevant Checks & Mapping |
| --- | --- | --- | --- |
| **Microsoft.CodeAnalysis.NetAnalyzers** | **MIT** | **Yes** (Preserve notice) | `CA1024` (Avoid expensive getters) $\rightarrow$ `DFG201`; `CA1031` (No general catch) $\rightarrow$ `DFG203` |
| **Meziantou.Analyzer** | **MIT** | **Yes** (Preserve notice) | `MA0038` (Static lambda) $\rightarrow$ `DFG204`; `MA0045` (No swallow exception) $\rightarrow$ `DFG203`; `MA0016` (Collection immutability) $\rightarrow$ `DFG202` |
| **Roslynator** | **Apache-2.0** | **Yes** (Include NOTICE & state changes) | `RCS1258` (Static lambda) $\rightarrow$ `DFG204`; `RCS1218` (Switch expression) $\rightarrow$ `DFG205`; `RCS1047` (Catch handling) $\rightarrow$ `DFG203` |
| **SharpSource** | **MIT** | **Yes** (Preserve notice) | Async / synchronization and collection mutation checks $\rightarrow$ `DFG202` |
| **ErrorProne.NET** | **Apache-2.0** | **Yes** (Include NOTICE & state changes) | `ERP021` (Do not swallow exception) $\rightarrow$ `DFG203`; `ERP022` (Improper rethrow) $\rightarrow$ `DFG203` |
| **SonarAnalyzer.CSharp** | **LGPL-3.0** | **NO (Prohibited)** | Copyleft license prevents vendoring or direct compilation into MIT/permissive assemblies. **Do not vendor.** |

---

## 3. The Performance Problem in Standard Analyzers

Roslyn analyzers run during active typing in developer IDEs and on every `dotnet build`. An
unoptimized analyzer is easily the largest contributor to build degradation.

The four primary bottlenecks in unoptimized analyzers are:

1. **Unconditional Semantic Model Invocations:**
   Calling `context.SemanticModel.GetSymbolInfo(node)` costs orders of magnitude more than AST
   node walking. Unoptimized analyzers call `GetSymbolInfo` before checking simple syntax tokens
   (such as identifier names).
2. **Repeated Type Resolution:**
   Calling `Compilation.GetTypeByMetadataName("System.Exception")` inside syntax node actions
   triggers type lookups on every node rather than once per compilation.
3. **Heap Allocations in Hot Paths:**
   Using LINQ queries (`.Where().Any()`), creating lambda delegates, or allocating arrays inside
   `AnalyzeNode` triggers high GC pressure during compilation.
4. **Ignoring `CancellationToken`:**
   Failing to check `context.CancellationToken` during deep statement walks causes canceled
   compiler passes to spin uselessly on background threads.

---

## 4. The 5-Step Performance Tuning Playbook

When vendoring or writing a DFG analyzer in `Diffra.Guardrails.CSharp`, apply this exact tuning
pipeline:

### Step 1: Pre-Bind Known Types in `CompilationStartAction`

Never look up symbols in `RegisterSyntaxNodeAction`. Cache all type symbols once per compilation in
a strongly typed, read-only context object:

```csharp
// GOOD: Cached symbols per compilation, zero lookup overhead in node actions
public sealed class CompilationContext
{
    public INamedTypeSymbol? ExceptionSymbol { get; }
    public INamedTypeSymbol? CollectionInterfaceSymbol { get; }

    public CompilationContext(Compilation compilation)
    {
        ExceptionSymbol = compilation.GetTypeByMetadataName("System.Exception");
        CollectionInterfaceSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
    }
}

context.RegisterCompilationStartAction(startContext =>
{
    var cached = new CompilationContext(startContext.Compilation);
    startContext.RegisterSyntaxNodeAction(
        nodeContext => AnalyzeInvocation(nodeContext, cached),
        SyntaxKind.InvocationExpression);
});
```

### Step 2: Implement Fast Syntax Reject Guards

Always inspect node syntax before touching `SemanticModel`. If the identifier name or token shape
does not match, abort immediately:

```csharp
private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, CompilationContext cached)
{
    var invocation = (InvocationExpressionSyntax)context.Node;

    // FAST SYNTAX GUARD: Cheap string check on syntax before any symbol resolution
    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
    {
        return;
    }

    var methodName = memberAccess.Name.Identifier.ValueText;
    if (methodName != "Add" && methodName != "AddRange" && methodName != "Remove")
    {
        return; // Zero semantic overhead for 99% of invocations
    }

    // Only invoke semantic model when syntax is a proven candidate
    var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;
    // ...
}
```

### Step 3: Zero-Allocation AST Traversal

Avoid LINQ queries like `node.DescendantNodes().OfType<ThrowStatementSyntax>().Any()`. Write direct
traversals using iterative loops and check `CancellationToken`:

```csharp
// BAD: Allocates IEnumerable, enumerator, and delegate closure
bool hasThrow = block.DescendantNodes().OfType<ThrowStatementSyntax>().Any();

// GOOD: Zero-allocation iterative traversal with cancellation check
private static bool HasRethrow(BlockSyntax block, CancellationToken cancellationToken)
{
    foreach (var statement in block.Statements)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (statement is ThrowStatementSyntax { Expression: null })
        {
            return true;
        }
    }
    return false;
}
```

### Step 4: Early Exit on Empty Captures for DFG204

For `DFG204` (Zero-Allocation Closures), avoid full semantic analysis if the syntax indicates
obvious outer state access:

```csharp
private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
{
    var lambda = (LambdaExpressionSyntax)context.Node;
    if (lambda.Modifiers.Any(SyntaxKind.StaticKeyword))
    {
        return; // Already static, instant reject
    }

    // Pass cancellation token to semantic flow analysis
    var dataFlow = context.SemanticModel.AnalyzeDataFlow(lambda);
    if (!dataFlow.Succeeded)
    {
        return;
    }

    // If Captured is empty and no instance references are captured, suggest static
    if (dataFlow.Captured.IsEmpty)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DFG204,
            lambda.GetLocation()));
    }
}
```

### Step 5: Wrap in the Stable Public Contract

Upstream diagnostics use varying diagnostic IDs (`CA1024`, `MA0038`, `RCS1258`) and open-ended
messages. When vendoring check logic:

1. Map the diagnostic strictly to the canonical `DFGxxx` ID.
2. Substitute parameters into the exact `RuleContracts` template.
3. Emit actionable remediation text with exact syntax hints.

---

## 5. Performance Benchmarking & Validation

Every vendored or optimized analyzer must be benchmarked against target repositories and synthetic
workloads using:

1. **Roslyn Analyzer Profiling:**
   Run `dotnet build /p:ReportAnalyzer=true` to track the exact execution time and allocation budget
   of each `DiagnosticAnalyzer`.
2. **BenchmarkDotNet Execution Time:**
   Ensure per-file analysis time stays under **$5\text{ms}$** on standard developer hardware,
   allowing sub-second execution across changed PR files in the `diffra-guardrails` CLI tool.
