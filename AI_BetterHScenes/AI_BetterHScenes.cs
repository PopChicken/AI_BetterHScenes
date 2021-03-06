﻿using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Harmony;
using BepInEx.Configuration;

using AIProject;
using AIProject.Definitions;

using AIChara;
using Manager;

using UnityEngine;
using Map = Manager.Map;

namespace AI_BetterHScenes
{
    [BepInPlugin(nameof(AI_BetterHScenes), nameof(AI_BetterHScenes), VERSION)][BepInProcess("AI-Syoujyo")]
    public class AI_BetterHScenes : BaseUnityPlugin
    {
        public const string VERSION = "2.2.1";

        public new static ManualLogSource Logger;

        private static bool inHScene;

        public static HScene hScene;
        public static HSceneManager manager;
        public static HSceneFlagCtrl hFlagCtrl;
        public static HSceneSprite hSprite;

        public static VirtualCameraController hCamera;
        
        public static List<ChaControl> characters;
        public static List<ChaControl> shouldCleanUp;
        public static List<GameObject> finishObjects;

        public static GameObject map;
        public static GameObject mapSimulation;
        public static List<SkinnedCollisionHelper> collisionHelpers;

        private static bool activeUI;
        public static bool cameraShouldLock; // compatibility with other plugins which might disable the camera control
        private static bool mapShouldEnable; // compatibility with other plugins which might disable the map
        private static bool mapSimulationShouldEnable; // compatibility with other plugins which might disable the map simulation

        //-- Draggers --//
        private static ConfigEntry<KeyboardShortcut> showDraggerUI { get; set; }
        
        //-- Clothes --//
        private static ConfigEntry<bool> preventDefaultAnimationChangeStrip { get; set; }
        
