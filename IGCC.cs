using BepInEx;
using BepInEx.Logging;
using DiskCardGame;
using HarmonyLib;
using IGCCMod.JSON;
using IGCCMod.Util;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using static CardSelectSequencers;

namespace IGCCMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("cyantist.inscryption.api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("MADH.inscryption.JSONLoader", BepInDependency.DependencyFlags.HardDependency)]
    public class IGCC : BaseUnityPlugin
    {
        private const string PluginGuid = "IngoH.inscryption.IGCCMod";
        private const string PluginName = "IGCCMod";
        private const string PluginVersion = "2.3.1";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Logger.LogInfo($"Loaded {PluginName}!");
            Log = base.Logger;

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

        }

        [HarmonyPatch(typeof(DoorKnobInteractable), "OnCursorSelectStart")]
        public class DoorPatch : DoorKnobInteractable
        {
            public static bool Prefix(DoorKnobInteractable __instance)
            {
                // Set cause of death to PixelP03FinaleBoss, as it clearly is unused for part 1 (and highly unlikely to be used by a mod)
                SaveManager.SaveFile.currentRun.causeOfDeath = new RunState.CauseOfDeath(Opponent.Type.PixelP03FinaleBoss);
                SaveManager.SaveToFile(saveActiveScene: false);
                // Load when door is clicked
                SceneLoader.Load("Part1_Sanctum");
                return true;
            }
        }


        [HarmonyPatch(typeof(SanctumSceneSequencer), "IntroSequence")]
        public class SanctumIntroPatch : SanctumSceneSequencer
        {
            public static bool Prefix(SanctumSceneSequencer __instance)
            {
                // Cancel orignal method
                return false;
                //if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss) return false;
                //else return true;
            }
        }

        [HarmonyPatch(typeof(SanctumSceneSequencer), "IntroSequence")]
        public class PostSanctumIntroPatch : SanctumSceneSequencer
        {
            public static IEnumerator Postfix(IEnumerator __result, SanctumSceneSequencer __instance)
            {
                //if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss)
                //{
                // Abbreviated version of original method.
                __instance.GetType().GetMethod("StartHumLoop", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                __instance.GetType().GetMethod("InitializeLeshy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                Singleton<ViewManager>.Instance.SwitchToView(View.SanctumFloorDown, immediate: true, lockAfter: true);
                yield return new WaitForSeconds(0.5f);
                LookUp();
                Singleton<InteractionCursor>.Instance.SetHidden(hidden: false);
                yield return new WaitForSeconds(0.5f);
                yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("You entered the [c:bR]card creation[c:] mode.");
                yield return __instance.GetType().GetMethod("DeathCardSequence", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                //}
            }
        }

        public static void LookDown()
        {
            bool skipMove = false;
            if (Singleton<ViewManager>.Instance.CurrentView == View.SanctumFloorDown) skipMove = true;
            Singleton<ViewManager>.Instance.SwitchToView(View.SanctumFloorDown, immediate: false, lockAfter: true);
            Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
            if (!skipMove)
            {
                Vector3 pos = Singleton<ViewManager>.Instance.CameraParent.localPosition;
                Pixelplacement.Tween.LocalPosition(Singleton<ViewManager>.Instance.CameraParent, new Vector3(pos.x, pos.y + 3.25f, pos.z + 4.25f), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
            }
        }

        public static void LookUp()
        {
            Singleton<RuleBookController>.Instance.SetShown(false);
            Singleton<ViewManager>.Instance.SwitchToView(View.SanctumFloorUp, immediate: false, lockAfter: true);
        }

        private static void ZoomIn()
        {
            Vector3 pos = Singleton<ViewManager>.Instance.CameraParent.localPosition;
            Pixelplacement.Tween.LocalPosition(Singleton<ViewManager>.Instance.CameraParent, new Vector3(pos.x, pos.y - 3.0f, pos.z), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
        }

        private static void MoveDown()
        {
            Vector3 pos = Singleton<ViewManager>.Instance.CameraParent.localPosition;
            Pixelplacement.Tween.LocalPosition(Singleton<ViewManager>.Instance.CameraParent, new Vector3(pos.x, pos.y, pos.z - 1.25f), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
        }

        private static void ResetCamera()
        {
            Vector3 pos = Singleton<ViewManager>.Instance.CameraParent.localPosition;
            Pixelplacement.Tween.LocalPosition(Singleton<ViewManager>.Instance.CameraParent, new Vector3(pos.x, pos.y + 3.0f, pos.z + 1.25f), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
        }


        // Rulebook camera cancel
        [HarmonyPatch(typeof(ViewManager), "OffsetPosition", new Type[] { typeof(Vector3), typeof(float) })]
        public class RuleBookCameraPatch : ViewManager
        {
            public static bool Prefix(ViewManager __instance, Vector3 cameraOffset)
            {
                if (Instance.CurrentView == View.SanctumFloorDown)
                {
                    Vector3 pos = Instance.CameraParent.localPosition;
                    if (cameraOffset.z >= 0)
                    {
                        Pixelplacement.Tween.LocalRotation(Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                    }
                    else
                    {
                        Pixelplacement.Tween.LocalRotation(Instance.CameraParent, new Vector3(105, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                    }
                    return false;
                }
                else return true;
            }
        }

        [HarmonyPatch(typeof(SanctumSceneSequencer), "DeathCardSequence", null)]
        public class DeathCardStartPatch : SanctumSceneSequencer
        {
            public static bool Prefix(SanctumSceneSequencer __instance)
            {
                // Cancel orignal method
                return false;
                //if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss) return false;
                //else return true;
            }
        }

        [HarmonyPatch(typeof(SanctumSceneSequencer), "DeathCardSequence", null)]
        public class PostDeathCardStartPatch : SanctumSceneSequencer
        {
            public static IEnumerator Postfix(IEnumerator __result, SanctumSceneSequencer __instance, DeathCardCreationSequencer ___deathCardSequencer)
            {
                //if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss)
                //{
                yield return new WaitForSeconds(0.25f);
                yield return ___deathCardSequencer.CreateCardSequence(true);
                //}
            }
        }


        [HarmonyPatch(typeof(DeathCardCreationSequencer), "CreateCardSequence", new Type[] { typeof(bool) })]
        public class DeathCardPatch : DeathCardCreationSequencer
        {
            public static bool Prefix(DeathCardCreationSequencer __instance)
            {
                // Cancel orignal method
                return false;
                //if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss) return false;
                //else return true;
            }
        }

        [HarmonyPatch(typeof(DeathCardCreationSequencer), "CreateCardSequence", new Type[] { typeof(bool) })]
        public class PostDeathCardPatch : DeathCardCreationSequencer
        {
            public static IEnumerator Postfix(IEnumerator __result, DeathCardCreationSequencer __instance, KeyboardInputHandler ___keyboardInput)
            {
                //if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss)
                //{
                bool done = false;
                if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType != Opponent.Type.PixelP03FinaleBoss)
                {
                    LookUp();
                    yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("It appears that you lost normally.");
                    yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("Only worthy challengers may access the Card Creator.");
                    yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("Try again, but this time open the backroom door normally.");
                    yield return Singleton<TextDisplayer>.Instance.ShowMessage("Goodbye.");
                    yield return new WaitForSeconds(0.5f);
                    LeshyAnimationController.Instance.LeftArm.PlayAnimation("takephoto_left");
                    yield return new WaitForSeconds(1.5f);
                    if (UnityEngine.Random.value > 0.8f)
                    {
                        Singleton<VideoCameraRig>.Instance.PlayCameraAnim("refocus_medium");
                    }
                    LeshyAnimationController.Instance.LeftArm.SetTrigger("photo_flare");
                    yield return new WaitForSeconds(1f);
                    Singleton<TextDisplayer>.Instance.Clear();
                    AudioSource audioSource2 = AudioController.Instance.PlaySound2D("camera_flash_gameover", MixerGroup.None, 0.85f);
                    audioSource2.gameObject.name = "flashSound";
                    UnityEngine.Object.DontDestroyOnLoad(audioSource2.gameObject);
                    AudioController.Instance.StopAllLoops();
                    Singleton<UIManager>.Instance.Effects.GetEffect<ScreenColorEffect>().SetColor(GameColors.Instance.nearWhite);
                    Singleton<UIManager>.Instance.Effects.GetEffect<ScreenColorEffect>().SetIntensity(1f, 100f);
                    SaveManager.SaveFile.NewPart1Run();
                    yield return new WaitForSeconds(1f);
                    AsyncOperation asyncOp2 = SceneLoader.StartAsyncLoad("Part1_Cabin");
                    SceneLoader.CompleteAsyncLoad(asyncOp2);
                    SaveManager.SaveToFile(saveActiveScene: false);
                }
                else
                {
                    while (!done)
                    {
                        // Create preview card
                        Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                        Vector3 vector = GetCardIndexLoc(obj, 17);
                        CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                        CardModificationInfo addTo = new CardModificationInfo
                        {
                            nameReplacement = "current card"
                        };
                        c.Mods.Add(addTo);
                        SelectableCard preview = CreatePreviewCard(__instance, vector, c);

                        List<int> prevSelects = new List<int>();
                        int i = 1;
                        int j = 0;
                        while (i != -1)
                        {
                            yield return CardCreationStep(i, prevSelects, __instance, preview);
                            if (j < 7)
                            {
                                i++;
                                j++;
                                if (i == 2) i++;
                            }
                            else
                            {
                                IntRef ir = new IntRef();
                                yield return StepSelect(__instance, ir);
                                i = ir.Value;
                            }
                        }


                        // Move to center
                        Vector3 vector2 = GetCardIndexLoc(obj, 7);
                        Pixelplacement.Tween.Position(preview.transform, vector2, 0.25f, 0f);

                        // Enter name
                        ___keyboardInput.maxInputLength = 127;
                        yield return new WaitForSeconds(0.25f);
                        ZoomIn();
                        Singleton<TextDisplayer>.Instance.ShowMessage("You should give it a name.");
                        CardModificationInfo nameMod = new CardModificationInfo();
                        preview.Info.Mods.Add(nameMod);
                        yield return __instance.GetType().GetMethod("EnterNameForCard", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { preview, nameMod });
                        yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("[c:bR]" + nameMod.nameReplacement.Trim() + "[c:]?");
                        yield return new WaitForSeconds(0.25f);

                        // Enter description
                        ___keyboardInput.maxInputLength = 1027;
                        Singleton<TextDisplayer>.Instance.ShowMessage("Please type the description.");
                        yield return EnterDescription(___keyboardInput, preview);
                        yield return new WaitForSeconds(0.25f);
                        MoveDown();
                        yield return FinalizeCard(__instance, preview);
                        // Finalize
                        if (preview == null)
                        {
                            // End
                            done = true;
                        }
                        else
                        {
                            // Restart
                            preview.Anim.PlayDeathAnimation();
                            UnityEngine.Object.Destroy(preview.gameObject, 2f);
                            yield return new WaitForSeconds(0.25f);
                        }
                        ResetCamera();

                    }
                    // Take photo and return to table
                    yield return new WaitForSeconds(0.5f);
                    yield return Singleton<TextDisplayer>.Instance.ShowMessage("It is time for you to return.");
                    LookUp();
                    yield return new WaitForSeconds(0.5f);
                    LeshyAnimationController.Instance.LeftArm.PlayAnimation("takephoto_left");
                    yield return new WaitForSeconds(1.5f);
                    if (UnityEngine.Random.value > 0.8f)
                    {
                        Singleton<VideoCameraRig>.Instance.PlayCameraAnim("refocus_medium");
                    }
                    LeshyAnimationController.Instance.LeftArm.SetTrigger("photo_flare");
                    yield return new WaitForSeconds(1f);
                    Singleton<TextDisplayer>.Instance.Clear();
                    AudioSource audioSource = AudioController.Instance.PlaySound2D("camera_flash_gameover", MixerGroup.None, 0.85f);
                    audioSource.gameObject.name = "flashSound";
                    UnityEngine.Object.DontDestroyOnLoad(audioSource.gameObject);
                    AudioController.Instance.StopAllLoops();
                    Singleton<UIManager>.Instance.Effects.GetEffect<ScreenColorEffect>().SetColor(GameColors.Instance.nearWhite);
                    Singleton<UIManager>.Instance.Effects.GetEffect<ScreenColorEffect>().SetIntensity(1f, 100f);
                    yield return new WaitForSeconds(1f);
                    AsyncOperation asyncOp = SceneLoader.StartAsyncLoad("Part1_Cabin");
                    SceneLoader.CompleteAsyncLoad(asyncOp);
                    SaveManager.SaveToFile(saveActiveScene: false);
                }
                //}
            }

            private static IEnumerator StepSelect(DeathCardCreationSequencer __instance, IntRef i)
            {
                yield return CreateStepCard(__instance, i);
            }

            private static IEnumerator CardCreationStep(int i, List<int> prevSelects, DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                CardInfo inf = preview.Info;
                switch (i)
                {
                    case 1:
                        // Cost
                        yield return CreateCostCard(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 2:
                        // Extra Cost
                        yield return CreateCostCardAdditive(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 3:
                        // Attack
                        yield return CreateAttackCard(__instance, preview);
                        preview.RenderInfo.hiddenAttack = false;
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 4:
                        // Health
                        yield return CreateHealthCard(__instance, preview);
                        preview.RenderInfo.hiddenHealth = false;
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 5:
                        // Sigils
                        if (prevSelects.Contains(5))
                        {
                            inf.abilities = new List<Ability>();
                            inf.evolveParams = null;
                            inf.iceCubeParams = null;
                        }
                        yield return CreateSigilCard(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        if (preview.Info.Abilities.Contains(Ability.Evolve) || preview.Info.Abilities.Contains(Ability.Transformer))
                        {
                            // Evolution
                            yield return CreateEvolution(__instance, preview, 0);
                            preview.Anim.PlayTransformAnimation();
                            yield return new WaitForSeconds(0.15f);
                            preview.SetInfo(preview.Info);
                            yield return new WaitForSeconds(0.25f);
                            if (preview.Info.evolveParams != null)
                            {
                                yield return CreateEvolutionTurnCount(__instance, preview);
                                preview.Anim.PlayTransformAnimation();
                                yield return new WaitForSeconds(0.15f);
                                preview.SetInfo(preview.Info);
                                yield return new WaitForSeconds(0.25f);
                            }
                        }
                        if (preview.Info.Abilities.Contains(Ability.IceCube))
                        {
                            // Ice cube
                            yield return CreateEvolution(__instance, preview, 1);
                            preview.Anim.PlayTransformAnimation();
                            yield return new WaitForSeconds(0.15f);
                            preview.SetInfo(preview.Info);
                            yield return new WaitForSeconds(0.25f);
                        }
                        break;
                    case 6:
                        // Tribe
                        if (prevSelects.Contains(6))
                        {
                            inf.tribes = new List<Tribe>();
                        }
                        yield return CreateTribeCard(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 7:
                        // Special Abilities
                        yield return CreateSpecialAbilities(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 8:
                        List<Texture2D> textures = PortraitLoader.Instance.GetPortraits();
                        // Portrait
                        yield return CreatePortrait(__instance, preview, textures, 0);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 9:
                        // Rarity
                        if (prevSelects.Contains(9))
                        {
                            inf.appearanceBehaviour = new List<CardAppearanceBehaviour.Appearance>();
                            inf.metaCategories = new List<CardMetaCategory>();
                            preview.Anim.PlayTransformAnimation();
                            yield return new WaitForSeconds(0.15f);
                            preview.SetInfo(preview.Info);
                            preview.UpdateAppearanceBehaviours();
                            yield return new WaitForSeconds(0.25f);
                        }
                        yield return CreateRarity(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                    break;
                    case 10:
                        // Complexity
                        yield return CreateComplexity(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    default:
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 11:
                        List<Texture2D> textures2 = PortraitLoader.Instance.GetPortraits();
                        // Portrait
                        yield return CreatePortrait(__instance, preview, textures2, 1);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    case 12:
                        // DECALS
                        break;
                }
                if (!prevSelects.Contains(i)) prevSelects.Add(i);
            }

            private static IEnumerator EnterDescription(KeyboardInputHandler ___keyboardInput, SelectableCard preview)
            {
                ___keyboardInput.Reset();
                while (!___keyboardInput.EnteredInput)
                {
                    string text = ___keyboardInput.KeyboardInput;
                    if (text != "")
                    {
                        yield return Singleton<TextDisplayer>.Instance.ShowMessage(text);
                    }
                    else
                    {
                        yield return Singleton<TextDisplayer>.Instance.ShowMessage("Please type the description.");
                    }
                    yield return new WaitForEndOfFrame();
                    preview.Info.description = text;
                }
            }

            
        }
    }

    public class IntRef
    {
        public int Value;
    }
}