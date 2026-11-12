# Functional C# Guidance and Idioms

This document defines the functional programming conventions enforced by Diffra Guardrails
through the **DFG2xx** rule family. It provides guidelines and idioms for human engineers and AI
agents targeting modern .NET (C# 10, 11, 12, and 13+).

---

## 1. Motivation: Why Functional Guardrails in C#?

Modern C# has evolved from an imperative, object-oriented language into a multi-paradigm language
rich in functional primitives: records, init-only properties, pattern matching, switch expressions,
collection expressions, immutability guarantees, and static lambdas.

However, code written by Large Language Models (LLMs) and procedural habit frequently reverts to
antipatterns:

- Stifling performance and determinism with hidden allocations and mutations.
- Stuffing complex logic, LINQ queries, and collection allocations inside property getters.
- Mutating collections in place with `Add`, `AddRange`, and `Remove`, causing temporal coupling and
  concurrency bugs.
- Swallowing exceptions with broad `catch (Exception)` blocks, masking critical failures.
- Capturing outer scopes inadvertently in closures, triggering heap allocations.
- Reassigning mutable local variables across procedural `if`/`else` branches instead of using
  single-assignment expressions.

The **DFG2xx** family eliminates these failure modes at authoring time.

---

## 2. Rule Overview: The DFG2xx Family

| Rule ID | Name | Target Construct | Prevents |
| --- | --- | --- | --- |
| **DFG201** | Segregation of Data and Behavior | Property Getters | O(N) hidden computations, LINQ queries, and allocations in accessors |
| **DFG202** | Immutable Collections & Transformations | Collection Mutations | In-place mutations (`Add`, `Remove`, `Clear`); temporal coupling |
| **DFG203** | Defensive Exception Discipline | `catch` Clauses | Catch-all swallowing, loss of stack traces, and unhandled failure states |
| **DFG204** | Zero-Allocation Closures | Lambdas & Local Functions | Unnecessary heap allocations for delegates and closures |
| **DFG205** | Single Assignment & Expression Flow | Local Variable Reassignment | Procedural mutable state; encourages switch and ternary expressions |

---

## 3. DFG201: Segregation of Data and Behavior (No Behavior in Property Getters)

### Principle

A property represents state or a lightweight accessor ($O(1)$ complexity, zero side effects, no
observable allocation). A method represents behavior, computation, or transformation.

When a property getter executes LINQ queries, allocates collections, or runs loops, callers (and
debuggers, serializers, and ORMs) are misled into treating an expensive operation as a cheap field
read.

### Anti-Patterns Flagged

```csharp
// BAD: LINQ query and collection allocation inside property getter
public class Order
{
    public List<LineItem> Items { get; init; } = [];

    // Violates DFG201: LINQ invocation and new collection allocation on every get
    public List<LineItem> ActiveItems => Items.Where(i => i.IsActive).ToList();

    // Violates DFG201: Control flow and complex computation in getter
    public decimal TotalAmount
    {
        get
        {
            decimal sum = 0;
            foreach (var item in Items)
            {
                sum += item.Price;
            }
            return sum;
        }
    }
}
```

### Functional Idioms

1. **Convert to a pure method:**

   ```csharp
   public class Order
   {
       public ImmutableArray<LineItem> Items { get; init; } = [];

       // GOOD: Method communicates computation and potential allocation
       public ImmutableArray<LineItem> GetActiveItems() =>
           Items.Where(static i => i.IsActive).ToImmutableArray();

       // GOOD: Method communicates calculation
       public decimal CalculateTotal() =>
           Items.Sum(static i => i.Price);
   }
   ```

2. **Precompute into an immutable, init-only property:**

   ```csharp
   // GOOD: Precomputed at creation; getter is an O(1) field read
   public record Order(
       ImmutableArray<LineItem> Items,
       ImmutableArray<LineItem> ActiveItems,
       decimal TotalAmount);
   ```

3. **Deferred evaluation via `Lazy<T>` for heavy singletons:**

   ```csharp
   private readonly Lazy<ExpensiveReport> _report;
   public ExpensiveReport Report => _report.Value; // O(1) after initialization
   ```

---

## 4. DFG202: Immutability & Functional Transformations

### Principle

Do not mutate collections in place. Modifying existing collections creates temporal coupling,
invalidates callers' assumptions, and causes race conditions in multithreaded environments.

Instead, treat collections as immutable data streams. Produce new collections using modern C#
collection expressions (`[.. a, .. b]`), pure LINQ transformations, or persistent immutable types
(`ImmutableArray<T>`, `FrozenSet<T>`).

### Anti-Patterns Flagged

```csharp
// BAD: In-place mutation with Add, AddRange, Remove
public List<string> AppendTags(List<string> existingTags, string newTag)
{
    existingTags.Add(newTag); // Violates DFG202
    return existingTags;
}

public void UpdateBasket(List<Item> basket, IEnumerable<Item> additions)
{
    basket.AddRange(additions); // Violates DFG202
}
```

### Functional Idioms

1. **Modern collection expressions (`[.. x, .. y]`):**

   ```csharp
   // GOOD: Pure transformation creating a new collection
   public ReadOnlySpan<string> AppendTag(ReadOnlySpan<string> existing, string newTag) =>
       [.. existing, newTag];

   public ImmutableArray<string> AppendTag(ImmutableArray<string> existing, string newTag) =>
       [.. existing, newTag];
   ```

2. **LINQ `Append`, `Concat`, and `SelectMany`:**

   ```csharp
   // GOOD: Pure composition
   public IEnumerable<Item> CombineItems(IEnumerable<Item> first, IEnumerable<Item> second) =>
       first.Concat(second);
   ```

3. **Persistent Immutable Collections:**

   ```csharp
   using System.Collections.Immutable;

   // GOOD: Immutable data structures provide safe structural sharing
   public ImmutableArray<string> AddItem(ImmutableArray<string> current, string item) =>
       current.Add(item); // Returns a new ImmutableArray, does not mutate in place
   ```

---

## 5. DFG203: Defensive Exception Discipline

### Principle

Broad catch-all blocks (`catch (Exception)`, `catch (System.Object)`, or bare `catch {}`) swallow
unexpected faults, obscure root causes, and mask critical bugs such as `NullReferenceException`,
`OutOfMemoryException`, or assertion failures.

A catch block must either:

- Target a specific, expected exception type.
- Use an exception filter (`when (...)`) to inspect conditions.
- Rethrow the caught exception using bare `throw;` to preserve the stack trace.

### Anti-Patterns Flagged

```csharp
// BAD: Swallowing all exceptions
try
{
    ProcessPayment(order);
}
catch (Exception) // Violates DFG203
{
    // Empty or logging-only without rethrowing
    logger.LogWarning("Payment failed");
}

// BAD: Catching general exception without filter
try
{
    FetchConfiguration();
}
catch (Exception ex) // Violates DFG203
{
    throw new InvalidOperationException("Failed", ex); // Lost context if not typed
}
```

### Functional Idioms

1. **Specific catch types:**

   ```csharp
   try
   {
       await httpClient.GetAsync(url);
   }
   catch (HttpRequestException ex) // GOOD: Specific domain exception
   {
       logger.LogError(ex, "Network request failed");
       throw;
   }
   ```

2. **Exception filters (`when` clauses):**

   ```csharp
   try
   {
       await service.ExecuteAsync();
   }
   catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) // GOOD: Filtered catch
   {
       return Option<Data>.None;
   }
   ```

3. **Railway-Oriented Programming (Result / OneOf types):**

   Instead of using exceptions for control flow, return an explicit `Result<T, TError>`:

   ```csharp
   public Result<Order, PaymentError> ProcessOrder(Order order) =>
       Validate(order)
           .Bind(AuthorizePayment)
           .Map(FinalizeOrder);
   ```

---

## 6. DFG204: Zero-Allocation Closures (Static Lambdas & Local Functions)

### Principle

Lambdas and local functions that do not capture outer variables or `this` should always be marked
with the `static` modifier.

In C#, non-static lambdas may cause delegate allocation or closure class allocations on every
invocation if the compiler cannot cache them. Marking them `static` guarantees:

1. Zero closure allocation ($0$ bytes on the heap).
2. Compilation failure if an author accidentally captures mutable outer state or enclosing `this`.

### Anti-Patterns Flagged

```csharp
public class InventoryService
{
    public IEnumerable<Product> FilterInStock(IEnumerable<Product> products)
    {
        // Violates DFG204: Does not capture outer variables, but missing 'static'
        return products.Where(p => p.Quantity > 0);
    }

    public int ComputeScore(int[] values)
    {
        // Violates DFG204: Local function captures nothing, missing 'static'
        int DoubleValue(int x) => x * 2;
        return values.Select(DoubleValue).Sum();
    }
}
```

### Functional Idioms

1. **`static` lambdas in LINQ:**

   ```csharp
   public class InventoryService
   {
       public IEnumerable<Product> FilterInStock(IEnumerable<Product> products) =>
           products.Where(static p => p.Quantity > 0); // GOOD: static lambda guaranteed zero-allocation
   }
   ```

2. **`static` local functions with explicit parameters:**

   ```csharp
   public int ComputeTotal(int basePrice, int discountRate)
   {
       // GOOD: Explicit inputs, static keyword prevents accidental capture
       static int ApplyDiscount(int price, int rate) => price - (price * rate / 100);

       return ApplyDiscount(basePrice, discountRate);
   }
   ```

---

## 7. DFG205: Single Assignment & Expression-Oriented Flow

### Principle

Favor single-assignment variables (`readonly`, `const`, or initialized immediately at declaration)
over mutable variables whose values are assigned or modified inside `if`/`else` branches.

Expression-oriented code (ternary expressions, `switch` expressions, and pattern matching) makes
uninitialized state impossible, ensures compiler exhaustiveness checks, and eliminates procedural
branching bugs.

### Anti-Patterns Flagged

```csharp
// BAD: Declaring mutable variable and reassigning in conditional branches
string status;
if (order.IsDelivered)
{
    status = "Delivered";
}
else if (order.IsShipped)
{
    status = "In Transit";
}
else
{
    status = "Pending";
}
```

### Functional Idioms

1. **`switch` expressions with pattern matching:**

   ```csharp
   // GOOD: Single assignment, exhaustive evaluation, expression-oriented
   var status = order switch
   {
       { IsDelivered: true } => "Delivered",
       { IsShipped: true }   => "In Transit",
       _                     => "Pending"
   };
   ```

2. **Ternary expressions for binary choices:**

   ```csharp
   // GOOD: Single assignment via ternary
   var label = count == 1 ? "item" : "items";
   ```

3. **Early return in methods:**

   ```csharp
   // GOOD: Return immediately rather than mutating an accumulator
   public string FormatGreeting(User? user)
   {
       if (user is null)
       {
           return "Welcome, Guest!";
       }

       return $"Welcome, {user.Name}!";
   }
   ```

---

## 8. Summary of Diagnostics & Fix Actions

| Rule | Finding Diagnostic | Actionable Fix Contract |
| --- | --- | --- |
| **DFG201** | Property getter '{property}' contains non-trivial behavior ({violation}); property getters must be lightweight, allocation-free accessors. | Convert '{property}' to a method, or precompute and store the value in an init-only property or field. |
| **DFG202** | In-place mutation '{method}' detected on collection '{target}'; steer toward immutable collections and pure transformations. | Replace '{method}' with a functional transformation, collection expression (e.g., '[.. {target}, item]'), or immutable collection. |
| **DFG203** | Catch block catches broad '{type}' without an exception filter or rethrow; catch-all exception swallowing is disallowed. | Catch a specific exception type, add an exception filter ('when (...)'), or rethrow with 'throw;' to preserve diagnostic flow. |
| **DFG204** | Lambda or local function does not capture outer variables and should be declared 'static' to guarantee zero closure allocation. | Add the 'static' modifier to the lambda or local function. |
| **DFG205** | Variable '{variable}' is declared and subsequently reassigned across conditional branches; prefer single assignment via a switch expression, ternary expression, or early return. | Initialize '{variable}' directly using a switch expression, ternary expression, or return early. |
