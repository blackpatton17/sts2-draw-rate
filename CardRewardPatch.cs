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
            string uniqueId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            string uiName = "CardStatsUI_" + uniqueId;

            var container = new Node2D();
            container.Name = uiName;
            
            float boxWidth = 190;
            float boxHeight = 85;

            container.Position = new Vector2(-boxWidth / 2, 160); 
            // 【核心修复】：彻底删除 ZIndex 的设定！
            // Godot 引擎中，一旦设置 ZIndex（即使是相对的 1），
            // 它就会在一个单独的更高渲染层绘制，从而刺穿游戏中所有 ZIndex 为 0 的黑色遮罩层。
            // 只要不设置 ZIndex，它就会乖乖作为卡牌的普通子节点，
            // 当遮罩层盖住卡牌时，也会完美盖住这个胜率框！
            container.Visible = false; 

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

            SetupTracker(__instance, container, border, label, uiName);
        }
        catch (System.Exception ex)
        {
            Log.Error($"[CardMod] 补丁执行出错: {ex.Message}");
        }
    }

    private static void SetupTracker(NCard cardNode, Node2D container, ColorRect border, Label label, string myUiName)
    {
        var timer = new Godot.Timer();
        timer.WaitTime = 0.2f; 
        timer.Autostart = true;
        container.AddChild(timer);

        float defaultY = 160f; 
        float shopY = -300f;   
        float boxWidth = 190f;

        timer.Timeout += () => 
        {
            if (!GodotObject.IsInstanceValid(container) || !GodotObject.IsInstanceValid(cardNode)) return;

            // 主动清理克隆造成的僵尸节点
            foreach (Node child in cardNode.GetChildren())
            {
                string childName = child.Name.ToString();
                if (childName.StartsWith("CardStatsUI") && childName != myUiName)
                {
                    // 找到了因为对象池/克隆机制混进来的旧僵尸节点
                    child.Name = "Killed_" + System.Guid.NewGuid().ToString(); // 改名防止干扰
                    child.QueueFree(); // 销毁
                }
            }

            // 1. 判断位置
            bool isCombat = false;
            bool isShop = false;
            bool isGridOrDeck = false;

            Node current = cardNode.GetParent();
            while (current != null)
            {
                string n = current.Name.ToString().ToLower();
                
                if (n.Contains("battle") || n.Contains("combat") || n.Contains("hand"))
                {
                    isCombat = true;
                }
                
                if (n.Contains("shop") || n.Contains("merchant") || n.Contains("store"))
                {
                    isShop = true;
                }

                if (n.Contains("grid") || n.Contains("deck") || n.Contains("pile") || n.Contains("select") || n.Contains("remove"))
                {
                    isGridOrDeck = true;
                }

                current = current.GetParent();
            }

            // 【关键】如果在战斗或手牌中，强制隐藏卡牌下所有可能的UI（即使是本节点的）
            if (isCombat)
            {
                container.Visible = false;
                // 为了防止克隆体没有Timer导致没被清理，我们手动扫一遍
                foreach (Node child in cardNode.GetChildren())
                {
                    if (child.Name.ToString().StartsWith("CardStatsUI"))
                    {
                        (child as CanvasItem)!.Visible = false;
                    }
                }
                return;
            }

            // 动态排版
            if (isShop && !isGridOrDeck)
            {
                container.Position = new Vector2(-boxWidth / 2, shopY);
            }
            else
            {
                container.Position = new Vector2(-boxWidth / 2, defaultY);
            }

            // 2. 动态读取数据
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            var modelObj = cardNode.GetType().GetField("_model", flags)?.GetValue(cardNode)
                        ?? cardNode.GetType().GetProperty("Model", flags)?.GetValue(cardNode);

            if (modelObj == null)
            {
                container.Visible = false;
                return;
            }

            string internalName = modelObj.GetType().Name;

            // 对于基础牌，也隐藏自己和所有可能的僵尸节点
            if (internalName.StartsWith("Strike") || internalName.StartsWith("Defend"))
            {
                container.Visible = false;
                foreach (Node child in cardNode.GetChildren())
                {
                    if (child.Name.ToString().StartsWith("CardStatsUI"))
                    {
                        (child as CanvasItem)!.Visible = false;
                    }
                }
                return;
            }

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
