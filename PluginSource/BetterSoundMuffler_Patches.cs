using AK.Wwise;
using AkWwise = AK.Wwise;
using HarmonyLib;
using KSP.VFX;
using UnityEngine;

namespace BetterSoundMufflerRedux
{
    internal static class BetterSoundMufflerPatchHelper
    {
        internal static void Apply(string patchName, GameObject target, string extra)
        {
            try
            {
                BetterSoundMufflerController active = BetterSoundMufflerController.Active;
                if (active == null) return;

                active.LogPatch(patchName, target, extra);
                if (target != null) active.ApplyToEmitter(target);
            }
            catch
            {
            }
        }

        internal static void ApplyHierarchy(string patchName, GameObject root, string extra)
        {
            try
            {
                BetterSoundMufflerController active = BetterSoundMufflerController.Active;
                if (active == null) return;

                active.LogPatch(patchName, root, extra);
                if (root != null) active.ApplyToHierarchy(root);
            }
            catch
            {
            }
        }

        internal static void PostStop(GameObject target)
        {
            try
            {
                BetterSoundMufflerController active = BetterSoundMufflerController.Active;
                if (active == null || target == null) return;

                active.StopEmitterIfMuffled(target);
            }
            catch
            {
            }
        }

        internal static void RefreshCache()
        {
            try
            {
                BetterSoundMufflerController active = BetterSoundMufflerController.Active;
                if (active == null) return;

                active.RefreshEmitterCacheNow();
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(KSPBaseAudio), "PostEvent", new[] { typeof(string), typeof(GameObject), typeof(uint), typeof(AkCallbackManager.EventCallback), typeof(object) })]
    internal static class BetterSoundMufflerBaseAudioPostEventStringPatch
    {
        private static void Prefix(string eventName, GameObject owner)
        {
            BetterSoundMufflerPatchHelper.Apply("KSPBaseAudio.PostEvent(string)", owner, "event=" + (eventName == null ? "null" : eventName));
        }

        private static void Postfix(GameObject owner)
        {
            BetterSoundMufflerPatchHelper.PostStop(owner);
        }
    }

    [HarmonyPatch(typeof(KSPBaseAudio), "PostEvent", new[] { typeof(uint), typeof(GameObject), typeof(uint), typeof(AkCallbackManager.EventCallback), typeof(object) })]
    internal static class BetterSoundMufflerBaseAudioPostEventIdPatch
    {
        private static void Prefix(uint eventID, GameObject owner)
        {
            BetterSoundMufflerPatchHelper.Apply("KSPBaseAudio.PostEvent(uint)", owner, "id=" + eventID);
        }

        private static void Postfix(GameObject owner)
        {
            BetterSoundMufflerPatchHelper.PostStop(owner);
        }
    }

    [HarmonyPatch(typeof(AkWwise.Event), "Post", new[] { typeof(GameObject) })]
    internal static class BetterSoundMufflerEventPostSimplePatch
    {
        private static void Prefix(GameObject gameObject, AkWwise.Event __instance)
        {
            BetterSoundMufflerPatchHelper.Apply("AkWwise.Event.Post(GameObject)", gameObject, "event=" + (__instance != null ? __instance.Name : "null"));
        }

        private static void Postfix(GameObject gameObject)
        {
            BetterSoundMufflerPatchHelper.PostStop(gameObject);
        }
    }

    [HarmonyPatch(typeof(AkWwise.Event), "Post", new[] { typeof(GameObject), typeof(CallbackFlags), typeof(AkCallbackManager.EventCallback), typeof(object) })]
    internal static class BetterSoundMufflerEventPostCallbackFlagsPatch
    {
        private static void Prefix(GameObject gameObject, AkWwise.Event __instance)
        {
            BetterSoundMufflerPatchHelper.Apply("AkWwise.Event.Post(GameObject,CallbackFlags)", gameObject, "event=" + (__instance != null ? __instance.Name : "null"));
        }

        private static void Postfix(GameObject gameObject)
        {
            BetterSoundMufflerPatchHelper.PostStop(gameObject);
        }
    }

