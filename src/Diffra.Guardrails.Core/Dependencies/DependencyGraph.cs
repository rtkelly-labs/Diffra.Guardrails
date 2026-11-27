namespace Diffra.Guardrails.Core.Dependencies;

/// <summary>A resolved package: one version per graph, with the line it appears on in the graph file.</summary>
public sealed record ResolvedPackage(string Id, string Version, int Line)
{
    public string Display => $"{Id} {Version}";
}

/// <summary>
/// One resolved dependency graph: a restore target in <c>project.assets.json</c>, a group in
/// <c>paket.lock</c>, or a tool manifest. Package IDs compare case-insensitively, as NuGet's do.
/// </summary>
public sealed class DependencyGraph(string path)
{
    private readonly Dictionary<string, ResolvedPackage> _packages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _edges = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _roots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The graph file, relative to the checked root, with forward slashes.</summary>
    public string Path { get; } = path;

    /// <summary>False when the direct dependencies are unknown (a paket.lock with no paket.dependencies).</summary>
    public bool RootsKnown { get; set; } = true;

    public IEnumerable<ResolvedPackage> Packages => _packages.Values;

    public void Add(ResolvedPackage package) => _packages.TryAdd(package.Id, package);

    public void AddEdge(string from, string to)
    {
        if (!_edges.TryGetValue(from, out var targets))
        {
            _edges[from] = targets = [];
        }

        targets.Add(to);
    }

    public void AddRoot(string id) => _roots.Add(id);

    /// <summary>
    /// The shortest chain from a direct dependency to <paramref name="id"/>, as "Id Version"
    /// entries ending with the package itself. Empty when the direct dependencies are unknown.
    /// </summary>
    public IReadOnlyList<string> PathTo(string id)
    {
        if (!RootsKnown)
        {
            return [];
        }

        var parent = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        foreach (var root in _roots.Where(_packages.ContainsKey).Order(StringComparer.OrdinalIgnoreCase))
        {
            parent[root] = null;
            queue.Enqueue(root);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (string.Equals(current, id, StringComparison.OrdinalIgnoreCase))
            {
                var chain = new List<string>();
                for (string? at = current; at is not null; at = parent[at])
                {
                    chain.Add(_packages[at].Display);
                }

                chain.Reverse();
                return chain;
            }

            foreach (var next in _edges.GetValueOrDefault(current, []))
            {
                if (_packages.ContainsKey(next) && parent.TryAdd(next, current))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return [_packages[id].Display];
    }
}
