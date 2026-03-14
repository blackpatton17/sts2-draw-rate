using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace CardProbMod;

[ModInitializer("Initialize")]
public static class ModMain
{
    private static Harmony? harmony;

    public static void Initialize()
    {
        Log.Info("[CardMod] 正在启动卡牌助手...");
        
        try 
        {
            // 获取 DLL 所在路径并加载 CSV
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string dbPath = Path.Combine(assemblyLocation, "result_cleaned.csv");
            
            CardDatabase.Load(dbPath);

            // 应用 Harmony 补丁
            harmony = new Harmony("CardProbMod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Info("[CardMod] 补丁已成功注入。");
        }
        catch (System.Exception ex)
        {
            Log.Error($"[CardMod] 初始化失败: {ex.Message}");
        }
    }
}
