using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using vScripting.core;
using vScripting.core.cmds;
using vScripting.core.engine;

namespace vScripting
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        internal static ScriptEngine Engine;
        internal static ManualLogSource LogS;

        private const string ScriptFileName = "deneme.vs";         

        public override void Load()
        {
            LogS = Log;
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Engine = new ScriptEngine();

            Engine.Register(new PrintCommand());           
            Engine.RegisterAlias("yazdir", "print");
            Engine.RegisterAlias("yazdýr", "print");

            var scriptPath = Path.Combine(Paths.ConfigPath, ScriptFileName);
            try
            {
                Engine.LoadScript(scriptPath);
                Log.LogInfo($"Script yüklendi: {scriptPath}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Script yüklenirken hata: {ex}");
            }
        }


        [HarmonyPatch(typeof(GameModeTag), nameof(GameModeTag.OnFreezeOver))]
        [HarmonyPostfix]
        internal static void OnFreezeOver()
        {
            if (Engine == null) return;

            // SONUNDA CALISTI 

            var ctx = new ScriptContext();
            try
            {
                Engine.Trigger("OyunBaslangici", ctx);

            }
            catch (Exception ex)
            {
                LogS?.LogError($"OyunBaslangici olayý çalýþýrken script hatasý: {ex}");
            }
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.TagPlayer))]
        [HarmonyPostfix]
        public static void ServerSendTagPlayer(ulong param_0, ulong param_1)
        {
            if (param_0 == 0) return;

            ulong tagger = param_0;
            ulong victim = param_1;

            String name = Utils.getNameByUlong(tagger);
            String name_victim = Utils.getNameByUlong(victim);

            if (Engine != null)
            {
                var ctx = new ScriptContext();
                Engine.Trigger("OyuncuTag", ctx, name, name_victim);
            }
        }

        public void pushEvent(string name)
        {
            if (Engine == null) return;

            var ctx = new ScriptContext();
            try
            {
                Engine.Trigger(name, ctx);

            }
            catch (Exception ex)
            {
                LogS?.LogError($"{name} olayý çalýþýrken script hatasý: {ex}");
            }
        }
    }
}