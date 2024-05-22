
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MC_SVAutoTargeterTargetting
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.autotargetertargetting";
        public const string pluginName = "SV Auto Targeter Targetting";
        public const string pluginVersion = "1.0.1";

        public static ConfigEntry<bool> cfg_OnlyIfNotarget;
        public static ConfigEntry<bool> cfg_OnlyIfCloser;
        public static Transform playerTf;
        public static PlayerControl playerCont;

        internal static ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));

            cfg_OnlyIfNotarget = Config.Bind<bool>("1. Settings",
                "Only auto target if no current target?",
                true,
                "If true, auto targetting an enemy who targets you only occurs if you have no current target.");

            cfg_OnlyIfCloser = Config.Bind<bool>("1. Settings",
                "Only auto target if new targetter is closer?",
                true,
                "If true, auto targetting an enemy who targets you only occurs if they are closer than current hostile target.");
        }

        [HarmonyPatch(typeof(AIControl), nameof(AIControl.SetNewTarget))]
        [HarmonyPostfix]
        private static void AICNewTarget_Pre(AIControl __instance, out bool __state)
        {
            if (playerCont == null)
                playerCont = GameManager.instance.Player.GetComponent<PlayerControl>();
            if (playerTf == null)
                playerTf = GameManager.instance.Player.transform;

            __state = __instance.target == playerTf;
        }

        [HarmonyPatch(typeof(AIControl), nameof(AIControl.SetNewTarget))]
        [HarmonyPostfix]
        private static void AICNewTarget_Post(AIControl __instance, bool __state)
        {
            if (!__state || (playerCont.target != null && cfg_OnlyIfNotarget.Value))
                return;

            if (__instance.target == playerTf)
            {
                if (cfg_OnlyIfCloser.Value && playerCont.target != null && playerCont.target != __instance.transform)
                {
                    AIControl curTAIC = playerCont.target.GetComponent<AIControl>();
                    if (curTAIC != null)
                    {
                        SpaceShip curTAICSS = curTAIC.GetSS();
                        if (curTAICSS != null && curTAICSS.ffSys != null && curTAICSS.ffSys.hostileToPlayer)
                        {
                            if (Vector3.Distance(playerTf.position, curTAIC.transform.position) <
                               Vector3.Distance(playerTf.position, __instance.transform.position))
                                return;
                        }
                    }
                }

                playerCont.SetTarget(__instance.transform);
            }
        }
    }
}
