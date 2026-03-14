using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MegaCrit.Sts2.Core.Logging;

namespace CardProbMod;

public class CardData
{
    public string InternalName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float WinRate { get; set; }
    public float PickRate { get; set; }
    public float SkipRate { get; set; }

    public float Score => (WinRate * 0.6f) + (PickRate * 0.4f);

    public string Rank => Score switch
    {
        > 48 => "S (必拿)",
        > 42 => "A (优质)",
        > 35 => "B (可用)",
        > 28 => "C (平庸)",
        _ => "D (陷阱)"
    };

    public string ColorHex => Score switch
    {
        > 48 => "#FFD700", // Gold
        > 42 => "#E0E0E0", // Silver
        > 35 => "#CD7F32", // Bronze
        > 28 => "#A0A0A0", // Gray
        _ => "#707070"    // Dark Gray
    };
}

public static class CardDatabase
{
    // 强制不区分大小写
    public static Dictionary<string, CardData> Data = new(StringComparer.OrdinalIgnoreCase);

    public static void Load(string filePath)
    {
        Data.Clear();
        if (!File.Exists(filePath))
        {
            Log.Error($"[CardMod] 数据库文件未找到: {filePath}");
            return;
        }

        try
        {
            using var reader = new StreamReader(filePath);
            var header = reader.ReadLine(); // 直接跳过表头行

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 5) continue;

                // 粗暴直白的按列读取
                string internalName = parts[0].Trim();
                
                // 如果是"未知"则忽略该行
                if (string.IsNullOrEmpty(internalName) || internalName == "未知") continue;

                var card = new CardData
                {
                    InternalName = internalName,
                    Name = parts[1].Trim(),
                    WinRate = ParseFloat(parts[2]),
                    PickRate = ParseFloat(parts[3]),
                    SkipRate = ParseFloat(parts[4])
                };

                // 直接把 InternalName 存为 Key
                if (!Data.ContainsKey(internalName))
                {
                    Data.Add(internalName, card);
                }
            }
            Log.Info($"[CardMod] 数据库加载完成。成功载入 {Data.Count} 条数据。");
        }
        catch (Exception ex)
        {
            Log.Error($"[CardMod] 加载数据库时出错: {ex.Message}");
        }
    }

    private static float ParseFloat(string value)
    {
        if (float.TryParse(value.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return 0;
    }
}
