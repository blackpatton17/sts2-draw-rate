using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System.Collections.Generic;
using System.Reflection;

namespace CardProbMod;

[HarmonyPatch]
public static class FinalPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var type = typeof(NCard);
        yield return type.GetMethod("_Ready", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        try
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            var modelObj = __instance.GetType().GetField("_model", flags)?.GetValue(__instance)
                        ?? __instance.GetType().GetProperty("Model", flags)?.GetValue(__instance);

            if (modelObj == null) return;

            // 获取卡牌的内部名称，例如 "BurningPact"
            string internalName = modelObj.GetType().Name; 

            // 过滤基础过渡牌（各种职业的打击和防御）
            if (internalName.StartsWith("Strike") || internalName.StartsWith("Defend"))
            {
                return;
            }

            // 直接使用 InternalName 去 CSV 加载的数据里查
            if (CardDatabase.Data.TryGetValue(internalName, out var stats))
            {
                DrawFinalUI(__instance, stats);
            }
            else
            {
                // 如果没查到，显示内部名，方便你检查 CSV 里是否漏了这张牌
                DrawDebugBox(__instance, internalName);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[CardMod] 补丁执行出错: {ex.Message}");
        }
    }

    private static void DrawFinalUI(NCard cardNode, CardData data)
    {
        if (cardNode.HasNode("CardStatsUI")) return;

        var container = new Node2D();
        container.Name = "CardStatsUI";
        
        float boxWidth = 190;
        float boxHeight = 85;

        // 原点在卡牌正中心。
        // X = -95 保证宽度为 190 的框完美左右居中。
        // Y = 160 大概是卡牌下边缘偏下的位置。
        container.Position = new Vector2(-boxWidth / 2, 160); 
        container.ZIndex = 500;

        // 1. 外层边框
        var border = new ColorRect();
        border.Color = Color.FromHtml(data.ColorHex); 
        border.SetSize(new Vector2(boxWidth + 4, boxHeight + 4));
        border.SetPosition(new Vector2(-2, -2)); 
        container.AddChild(border);

        // 2. 内层纯黑背景
        var bg = new ColorRect();
        bg.Color = new Color(0.05f, 0.05f, 0.05f, 0.95f); 
        bg.SetSize(new Vector2(boxWidth, boxHeight));
        container.AddChild(bg);

        // 3. 文字标签
        var label = new Label();
        label.Text = $"【{data.Rank}】\n胜率: {data.WinRate}%\n综合: {data.Score:F1}";
        label.Modulate = Color.FromHtml(data.ColorHex);
        label.SetSize(new Vector2(boxWidth, boxHeight));
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        
        // 字体加粗与阴影
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 1));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        
        container.AddChild(label);
        cardNode.AddChild(container);
    }

    private static void DrawDebugBox(NCard cardNode, string text)
    {
        if (cardNode.HasNode("CardStatsUI")) return;

        var container = new Node2D();
        container.Name = "CardStatsUI";
        
        float boxWidth = 190;
        float boxHeight = 100;

        container.Position = new Vector2(-boxWidth / 2, 160); 
        container.ZIndex = 500;

        var bg = new ColorRect();
        bg.Color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        bg.SetSize(new Vector2(boxWidth, boxHeight)); 
        container.AddChild(bg);

        int total = CardDatabase.Data.Count;
        string sampleKeys = "";
        int count = 0;
        foreach (var k in CardDatabase.Data.Keys)
        {
            sampleKeys += k + " ";
            if (++count >= 2) break;
        }

        var label = new Label();
        label.Text = $"缺失: {text}\n库总数: {total}\n样例: {sampleKeys}";
        label.SetSize(new Vector2(boxWidth, boxHeight));
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AddThemeFontSizeOverride("font_size", 12); 
        
        container.AddChild(label);
        cardNode.AddChild(container);
    }
}
