using MegaCrit.Sts2.Core.Modding;
using Sts2.ModKit.Bootstrap;

namespace Sts2CloneOptimizer;

[ModInitializer(nameof(Initialize))]
public class MainFile
{
    public const string ModId = "Sts2CloneOptimizer";

    public static readonly MegaCrit.Sts2.Core.Logging.Logger Logger
        = ModBootstrap.CreateLogger(ModId);

    // Patches-only mod: ModBootstrap.Run applies the Harmony patches in this assembly.
    public static void Initialize() =>
        ModBootstrap.Run(ModId, Logger, typeof(MainFile).Assembly);
}