        private static ConfigEntry<Tools.OffHStartAnimChange> stripMaleClothes { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripMaleTop { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripMaleBottom { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripMaleGloves { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripMaleShoes { get; set; }

        private static ConfigEntry<Tools.OffHStartAnimChange> stripFemaleClothes { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripFemaleTop { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripFemaleBottom { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripFemaleBra { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripFemalePanties { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripFemaleGloves { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripFemalePantyhose { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripFemaleSocks { get; set; }
        private static ConfigEntry<Tools.ClothesStrip> stripFemaleShoes { get; set; }

        //-- Weakness --//
        public static ConfigEntry<int> countToWeakness { get; private set; }
        private static ConfigEntry<Tools.OffWeaknessAlways> forceTears { get; set; }
        private static ConfigEntry<Tools.OffWeaknessAlways> forceCloseEyes { get; set; }
        private static ConfigEntry<Tools.OffWeaknessAlways> forceStopBlinking { get; set; }
        
        //-- Cum --//
        private static ConfigEntry<Tools.AutoFinish> autoFinish { get; set; }
        private static ConfigEntry<Tools.AutoServicePrefer> autoServicePrefer { get; set; }
        private static ConfigEntry<Tools.AutoInsertPrefer> autoInsertPrefer { get; set; }
        public static ConfigEntry<Tools.CleanCum> cleanCumAfterH { get; private set; }
        private static ConfigEntry<bool> increaseBathDesire { get; set; }
        
        //-- General --//
        private static ConfigEntry<Tools.OffWeaknessAlways> alwaysGaugesHeart { get; set; }
        public static ConfigEntry<bool> keepButtonsInteractive { get; private set; }
        private static ConfigEntry<int> hPointSearchRange { get; set; }
        private static ConfigEntry<bool> unlockCamera { get; set; }
        
        //-- Performance --//
        private static ConfigEntry<bool> disableMap { get; set; }
        private static ConfigEntry<bool> disableMapSimulation { get; set; }
        private static ConfigEntry<bool> optimizeCollisionHelpers { get; set; }
        
        private void Awake()
        {
            Logger = base.Logger;

            shouldCleanUp = new List<ChaControl>();

            showDraggerUI = Config.Bind("QoL > Draggers", "Show draggers UI", new KeyboardShortcut(KeyCode.M));
            
            preventDefaultAnimationChangeStrip = Config.Bind("QoL > Clothes", "Prevent default animationchange strip", true, new ConfigDescription("Prevent default animation change clothes strip (pants, panties, top half state)"));
            
            stripMaleClothes = Config.Bind("QoL > Clothes", "Should strip male clothes", Tools.OffHStartAnimChange.OnHStart, new ConfigDescription("Should strip male clothes during H"));
            stripMaleTop = Config.Bind("QoL > Clothes", "Strip male top", Tools.ClothesStrip.All, new ConfigDescription("Strip male top during H"));
            stripMaleBottom = Config.Bind("QoL > Clothes", "Strip male bottom", Tools.ClothesStrip.All, new ConfigDescription("Strip male bottom during H"));
            stripMaleGloves = Config.Bind("QoL > Clothes", "Strip male gloves", Tools.ClothesStrip.Off, new ConfigDescription("Strip male gloves during H"));
            stripMaleShoes = Config.Bind("QoL > Clothes", "Strip male shoes", Tools.ClothesStrip.Off, new ConfigDescription("Strip male shoes during H"));

            stripFemaleClothes = Config.Bind("QoL > Clothes", "Should strip female clothes", Tools.OffHStartAnimChange.OnHStartAndAnimChange, new ConfigDescription("Should strip female clothes during H"));
            stripFemaleTop = Config.Bind("QoL > Clothes", "Strip female top", Tools.ClothesStrip.Half, new ConfigDescription("Strip female top during H"));
            stripFemaleBottom = Config.Bind("QoL > Clothes", "Strip female bottom", Tools.ClothesStrip.Half, new ConfigDescription("Strip female bottom during H"));
            stripFemaleBra = Config.Bind("QoL > Clothes", "Strip female bra", Tools.ClothesStrip.Half, new ConfigDescription("Strip female bra during H"));
            stripFemalePanties = Config.Bind("QoL > Clothes", "Strip female panties", Tools.ClothesStrip.Half, new ConfigDescription("Strip female panties during H"));
            stripFemaleGloves = Config.Bind("QoL > Clothes", "Strip female gloves", Tools.ClothesStrip.Off, new ConfigDescription("Strip female gloves during H"));
            stripFemalePantyhose = Config.Bind("QoL > Clothes", "Strip female pantyhose", Tools.ClothesStrip.Half, new ConfigDescription("Strip female pantyhose during H"));
            stripFemaleSocks = Config.Bind("QoL > Clothes", "Strip female socks", Tools.ClothesStrip.Off, new ConfigDescription("Strip female socks during H"));
            stripFemaleShoes = Config.Bind("QoL > Clothes", "Strip female shoes", Tools.ClothesStrip.Off, new ConfigDescription("Strip female shoes during H"));
            
            countToWeakness = Config.Bind("QoL > Weakness", "Orgasm count until weakness", 3, new ConfigDescription("How many times does the girl have to orgasm to reach weakness", new AcceptableValueRange<int>(1, 999)));
            forceTears = Config.Bind("QoL > Weakness", "Tears when weakness is reached", Tools.OffWeaknessAlways.WeaknessOnly, new ConfigDescription("Make girl cry when weakness is reached during H"));
            forceCloseEyes = Config.Bind("QoL > Weakness", "Close eyes when weakness is reached", Tools.OffWeaknessAlways.Off, new ConfigDescription("Close girl eyes when weakness is reached during H"));
            forceStopBlinking = Config.Bind("QoL > Weakness", "Stop blinking when weakness is reached", Tools.OffWeaknessAlways.Off, new ConfigDescription("Stop blinking when weakness is reached during H"));

            autoFinish = Config.Bind("QoL > Cum", "Auto finish", Tools.AutoFinish.Off, new ConfigDescription("Automatically finish inside when both gauges reach max"));
            autoServicePrefer = Config.Bind("QoL > Cum", "Preferred auto service finish", Tools.AutoServicePrefer.Drink, new ConfigDescription("Preferred auto finish type. Will fall back to any available option if selected is not available"));
            autoInsertPrefer = Config.Bind("QoL > Cum", "Preferred auto insert finish", Tools.AutoInsertPrefer.Same, new ConfigDescription("Preferred auto finish type. Will fall back to any available option if selected is not available"));
            cleanCumAfterH = Config.Bind("QoL > Cum", "Clean cum on body after H", Tools.CleanCum.All, new ConfigDescription("Clean cum on body after H"));
            increaseBathDesire = Config.Bind("QoL > Cum", "Increase bath desire after H", false, new ConfigDescription("Increase bath desire after H (agents only)"));

            alwaysGaugesHeart = Config.Bind("QoL > General", "Always hit gauge heart", Tools.OffWeaknessAlways.WeaknessOnly, new ConfigDescription("Always hit gauge heart. Will cause progress to increase without having to scroll specific amount"));
            keepButtonsInteractive = Config.Bind("QoL > General", "Keep UI buttons interactive*", false, new ConfigDescription("Keep buttons interactive during certain events like orgasm (WARNING: May cause bugs)"));
            hPointSearchRange = Config.Bind("QoL > General", "H point search range", 300, new ConfigDescription("Range in which H points are shown when changing location (default 60)", new AcceptableValueRange<int>(1, 999)));
            unlockCamera = Config.Bind("QoL > General", "Unlock camera movement", true, new ConfigDescription("Unlock camera zoom out / distance limit during H"));
            
            disableMap = Config.Bind("Performance Improvements", "Disable map", false, new ConfigDescription("Disable map during H scene"));
            disableMapSimulation = Config.Bind("Performance Improvements", "Disable map simulation*", false, new ConfigDescription("Disable map simulation during H scene (WARNING: May cause some effects to disappear)"));
            optimizeCollisionHelpers = Config.Bind("Performance Improvements", "Optimize collisionhelpers", true, new ConfigDescription("Optimize collisionhelpers by letting them update once per frame"));

            countToWeakness.SettingChanged += delegate
            {
                if (!inHScene || hFlagCtrl == null)
                    return;
                
                Traverse.Create(hFlagCtrl).Field("gotoFaintnessCount").SetValue(countToWeakness.Value);
            };
            
            hPointSearchRange.SettingChanged += delegate
            {
                if (!inHScene || hSprite == null)
                    return;

                hSprite.HpointSearchRange = hPointSearchRange.Value;
            };

            unlockCamera.SettingChanged += delegate
            {
                if (!inHScene || hCamera == null)
                    return;
                
                hCamera.isLimitDir = !unlockCamera.Value;
                hCamera.isLimitPos = !unlockCamera.Value;
            };

            disableMap.SettingChanged += delegate
            {
                if (!inHScene || map == null)
                    return;
                
                map.SetActive(!disableMap.Value);
                mapShouldEnable = disableMap.Value;
            };
            
            disableMapSimulation.SettingChanged += delegate
            {
                if (!inHScene || mapSimulation == null)
                    return;
                
                mapSimulation.SetActive(!disableMapSimulation.Value);
                mapSimulationShouldEnable = disableMapSimulation.Value;
            };
            
            optimizeCollisionHelpers.SettingChanged += delegate
            {
                if (!inHScene || collisionHelpers == null)
                    return;

                foreach (var helper in collisionHelpers.Where(helper => helper != null))
                {
                    if (!optimizeCollisionHelpers.Value)
                        helper.forceUpdate = true;
                    
                    helper.updateOncePerFrame = optimizeCollisionHelpers.Value;
                }
            };

            HarmonyWrapper.PatchAll(typeof(Transpilers));
            HarmonyWrapper.PatchAll(typeof(AI_BetterHScenes));
        }

        //-- Draw chara draggers UI --//
        private void OnGUI()
        {
            if(inHScene && activeUI)
                UI.DrawDraggersUI();
        }

        //-- Auto finish, togle chara draggers UI --//
        private void Update()
        {
            if (!inHScene)
                return;
            
            if (showDraggerUI.Value.IsDown())
                activeUI = !activeUI;

            if (autoFinish.Value == Tools.AutoFinish.Off || hFlagCtrl == null || finishObjects == null || finishObjects.Count == 0) 
                return;
            
            int mode = Traverse.Create(hScene).Field("mode").GetValue<int>();
            switch (mode)
            {
                case 1 when hFlagCtrl.feel_m >= 0.98f && (autoFinish.Value == Tools.AutoFinish.ServiceOnly || autoFinish.Value == Tools.AutoFinish.Both): // Houshi
                {
                    bool drink = finishObjects[0].activeSelf;
                    bool vomit = finishObjects[1].activeSelf;
                    bool onbody = finishObjects[2].activeSelf;
                    
                    switch (autoServicePrefer.Value)
                    {
                        case Tools.AutoServicePrefer.Drink when drink:
                            hFlagCtrl.click = HSceneFlagCtrl.ClickKind.FinishDrink;
                            break;
                        case Tools.AutoServicePrefer.Spit when vomit:
                            hFlagCtrl.click = HSceneFlagCtrl.ClickKind.FinishVomit;
                            break;
                        case Tools.AutoServicePrefer.Outside when onbody:
                            hFlagCtrl.click = HSceneFlagCtrl.ClickKind.FinishOutSide;
                            break;
                        case Tools.AutoServicePrefer.Random:
                            List<HSceneFlagCtrl.ClickKind> random = new List<HSceneFlagCtrl.ClickKind>();
                            if(drink)
                                random.Add(HSceneFlagCtrl.ClickKind.FinishDrink);
                            if(vomit)
                                random.Add(HSceneFlagCtrl.ClickKind.FinishVomit);
                            if(onbody)
                                random.Add(HSceneFlagCtrl.ClickKind.FinishOutSide);

                            if (random.Count < 1)
                                break;
                                
                            var rand = new System.Random();
                            hFlagCtrl.click = random[rand.Next(random.Count)];

                            break;
                        default:
                            hFlagCtrl.click = drink ? HSceneFlagCtrl.ClickKind.FinishDrink : vomit ? HSceneFlagCtrl.ClickKind.FinishVomit : onbody ? HSceneFlagCtrl.ClickKind.FinishOutSide : HSceneFlagCtrl.ClickKind.None;
                            break;
                    }

                    break;
                }
                case 2 when hFlagCtrl.feel_f >= 0.98f && hFlagCtrl.feel_m >= 0.98f && (autoFinish.Value == Tools.AutoFinish.InsertOnly || autoFinish.Value == Tools.AutoFinish.Both): // Sonyu
                {
                    bool inside = finishObjects[3].activeSelf;
                    bool outside = finishObjects[2].activeSelf;
                    bool same = finishObjects[4].activeSelf;
                    
                    switch (autoInsertPrefer.Value)
                    {
                        case Tools.AutoInsertPrefer.Inside when inside:
                            hFlagCtrl.click = HSceneFlagCtrl.ClickKind.FinishInSide;
                            break;
                        case Tools.AutoInsertPrefer.Outside when outside:
                            hFlagCtrl.click = HSceneFlagCtrl.ClickKind.FinishOutSide;
                            break;
                        case Tools.AutoInsertPrefer.Same when same:
                            hFlagCtrl.click = HSceneFlagCtrl.ClickKind.FinishSame;
                            break;
                        case Tools.AutoInsertPrefer.Random:
                            List<HSceneFlagCtrl.ClickKind> random = new List<HSceneFlagCtrl.ClickKind>();
                            if(inside)
                                random.Add(HSceneFlagCtrl.ClickKind.FinishInSide);
                            if(outside)
                                random.Add(HSceneFlagCtrl.ClickKind.FinishOutSide);
                            if(same)
                                random.Add(HSceneFlagCtrl.ClickKind.FinishSame);

                            if (random.Count < 1)
                                break;
                                
                            var rand = new System.Random();
                            hFlagCtrl.click = random[rand.Next(random.Count)];

                            break;
                        default:
                            hFlagCtrl.click = inside ? HSceneFlagCtrl.ClickKind.FinishInSide : outside ? HSceneFlagCtrl.ClickKind.FinishOutSide : same ? HSceneFlagCtrl.ClickKind.FinishSame : HSceneFlagCtrl.ClickKind.None;
                            break;
                    }

                    break;
                }
            }
        }
        
        //-- Disable map, simulation to improve performance --//
        //-- Remove hcamera movement limit --//
        //-- Change H point search range --//
        //-- Strip clothes when starting H --//
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void HScene_SetStartVoice_Patch(HScene __instance)
        {
            inHScene = true;
            
            Tools.SetupVariables(__instance);
            
            if (map != null && disableMap.Value)
            {
                map.SetActive(false);
                mapShouldEnable = true;
            }
            
            if (mapSimulation != null && disableMapSimulation.Value)
            {
                mapSimulation.SetActive(false);
                mapSimulationShouldEnable = true;
            }

            if (hCamera != null && unlockCamera.Value)
            {
                hCamera.isLimitDir = false;
                hCamera.isLimitPos = false;
            }

            if (hPointSearchRange.Value != 60 && hSprite != null)
                hSprite.HpointSearchRange = hPointSearchRange.Value;
            
            HScene_StripClothes(stripMaleClothes.Value == Tools.OffHStartAnimChange.OnHStart || stripFemaleClothes.Value == Tools.OffHStartAnimChange.OnHStart);
        }

        //-- Enable map, simulation after H if disabled previously, disable dragger UI --//
        //-- Set bath desire after h --//
        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProc")]
        public static void HScene_EndProc_Patch()
        {
            inHScene = false;

            if (map != null && mapShouldEnable)
            {
                map.SetActive(true);
                mapShouldEnable = false;
            }
            
            if (mapSimulation != null && mapSimulationShouldEnable)
            {
                mapSimulation.SetActive(true);
                mapSimulationShouldEnable = false;
            }

            activeUI = false;

            if (!increaseBathDesire.Value || manager != null && manager.bMerchant)
                return;

            var females = hScene.GetFemales();
            var agentTable = Singleton<Map>.Instance.AgentTable;

            if (females == null || agentTable == null)
                return;
            
            foreach (var female in females.Where(female => female != null))
            {
                var agent = agentTable.FirstOrDefault(pair => pair.Value != null && pair.Value.ChaControl == female).Value;
                if (agent == null)
                    continue;
                
                int bathDesireType = Desire.GetDesireKey(Desire.Type.Bath);
                int lewdDesireType = Desire.GetDesireKey(Desire.Type.H);

                float clampedReason = Tools.Remap(agent.GetFlavorSkill(FlavorSkill.Type.Reason), 0, 99999f, 0, 100f);
                float clampedDirty = Tools.Remap(agent.GetFlavorSkill(FlavorSkill.Type.Dirty), 0, 99999f, 0, 100f);
                float clampedLewd = agent.GetDesire(lewdDesireType) ?? 0;
                float newBathDesire = 100f + (clampedReason * 1.25f) - clampedDirty - clampedLewd * 1.5f;

                agent.SetDesire(bathDesireType, Mathf.Clamp(newBathDesire, 0f, 100f));
            }
        }
        
        //-- Prevent default animation change clothes strip --//
        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "SetClothStateStartMotion")]
        public static bool HScene_SetClothStateStartMotion_PreventDefaultClothesStrip()
        {
            return !inHScene || !preventDefaultAnimationChangeStrip.Value;
        }
        
        //-- Always gauges heart --//
        [HarmonyPostfix, HarmonyPatch(typeof(FeelHit), "isHit")]
        public static void FeelHit_isHit_AlwaysGaugesHeart(ref bool __result)
        {
            if(inHScene && alwaysGaugesHeart.Value == Tools.OffWeaknessAlways.Always || alwaysGaugesHeart.Value == Tools.OffWeaknessAlways.WeaknessOnly && hFlagCtrl != null && hFlagCtrl.isFaintness)
                __result = true;
        }

        //-- Disable camera control when dragger ui open --//
        [HarmonyPrefix, HarmonyPatch(typeof(VirtualCameraController), "LateUpdate")]
        public static bool VirtualCameraController_LateUpdate_DisableCameraControl(VirtualCameraController __instance)
        {
            if (!inHScene || !cameraShouldLock || !activeUI || __instance == null)
                return true;
            
            Traverse.Create(__instance).Property("isControlNow").SetValue(false);
            return false;
        }
   
        //-- Tears, close eyes, stop blinking --//
        [HarmonyPrefix, HarmonyPatch(typeof(HVoiceCtrl), "SetFace")]
        public static void HVoiceCtrl_SetFace_ForceTearsOnWeakness(ref HVoiceCtrl.FaceInfo _face)
        {
            if (!inHScene || _face == null)
                return;

            if(forceTears.Value == Tools.OffWeaknessAlways.Always || forceTears.Value == Tools.OffWeaknessAlways.WeaknessOnly && hFlagCtrl.isFaintness) 
                _face.tear = 1f;

            if(forceCloseEyes.Value == Tools.OffWeaknessAlways.Always || forceCloseEyes.Value == Tools.OffWeaknessAlways.WeaknessOnly && hFlagCtrl.isFaintness)
                _face.openEye = 0.05f;
            
            if(forceStopBlinking.Value == Tools.OffWeaknessAlways.Always || forceStopBlinking.Value == Tools.OffWeaknessAlways.WeaknessOnly && hFlagCtrl.isFaintness)
                _face.blink = false;
        }

        //-- Fix for the massive FPS drop during HScene insert/service positions --//
        [HarmonyPostfix, HarmonyPatch(typeof(SkinnedCollisionHelper), "Init")]
        public static void SkinnedCollisionHelper_Init_UpdateOncePerFrame(SkinnedCollisionHelper __instance)
        {
            if (!inHScene || __instance == null || collisionHelpers == null)
                return;
            
            collisionHelpers.Add(__instance);
            
            if(optimizeCollisionHelpers.Value)
                __instance.updateOncePerFrame = true;
        }
        
        //-- Add character to the shouldCleanUp list --//
        [HarmonyPostfix, HarmonyPatch(typeof(SiruPasteCtrl), "Proc")]
        public static void SiruPasteCtrl_Proc_PopulateList(ChaControl ___chaFemale)
        {
            if (!inHScene || cleanCumAfterH.Value == Tools.CleanCum.Off || cleanCumAfterH.Value == Tools.CleanCum.MerchantOnly || shouldCleanUp == null)
                return;

            ChaControl chara = ___chaFemale;
            if (chara == null || chara.isPlayer || shouldCleanUp.Contains(chara) || (manager != null && manager.bMerchant))
                return;

            AgentActor agent = Singleton<Map>.Instance.AgentTable.Values.FirstOrDefault(actor => actor != null && actor.ChaControl == chara);
            if (agent == null)
                return;
            
            for (int i = 0; i < 5; i++)
            {
                if (chara.GetSiruFlag((ChaFileDefine.SiruParts)i) == 0) 
                    continue;
                
                shouldCleanUp.Add(chara);
                break;
            }
        }
        
        //-- Clean up chara after bath if retaining cum effect --//
        [HarmonyPostfix, HarmonyPatch(typeof(Bath), "OnCompletedStateTask")]
        public static void Bath_OnCompletedStateTask_CleanUpCum(Bath __instance) => Tools.CleanUpSiru(__instance);
        
        //-- Clean up chara after changing if retaining cum effect --//
        [HarmonyPostfix, HarmonyPatch(typeof(ClothChange), "OnCompletedStateTask")]
        public static void ClothChange_OnCompletedStateTask_CleanUpCum(ClothChange __instance) => Tools.CleanUpSiru(__instance);
        
        //-- Strip clothes when changing animation --//
        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        private static void HScene_ChangeAnimation_StripClothes() => HScene_StripClothes(stripMaleClothes.Value == Tools.OffHStartAnimChange.OnHStartAndAnimChange || stripFemaleClothes.Value == Tools.OffHStartAnimChange.OnHStartAndAnimChange);

        private static void HScene_StripClothes(bool shouldStrip)
        {
            if (!inHScene || !shouldStrip || hScene == null)
                return;

            bool stripMales = stripMaleClothes.Value != Tools.OffHStartAnimChange.Off;
            bool stripFemales = stripFemaleClothes.Value != Tools.OffHStartAnimChange.Off;

            var males = hScene.GetMales();
            var females = hScene.GetFemales();
            
            if (stripMales && males != null && males.Length > 0)
            {
                Dictionary<int, Tools.ClothesStrip> stripAmounts = new Dictionary<int, Tools.ClothesStrip>
                {
                    {0, stripMaleTop.Value},
                    {1, stripMaleBottom.Value},
                    {4, stripMaleGloves.Value},
                    {7, stripMaleShoes.Value}
                };

                foreach (var male in males.Where(male => male != null))
                    foreach (var strip in stripAmounts.Where(strip => strip.Value > 0 && male.IsClothesStateKind(strip.Key) && male.fileStatus.clothesState[strip.Key] != 2))
                        male.SetClothesState(strip.Key, (byte)strip.Value);
            }
            
            if (stripFemales && females != null && females.Length > 0)
            {
                Dictionary<int, Tools.ClothesStrip> stripAmounts = new Dictionary<int, Tools.ClothesStrip>
                {
                    {0, stripFemaleTop.Value},
                    {1, stripFemaleBottom.Value},
                    {2, stripFemaleBra.Value},
                    {3, stripFemalePanties.Value},
                    {4, stripFemaleGloves.Value},
                    {5, stripFemalePantyhose.Value},
                    {6, stripFemaleSocks.Value},
                    {7, stripFemaleShoes.Value}
                };

                foreach (var female in females.Where(female => female != null))
                    foreach (var strip in stripAmounts.Where(strip => strip.Value > 0 && female.IsClothesStateKind(strip.Key) && female.fileStatus.clothesState[strip.Key] != 2))
                        female.SetClothesState(strip.Key, (byte)strip.Value);
            }
        }
    }
}