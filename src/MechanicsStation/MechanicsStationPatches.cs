﻿using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using static MechanicsStation.MechanicsStationAssets;

namespace MechanicsStation
{
    internal sealed class MechanicsStationPatches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(MechanicsStationPatches));
            new POptions().RegisterOptions(this, typeof(MechanicsStationOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AddBuildingAndModifiers()
        {
            Utils.AddBuildingToPlanScreen("Equipment", MechanicsStationConfig.ID, "work stations", PowerControlStationConfig.ID);
            Utils.AddBuildingToTechnology("RefinedObjects", MechanicsStationConfig.ID);
            Init();
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            LoadOptions();
        }

        // шестеренки
        [HarmonyPatch(typeof(MachinePartsConfig), nameof(MachinePartsConfig.CreatePrefab))]
        internal static class MachinePartsConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddOrGet<KPrefabID>().AddTag(GameTags.MiscPickupable);
                __result.AddOrGet<EntitySplitter>();
            }
        }

        // не выбрасывать шестеренки из фабрикаторов
        [HarmonyPatch(typeof(ComplexFabricator), "OnPrefabInit")]
        internal static class ComplexFabricator_OnPrefabInit
        {
            private static void Postfix(ref ComplexFabricator __instance)
            {
                __instance.keepAdditionalTags.SetTag(MachinePartsConfig.TAG);
            }
        }

        // добавление построек для улучшения
        // todo: добавлять постройки из длц по мере необходимости
        private static List<string> BuildingWithElementConverterStopList = new List<string>()
        {
            ResearchCenterConfig.ID,            // слишком читерно
            AdvancedResearchCenterConfig.ID,
            CosmicResearchCenterConfig.ID,
            DLC1CosmicResearchCenterConfig.ID,
            OilRefineryConfig.ID,               // ограничение по трубе
            AirFilterConfig.ID,                 // бесполезно и бессмысленно
            AtmoicGardenConfig.ID,              // вообще недоделанная хрень
            RadiationLightConfig.ID,            // бесполезно. todo: в будующем сделать повышение радиации
        };

        private static List<string> BuildingWithComplexFabricatorWorkableStopList = new List<string>()
        {
            GenericFabricatorConfig.ID,         // странная старая хрень
            EggCrackerConfig.ID,                // было бы нелепо ;-D
            OrbitalResearchCenterConfig.ID,     // слишком читерно
            AdvancedApothecaryConfig.ID,        // новая так и недоделанная хрень
        };

        [HarmonyPatch(typeof(Assets), nameof(Assets.AddBuildingDef))]
        internal static class Assets_AddBuildingDef
        {
            private static void Prefix(ref BuildingDef def)
            {
                var go = def.BuildingComplete;
                if (go != null)
                {
                    // перерабатывающие постройки, требующие искричество
                    if (def.RequiresPowerInput && go.GetComponent<ElementConverter>() != null && !BuildingWithElementConverterStopList.Contains(def.PrefabID))
                    {
                        MakeMachineTinkerable(go);
                        // увеличить всасывание (впервую очередь для скруббера)
                        // увеличить ёмкость потребления из трубы
                        if (go.GetComponent<ConduitConsumer>() != null || go.GetComponent<PassiveElementConsumer>() != null)
                        {
                            go.GetComponent<KPrefabID>().prefabInitFn += delegate (GameObject prefab)
                            {
                                float multiplier = BASE_SPEED_VALUE + (MechanicsStationOptions.Instance.machinery_speed_modifier / 100);
                                var elementConsumer = prefab.GetComponent<PassiveElementConsumer>();
                                if (elementConsumer != null)
                                {
                                    elementConsumer.consumptionRate *= multiplier;
                                    elementConsumer.capacityKG *= multiplier;
                                }

                                foreach (var conduitConsumer in prefab.GetComponents<ConduitConsumer>())
                                {
                                    if (conduitConsumer != null)
                                    {
                                        conduitConsumer.consumptionRate *= multiplier;
                                        conduitConsumer.capacityKG *= multiplier;
                                    }
                                }
                            };
                        }

                        // скважина
                        if (def.PrefabID == OilWellCapConfig.ID)
                        {
                            go.AddOrGet<TinkerableWorkable>();
                        }
                        // удобрятор
                        if (def.PrefabID == FertilizerMakerConfig.ID)
                        {
                            go.AddOrGet<TinkerableFertilizerMaker>();
                            var buildingElementEmitter = go.GetComponent<BuildingElementEmitter>();
                            if (buildingElementEmitter != null)
                            {
                                TinkerableFertilizerMaker.base_methane_production_rate = buildingElementEmitter.emitRate;
                            }
                        }
                    }
                    // фабрикаторы
                    else if (go.GetComponent<ComplexFabricatorWorkable>() != null && !BuildingWithComplexFabricatorWorkableStopList.Contains(def.PrefabID))
                    {
                        MakeMachineTinkerable(go);
                        go.AddOrGet<TinkerableWorkable>();
                    }
                }
            }
        }

        // хак для повышения скорости работы фабрикаторов
        [HarmonyPatch(typeof(Workable), nameof(Workable.GetEfficiencyMultiplier))]
        internal static class Workable_GetEfficiencyMultiplier
        {
            private static void Postfix(Workable __instance, ref float __result)
            {
                var tinkerableWorkable = __instance.GetComponent<TinkerableWorkable>();
                if (tinkerableWorkable != null)
                {
                    __result *= tinkerableWorkable.GetCraftingSpeedMultiplier();
                }
            }
        }

        // хак для повышения скорости выработки газа скважиной
        [HarmonyPatch(typeof(OilWellCap), nameof(OilWellCap.AddGasPressure))]
        internal static class OilWellCap_AddGasPressure
        {
            private static void Prefix(OilWellCap __instance, ref float dt)
            {
                var tinkerableWorkable = __instance.GetComponent<TinkerableWorkable>();
                if (tinkerableWorkable != null)
                {
                    dt *= tinkerableWorkable.GetMachinerySpeedMultiplier();
                }
            }
        }

        // хаки для нефтяной скважины
        // она не является обычной постройкой, она в другом слое присоединяемых построек, поэтому не участвует в подсистеме комнат
        // объявляем растением саму нефтяную дырку, чтобы она получала сообщения о комнатах
        // и перенаправляем сообщения в скважину

        [HarmonyPatch(typeof(OilWellConfig), nameof(OilWellConfig.CreatePrefab))]
        internal static class OilWellConfig_CreatePrefab
        {
            private static void Postfix(ref GameObject __result)
            {
                __result.AddTag(GameTags.Plant);
            }
        }

        private static readonly EventSystem.IntraObjectHandler<BuildingAttachPoint> OnUpdateRoomDelegate =
            new EventSystem.IntraObjectHandler<BuildingAttachPoint>((BuildingAttachPoint component, object data)
                => RetriggerOnUpdateRoom(component, data));

        private static void RetriggerOnUpdateRoom(BuildingAttachPoint buildingAttachPoint, object data)
        {
            for (int i = 0; i < buildingAttachPoint.points.Length; i++)
            {
                buildingAttachPoint.points[i].attachedBuilding?.Trigger((int)GameHashes.UpdateRoom, data);
            }
        }

        [HarmonyPatch(typeof(BuildingAttachPoint), "OnPrefabInit")]
        internal static class BuildingAttachPoint_OnPrefabInit
        {
            private static void Postfix(BuildingAttachPoint __instance)
            {
                __instance.Subscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate);
            }
        }

        [HarmonyPatch(typeof(BuildingAttachPoint), "OnCleanUp")]
        internal static class BuildingAttachPoint_OnCleanUp
        {
            private static void Prefix(BuildingAttachPoint __instance)
            {
                __instance.Unsubscribe((int)GameHashes.UpdateRoom, OnUpdateRoomDelegate);
            }
        }
    }
}
