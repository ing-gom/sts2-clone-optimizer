using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Sts2CloneOptimizer;

/// <summary>
/// STS2's ModManager loads only <c>{ModId}.dll</c> via <see cref="AssemblyLoadContext.LoadFromAssemblyPath"/>
/// — the mod folder is NOT on the runtime's probing paths, so a bundled sibling DLL
/// (<c>Sts2.ModKit.dll</c>) is never found by default and the JIT throws
/// <c>FileNotFoundException</c> the moment Initialize() references a ModKit type.
///
/// Workaround: register an <see cref="AssemblyLoadContext.Resolving"/> handler that looks
/// next to our own DLL for the missing assembly. <c>[ModuleInitializer]</c> runs this at
/// assembly-load time — before any of our methods JIT — so Initialize() is safe.
/// </summary>
internal static class AssemblyResolverBootstrap
{
    private static bool _registered;

    [ModuleInitializer]
    internal static void Register()
    {
        if (_registered) return;
        _registered = true;

        var self = typeof(AssemblyResolverBootstrap).Assembly;
        var ctx = AssemblyLoadContext.GetLoadContext(self);
        if (ctx is null) return;

        var modDir = Path.GetDirectoryName(self.Location);
        if (string.IsNullOrEmpty(modDir)) return;

        ctx.Resolving += (loadContext, name) =>
        {
            if (string.IsNullOrEmpty(name.Name)) return null;
            var candidate = Path.Combine(modDir, name.Name + ".dll");
            return File.Exists(candidate) ? loadContext.LoadFromAssemblyPath(candidate) : null;
        };
    }
}
