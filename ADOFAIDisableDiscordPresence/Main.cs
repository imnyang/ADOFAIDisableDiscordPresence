using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace ADOFAIDisableDiscordPresence
{
    public class Main
    {
        private const string HarmonyId = "ng.imnya.disablediscordpresence";
        private static readonly string[] TargetMethods =
        {
            "Awake",
            "OnEnable",
            "Update",
            "UpdatePresence",
            "CheckForBirthday"
        };

        public static UnityModManager.ModEntry.ModLogger Logger;
        public static UnityModManager.ModEntry ModEntry;
        private static Harmony harmony;

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            Setup(modEntry);
            return true;
        }
        
        internal static void Setup(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            ModEntry = modEntry;
            modEntry.OnToggle = OnToggle;
        }

        private static bool OnToggle(UnityModManager.ModEntry entry, bool value)
        {
            if (value)
            {
                ApplyPatches();
                DisableCurrentPresence();
                Logger.Log("Discord Presence Disable Patches Applied");
            }
            else
            {
                RemovePatches();
                Logger.Log("Discord Presence Disable Patches Removed");
            }

            return true;
        }

        private static void ApplyPatches()
        {
            if (harmony != null)
            {
                return;
            }

            var discordControllerType = AccessTools.TypeByName("DiscordController");
            if (discordControllerType == null)
            {
                Logger.Error("DiscordController is not found. DiscordController is exist?");
                return;
            }

            harmony = new Harmony(HarmonyId);
            var prefix = new HarmonyMethod(typeof(Main), nameof(BlockDiscordPresence));

            foreach (var methodName in TargetMethods)
            {
                var method = AccessTools.Method(discordControllerType, methodName);
                if (method == null)
                {
                    Logger.Warning($"DiscordController.{methodName} is not found. Skipping patch.");
                    continue;
                }

                harmony.Patch(method, prefix: prefix);
            }
        }

        private static void RemovePatches()
        {
            if (harmony == null)
            {
                return;
            }

            harmony.UnpatchAll(HarmonyId);
            harmony = null;
        }

        private static bool BlockDiscordPresence()
        {
            return false;
        }

        private static void DisableCurrentPresence()
        {
            try
            {
                var discordControllerType = AccessTools.TypeByName("DiscordController");
                if (discordControllerType == null)
                {
                    return;
                }

                var shouldUpdatePresenceField = AccessTools.Field(discordControllerType, "shouldUpdatePresence");
                shouldUpdatePresenceField?.SetValue(null, false);

                var instanceField = AccessTools.Field(discordControllerType, "instance");
                var controllerInstance = instanceField?.GetValue(null);
                if (controllerInstance == null)
                {
                    return;
                }

                var discordField = AccessTools.Field(discordControllerType, "discord");
                var discordObject = discordField?.GetValue(controllerInstance);
                if (discordObject == null)
                {
                    return;
                }

                var disposeMethod = discordObject.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance);
                disposeMethod?.Invoke(discordObject, null);
                discordField?.SetValue(controllerInstance, null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Discord Presence disable error: {ex}");
            }
        }
    }
}