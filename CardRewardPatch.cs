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
            if (__instance.HasNode("CardStatsUI")) return;

            var container = new Node2D();
            container.Name = "CardStatsUI";
            
            float boxWidth = 190;
            float boxHeight = 85;

            // 完美居中，在卡牌下方
            container.Position = new Vector2(-boxWidth / 2, 160); 
            container.ZIndex = 500;
            container.Visible = false; // 初始隐藏，由定时器接管

            var border = new ColorRect();
            border.SetSize(new Vector2(boxWidth + 4, boxHeight + 4));
            border.SetPosition(new Vector2(-2, -2)); 
            container.AddChild(border);

            var bg = new ColorRect();
            bg.Color = new Color(0.05f, 0.05f, 0.05f, 0.95f); 
            bg.SetSize(new Vector2(boxWidth, boxHeight));
            container.AddChild(bg);

            var label = new Label();
            label.SetSize(new Vector2(boxWidth, boxHeight));
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 16);
            label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 1));
            label.AddThemeConstantOverride("shadow_offset_x", 1);
            label.AddThemeConstantOverride("shadow_offset_y", 1);
            
            container.AddChild(label);
            __instance.AddChild(container);

            // 使用超轻量定时器来应对 Godot 的对象池复用
            SetupTracker(__instance, container, border, label);
        }
        catch (System.Exception ex)
        {
            Log.Error($"[CardMod] 补丁执行出错: {ex.Message}");
        }
    }

    private static void SetupTracker(NCard cardNode, Node2D container, ColorRect border, Label label)
    {
        var timer = new Godot.Timer();
        timer.WaitTime = 0.2f; // 每秒只执行 5 次，性能开销为 0
        timer.Autostart = true;
        container.AddChild(timer);

        timer.Timeout += () => 
        {
            if (!GodotObject.IsInstanceValid(container) || !GodotObject.IsInstanceValid(cardNode)) return;

            // 如果卡牌不在屏幕树里，直接隐藏
            if (!cardNode.IsInsideTree())
            {
                container.Visible = false;
                return;
            }

            // 1. 判断位置：只要父节点有以下名字，说明在战斗中，立刻隐藏
            bool isCombat = false;
            Node current = cardNode.GetParent();
            while (current != null)
            {
                string n = current.Name.ToString().ToLower();
                if (n.Contains("hand") || n.Contains("battle") || n.Contains("combat") || 
                    n.Contains("deck") || n.Contains("pile") || n.Contains("discard"))
                {
                    isCombat = true;
                    break;
                }
                current = current.GetParent();
            }

            if (isCombat)
            {
                container.Visible = false;
                return;
            }

            // 2. 动态读取数据（卡牌从对象池拿出来时，Model 会变）
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            var modelObj = cardNode.GetType().GetField("_model", flags)?.GetValue(cardNode)
                        ?? cardNode.GetType().GetProperty("Model", flags)?.GetValue(cardNode);

            if (modelObj == null)
            {
                container.Visible = false;
                return;
            }

            string internalName = modelObj.GetType().Name;

            if (internalName.StartsWith("Strike") || internalName.StartsWith("Defend"))
            {
                container.Visible = false;
                return;
            }

            // 显示数据
            container.Visible = true;
            if (CardDatabase.Data.TryGetValue(internalName, out var stats))
            {
                label.Text = $"【{stats.Rank}】\n胜率: {stats.WinRate}%\n综合: {stats.Score:F1}";
                label.Modulate = Color.FromHtml(stats.ColorHex);
                border.Color = Color.FromHtml(stats.ColorHex);
            }
            else
            {
                label.Text = $"缺失:\n{internalName}";
                label.Modulate = new Color(0.8f, 0.8f, 0.8f, 1);
                border.Color = new Color(0.3f, 0.3f, 0.3f, 1);
            }
        };
    }
}
