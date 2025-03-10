using System.Reflection;
using HarmonyLib;
using NitroxClient.MonoBehaviours;
using NitroxClient.Unity.Helper;
using NitroxModel.DataStructures;
using NitroxModel.Helper;
using UnityEngine;
using NitroxClient.GameLogic;

namespace NitroxPatcher.Patches.Dynamic
{
    public class SubNameInput_OnColorChange_Patch : NitroxPatch, IDynamicPatch
    {
        private static readonly MethodInfo TARGET_METHOD = Reflect.Method((SubNameInput t) => t.OnColorChange(default(ColorChangeEventData)));

        public static void Postfix(SubNameInput __instance, ColorChangeEventData eventData)
        {
            if (!__instance.TryGetComponent(out NitroxEntity subNameInput))
            {
                // prevent this patch from firing when the initial template cyclops loads (happens on game load with living large update).
                return;
            }

            SubName subname = __instance.target;
            if (subname)
            {
                GameObject parentVehicle;
                NitroxId controllerId = null;

                if (subname.TryGetComponent(out Vehicle vehicle))
                {
                    parentVehicle = vehicle.gameObject;

                    GameObject baseCell = __instance.gameObject.RequireComponentInParent<BaseCell>().gameObject;
                    GameObject moonpool = baseCell.RequireComponentInChildren<BaseFoundationPiece>().gameObject;

                    controllerId = NitroxEntity.GetId(moonpool);
                }
                else if (subname.TryGetComponentInParent(out SubRoot subRoot))
                {
                    parentVehicle = subRoot.gameObject;
                }
                else if (subname.TryGetComponentInParent(out Rocket rocket))
                {
                    // For some reason only the rocket has a full functioning ghost with a different NitroxId when spawning/constructing, so we are ignoring it.
                    if (rocket.TryGetComponentInChildren(out VFXConstructing constructing) && !constructing.isDone)
                    {
                        return;
                    }

                    parentVehicle = rocket.gameObject;
                }
                else
                {
                    Log.Error($"[SubNameInput_OnColorChange_Patch] The GameObject {subname.gameObject.name} doesn't have a Vehicle/SubRoot/Rocket component.");
                    return;
                }

                Resolve<Entities>().EntityMetadataChangedThrottled(__instance, subNameInput.Id);
            }
        }

        public override void Patch(Harmony harmony)
        {
            PatchPostfix(harmony, TARGET_METHOD);
        }
    }
}
