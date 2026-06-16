using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace Sts2CloneOptimizer.Localization;

/// <summary>
/// Minimal in-code localization, following the STS2 sister-mod convention: resolve the
/// current language via <see cref="LocManager"/> (3-letter codes: eng/kor/jpn/…) and look up
/// the string from a per-language table, falling back to English then to the raw key.
/// </summary>
internal static class Strings
{
    public static string Get(string key)
    {
        string lang = CurrentLanguage();
        if (Tables.TryGetValue(lang, out var table) && table.TryGetValue(key, out var value))
            return value;
        if (Tables.TryGetValue("ENG", out var eng) && eng.TryGetValue(key, out var engValue))
            return engValue;
        return key;
    }

    private static string CurrentLanguage()
    {
        try { return (LocManager.Instance?.Language ?? "ENG").ToUpperInvariant(); }
        catch { return "ENG"; }
    }

    // STS2's 14 supported languages: eng zhs deu esp fra ita jpn kor pol ptb rus spa tha tur.
    private static readonly Dictionary<string, Dictionary<string, string>> Tables =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["ENG"] = new() { ["cloning"] = "Cloning…" },
        ["KOR"] = new() { ["cloning"] = "복제 중" },
        ["JPN"] = new() { ["cloning"] = "複製中…" },
        ["ZHS"] = new() { ["cloning"] = "复制中…" },
        ["DEU"] = new() { ["cloning"] = "Klonen…" },
        ["ESP"] = new() { ["cloning"] = "Clonando…" },
        ["SPA"] = new() { ["cloning"] = "Clonando…" },
        ["FRA"] = new() { ["cloning"] = "Clonage…" },
        ["ITA"] = new() { ["cloning"] = "Clonazione…" },
        ["POL"] = new() { ["cloning"] = "Klonowanie…" },
        ["PTB"] = new() { ["cloning"] = "Clonando…" },
        ["RUS"] = new() { ["cloning"] = "Клонирование…" },
        ["THA"] = new() { ["cloning"] = "กำลังโคลน…" },
        ["TUR"] = new() { ["cloning"] = "Klonlanıyor…" },
    };
}