    [HarmonyPatch(typeof(AkWwise.Event), "Post", new[] { typeof(GameObject), typeof(uint), typeof(AkCallbackManager.EventCallback), typeof(object) })]
    internal static class BetterSoundMufflerEventPostUintPatch
    {
        private static void Prefix(GameObject gameObject, AkWwise.Event __instance)
        {
            BetterSoundMufflerPatchHelper.Apply("AkWwise.Event.Post(GameObject,uint)", gameObject, "event=" + (__instance != null ? __instance.Name : "null"));
        }

        private static void Postfix(GameObject gameObject)
        {
            BetterSoundMufflerPatchHelper.PostStop(gameObject);
        }
    }

    [HarmonyPatch(typeof(AkWwise.Event), "ExecuteAction", new[] { typeof(GameObject), typeof(AkActionOnEventType), typeof(int), typeof(AkCurveInterpolation) })]
    internal static class BetterSoundMufflerEventExecuteActionPatch
    {
        private static void Prefix(GameObject gameObject, AkWwise.Event __instance)
        {
            BetterSoundMufflerPatchHelper.Apply("AkWwise.Event.ExecuteAction", gameObject, "event=" + (__instance != null ? __instance.Name : "null"));
        }

        private static void Postfix(GameObject gameObject)
        {
            BetterSoundMufflerPatchHelper.PostStop(gameObject);
        }
    }

    [HarmonyPatch(typeof(FXContextualEvent), "Instantiate")]
    internal static class BetterSoundMufflerContextualVfxPatch
    {
        private static void Postfix(FXContextualEvent __instance)
        {
            if (__instance == null) return;
            BetterSoundMufflerPatchHelper.RefreshCache();
            BetterSoundMufflerPatchHelper.ApplyHierarchy("FXContextualEvent.Instantiate", __instance.SpawnedPrefab, "class=" + __instance.GetType().Name);
        }
    }

    [HarmonyPatch(typeof(FXPersistantSurfaceContactContextualEvent), "Instantiate")]
    internal static class BetterSoundMufflerPersistantContactPatch
    {
        private static void Postfix(FXPersistantSurfaceContactContextualEvent __instance)
        {
            if (__instance == null) return;
            BetterSoundMufflerPatchHelper.RefreshCache();
            BetterSoundMufflerPatchHelper.ApplyHierarchy("FXPersistantSurfaceContactContextualEvent.Instantiate", __instance.SpawnedPrefab, null);
        }
    }

    [HarmonyPatch(typeof(FXWheelSurfaceContactContextualEvent), "Instantiate")]
    internal static class BetterSoundMufflerWheelContactPatch
    {
        private static void Postfix(FXWheelSurfaceContactContextualEvent __instance)
        {
            if (__instance == null) return;
            BetterSoundMufflerPatchHelper.RefreshCache();
            BetterSoundMufflerPatchHelper.ApplyHierarchy("FXWheelSurfaceContactContextualEvent.Instantiate", __instance.SpawnedPrefab, null);
        }
    }

    [HarmonyPatch(typeof(KSPPartAudioManager), "PostEvent")]
    internal static class BetterSoundMufflerPartAudioManagerPostPatch
    {
        private static void Prefix(KSPPartAudioManager __instance)
        {
            if (__instance == null) return;

            BetterSoundMufflerPatchHelper.RefreshCache();
            ApplyPartAudio(__instance.PartAudio, "PartAudioManager.PostEvent:part");
            ApplyPartAudio(__instance.PartEngineAudio, "PartAudioManager.PostEvent:engine");
            ApplyPartAudio(__instance.PartGeneratorAudio, "PartAudioManager.PostEvent:generator");
            ApplyPartAudio(__instance.PartWheelAudio, "PartAudioManager.PostEvent:wheel");
            ApplyPartAudio(__instance.PartParachuteAudio, "PartAudioManager.PostEvent:parachute");
            ApplyPartAudio(__instance.PartCoolingAudio, "PartAudioManager.PostEvent:cooling");
        }

        private static void ApplyPartAudio(KSPPartAudioBase audio, string patchName)
        {
            if (audio == null) return;

            BetterSoundMufflerPatchHelper.Apply(patchName, audio.gameObject, null);
        }
    }

    [HarmonyPatch(typeof(KSPPartAudioManager), "OnPartUndocked")]
    internal static class BetterSoundMufflerPartUndockPatch
    {
        private static void Prefix()
        {
            BetterSoundMufflerPatchHelper.RefreshCache();
        }
    }
}
