using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Sts2CloneOptimizer.Localization;

namespace Sts2CloneOptimizer.Patches;

/// <summary>
/// Optimizes the rest-site "Clone" option (granted by the Pael's Growth relic).
///
/// Vanilla <see cref="CloneRestSiteOption.OnSelect"/> clones every Clone-enchanted deck card
/// with a per-card <c>await CardPileCmd.Add(card, Deck)</c> (fly visuals on) and then a
/// <c>PreviewCardPileAdd</c> over ALL clones (one NCard node each). With many Clone-enchanted
/// cards — and the count doubles every rest — that is N sequential pipeline adds plus an
/// N-node preview, all in one frame: the game freezes.
///
/// Above <see cref="FastPathThreshold"/> we clone the same cards (identical gameplay result)
/// but: batch the add with skipVisuals, preview only a small sample, chunk the work and yield a
/// frame between chunks (so the game stays responsive), and show a top-screen progress bar.
/// Below the threshold the original runs unchanged (its nice per-card animation is kept).
/// </summary>
[HarmonyPatch(typeof(CloneRestSiteOption), "OnSelect")]
internal static class CloneRestThrottlePatch
{
    // Below this many Clone-enchanted cards, let vanilla run (animation is fine and pretty).
    private const int FastPathThreshold = 50;
    // Per-frame clone work is time-sliced: keep each frame's clone burst under this budget so the
    // game keeps rendering smoothly no matter how many cards are cloned. A fixed per-frame count
    // stutters when each deep-clone is slow (huge decks / slow CPUs); a time slice adapts.
    private const double FrameBudgetMs = 6.0;
    // Floor so we don't thrash on tiny per-frame batches; ceiling so one batched Add never spikes.
    private const int MinChunk = 16;
    private const int MaxChunk = 256;
    // How many clones still get the fly-in preview effect (bounded, vs vanilla's all-N).
    private const int PreviewCap = 25;
    // Only show the progress bar when the clone is big enough to actually take time.
    private const int ProgressOverlayMin = 150;

    private static bool Prefix(CloneRestSiteOption __instance, ref Task<bool> __result)
    {
        Player owner = __instance.Owner;
        var toClone = owner.Deck.Cards.Where(c => c.Enchantment is Clone).ToList();
        if (toClone.Count < FastPathThreshold)
            return true; // small count: keep vanilla per-card animation + preview.

        __result = CloneFast(owner, toClone);
        return false;
    }

    private static async Task<bool> CloneFast(Player owner, List<CardModel> toClone)
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        int total = toClone.Count;

        // Progress overlay: a top-of-screen percentage bar so the user sees the clone advancing
        // instead of a dead-air gap followed by a sudden pop. Only for large clones.
        CanvasLayer? overlay = null;
        ProgressBar? bar = null;
        if (tree != null && total >= ProgressOverlayMin)
        {
            overlay = ProgressOverlay.Make(Strings.Get("cloning"), total, out bar);
            tree.Root.AddChild(overlay);
            // Yield once so the bar paints before the first heavy chunk.
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        try
        {
            IReadOnlyList<CardPileAddResult>? firstResults = null;
            var sw = new System.Diagnostics.Stopwatch();
            int done = 0;
            while (done < total)
            {
                sw.Restart();
                var chunkClones = new List<CardModel>();
                // Clone until either the per-frame time budget is spent (past MinChunk) or we hit
                // the MaxChunk ceiling — adapts the batch size to how fast each clone actually is.
                while (done < total
                    && chunkClones.Count < MaxChunk
                    && (chunkClones.Count < MinChunk || sw.Elapsed.TotalMilliseconds < FrameBudgetMs))
                {
                    chunkClones.Add(owner.RunState.CloneCard(toClone[done]));
                    done++;
                }

                // Batched add, no per-card fly visuals. Model + top-bar deck counter still update
                // fully (skipVisuals only suppresses the NCard table animation, not ContentsChanged).
                var res = await CardPileCmd.Add(chunkClones, PileType.Deck, skipVisuals: true);
                firstResults ??= res;

                if (bar != null && GodotObject.IsInstanceValid(bar)) bar.Value = done;

                // Let one frame render between chunks so the game never hard-freezes.
                if (tree != null && done < total)
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }

            // Show the clone effect on a bounded sample (from the first chunk) instead of all N.
            if (firstResults != null)
            {
                int previewCount = System.Math.Min(firstResults.Count, PreviewCap);
                if (previewCount > 0)
                    CardCmd.PreviewCardPileAdd(firstResults.Take(previewCount).ToList(), 1.2f, CardPreviewStyle.MessyLayout);
            }
        }
        finally
        {
            overlay?.QueueFree();
        }
        return true;
    }
}
