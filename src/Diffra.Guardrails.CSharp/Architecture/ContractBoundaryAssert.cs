using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Diffra.Guardrails.Core.Catalogue;

namespace Diffra.Guardrails.CSharp.Architecture;

/// <summary>
/// Architecture test assertions for verifying contract purity boundaries at test-time.
/// Complements the compile-time Roslyn analyzer (DFG3xx) for teams using test suites
/// or NetArchTest / ArchUnitNET architectural validation.
/// </summary>
public static class ContractBoundaryAssert
{
    private static readonly HashSet<string> StandardRecordMethods = new(StringComparer.Ordinal)
    {
        "<Clone>$", "PrintMembers", "Equals", "GetHashCode", "ToString", "Deconstruct"
    };

    private static readonly HashSet<string> MutableCollectionGenericTypeNames = new(StringComparer.Ordinal)
    {
        "List`1", "IList`1", "Dictionary`2", "IDictionary`2", "HashSet`1", "ISet`1", "Collection`1", "ICollection`1"
    };

    /// <summary>
    /// Asserts that all public and internal types in the specified assembly satisfy DFG3xx contract purity.
    /// </summary>
    public static void AssertAssembly(Assembly assembly, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var types = assembly.GetTypes()
            .Where(t => !t.IsNestedPrivate && !IsCompilerGenerated(t))
            .Where(t => filter == null || filter(t))
            .ToList();

        AssertTypes(types);
    }

    /// <summary>
    /// Asserts that all types in the given sequence satisfy DFG3xx contract purity.
    /// </summary>
    public static void AssertTypes(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        var violations = new List<string>();

        foreach (var type in types)
        {
            if (IsCompilerGenerated(type)) continue;

            // DFG304: Permitted declaration kinds
            var isRecord = IsRecord(type);
            var isInterface = type.IsInterface;
            var isEnum = type.IsEnum;
            var isStaticClass = type.IsAbstract && type.IsSealed;

            if (!isRecord && !isInterface && !isEnum)
            {
                if (isStaticClass)
                {
                    // Must only contain const fields
                    var nonConstFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                        .Where(f => !f.IsLiteral && !f.IsInitOnly);
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                    if (nonConstFields.Any() || methods.Any())
                    {
                        violations.Add($"[DFG304] Static class '{type.FullName}' contains non-constant members; static contract containers must contain only const fields.");
                    }
                }
                else
                {
                    violations.Add($"[DFG304] Type '{type.FullName}' is declared as {(type.IsValueType ? "struct" : "class")}; pure contract types must be records, enums, interfaces, or const containers.");
                }
            }

            // DFG301: Pure contract types must not declare methods
            var declaredMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && !StandardRecordMethods.Contains(m.Name) && !IsCompilerGenerated(m))
                .ToList();

            foreach (var method in declaredMethods)
            {
                violations.Add($"[DFG301] Contract type '{type.FullName}' declares method '{method.Name}'; pure contracts must define only data schemas.");
            }

            // DFG303: Properties must not have mutable setters or mutable collections
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var prop in properties)
            {
                var setMethod = prop.SetMethod;
                if (setMethod != null)
                {
                    // Check if setter is 'init' (IsExternalInit)
                    var isInitOnly = setMethod.ReturnParameter.GetRequiredCustomModifiers()
                        .Any(mod => mod.FullName == typeof(IsExternalInit).FullName);

                    if (!isInitOnly)
                    {
                        violations.Add($"[DFG303] Property '{prop.Name}' in '{type.FullName}' has a mutable setter; contract properties must be 'init;' or get-only.");
                    }
                }

                // Check for mutable collections
                if (IsMutableCollection(prop.PropertyType))
                {
                    violations.Add($"[DFG303] Property '{prop.Name}' in '{type.FullName}' uses mutable collection type '{prop.PropertyType.Name}'; use IReadOnlyList or ImmutableArray.");
                }
            }
        }

        if (violations.Count > 0)
        {
            throw new ContractPurityViolationException(violations);
        }
    }

    private static bool IsRecord(Type type)
    {
        // Record classes synthesize <Clone>$
        if (type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
        {
            return true;
        }

        // Both record classes and record structs synthesize PrintMembers
        if (type.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) != null)
        {
            return true;
        }

        return false;
    }

    private static bool IsCompilerGenerated(MemberInfo member) =>
        member.GetCustomAttribute<CompilerGeneratedAttribute>() != null || member.Name.StartsWith('<');

    private static bool IsMutableCollection(Type type)
    {
        if (type.IsGenericType)
        {
            var genericName = type.GetGenericTypeDefinition().Name;
            if (MutableCollectionGenericTypeNames.Contains(genericName))
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class ContractPurityViolationException : Exception
{
    public IReadOnlyList<string> Violations { get; }

    public ContractPurityViolationException(IReadOnlyList<string> violations)
        : base($"Contract purity boundaries violated ({violations.Count} errors):\n" + string.Join("\n", violations))
    {
        Violations = violations;
    }
}
