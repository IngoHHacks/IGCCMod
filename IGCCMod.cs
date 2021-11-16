using BepInEx;
using BepInEx.Logging;
using DiskCardGame;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace IGCCMod
{

    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PluginGuid = "IngoH.inscryption.IGCCMod";
        private const string PluginName = "IGCCMod";
        private const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Logger.LogInfo($"Loaded {PluginName}!");
            Plugin.Log = base.Logger;

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
                if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss) return false;
                else return true;
            }
        }

        [HarmonyPatch(typeof(SanctumSceneSequencer), "IntroSequence")]
        public class PostSanctumIntroPatch : SanctumSceneSequencer
        {
            public static IEnumerator Postfix(IEnumerator __result, SanctumSceneSequencer __instance)
            {
                if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss)
                {
                    // Abbreviated version of original method.
                    __instance.GetType().GetMethod("StartHumLoop", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                    __instance.GetType().GetMethod("InitializeLeshy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                    Singleton<ViewManager>.Instance.SwitchToView(View.SanctumFloorDown, immediate: true, lockAfter: true);
                    yield return new WaitForSeconds(1.5f);
                    yield return Singleton<UIManager>.Instance.Effects.GetEffect<EyelidMaskEffect>().Blink(delegate
                    {
                        Singleton<CameraEffects>.Instance.TweenBlur(0f, 3f);
                    });
                    LookUp();
                    Singleton<InteractionCursor>.Instance.SetHidden(hidden: false);
                    yield return new WaitForSeconds(0.5f);
                    yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("You entered the [c:bR]card creation[c:] mode.");
                    yield return __instance.GetType().GetMethod("DeathCardSequence", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                }
            }
        }

        private static void LookDown()
        {
            bool skipMove = false;
            if (Singleton<ViewManager>.Instance.CurrentView == View.SanctumFloorDown) skipMove = true;
            Singleton<ViewManager>.Instance.SwitchToView(View.SanctumFloorDown, immediate: false, lockAfter: true);
            if (!skipMove)
            {
                Vector3 pos = Singleton<ViewManager>.Instance.CameraParent.localPosition;
                Pixelplacement.Tween.LocalPosition(Singleton<ViewManager>.Instance.CameraParent, new Vector3(pos.x, pos.y + 3.25f, pos.z + 4.25f), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
            }
        }

        private static void LookUp()
        {
            Singleton<ViewManager>.Instance.SwitchToView(View.SanctumFloorUp, immediate: false, lockAfter: true);
        }

        [HarmonyPatch(typeof(SanctumSceneSequencer), "DeathCardSequence", null)]
        public class DeathCardStartPatch : SanctumSceneSequencer
        {
            public static bool Prefix(SanctumSceneSequencer __instance)
            {
                // Cancel orignal method
                if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss) return false;
                else return true;
            }
        }

        [HarmonyPatch(typeof(SanctumSceneSequencer), "DeathCardSequence", null)]
        public class PostDeathCardStartPatch : SanctumSceneSequencer
        {
            public static IEnumerator Postfix(IEnumerator __result, SanctumSceneSequencer __instance, DeathCardCreationSequencer ___deathCardSequencer)
            {
                if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss)
                {
                    yield return new WaitForSeconds(0.25f);
                    yield return ___deathCardSequencer.CreateCardSequence(true);
                }
            }
        }


        [HarmonyPatch(typeof(DeathCardCreationSequencer), "CreateCardSequence", new Type[] { typeof(bool) })]
        public class DeathCardPatch : DeathCardCreationSequencer
        {
            public static bool Prefix(DeathCardCreationSequencer __instance)
            {
                // Cancel orignal method
                if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss) return false;
                else return true;
            }
        }

        [HarmonyPatch(typeof(DeathCardCreationSequencer), "CreateCardSequence", new Type[] { typeof(bool) })]
        public class PostDeathCardPatch : DeathCardCreationSequencer
        {
            public static IEnumerator Postfix(IEnumerator __result, DeathCardCreationSequencer __instance, KeyboardInputHandler ___keyboardInput)
            {
                if (SaveManager.SaveFile.currentRun.causeOfDeath.opponentType == Opponent.Type.PixelP03FinaleBoss)
                {
                    Boolean done = false;
                    while (!done)
                    {
                        // Create preview card
                        Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                        Vector3 vector = GetCardIndexLoc(obj, 17);
                        CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                        CardModificationInfo addTo = new CardModificationInfo();
                        addTo.nameReplacement = "current card";
                        c.Mods.Add(addTo);
                        SelectableCard preview = CreatePreviewCard(__instance, vector, c);
                        // Cost
                        yield return CreateCostCard(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        // Attack
                        yield return CreateAttackCard(__instance, preview);
                        preview.RenderInfo.hiddenAttack = false;
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        // Health
                        yield return CreateHealthCard(__instance, preview);
                        preview.RenderInfo.hiddenHealth = false;
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        // Sigils
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
                        // Tribe
                        yield return CreateTribeCard(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        // Special Abilities
                        yield return CreateSpecialAbilities(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        // Load textures from cards
                        // TODO: Remove duplicates
                        Texture2D[] textures = (Texture2D[])Resources.FindObjectsOfTypeAll(typeof(Texture2D));
                        List<Texture2D> validTextures = new List<Texture2D>();
                        int baseCount = 0;
                        foreach (Texture2D texture in textures)
                        {
                            if (texture.width == 114 && texture.height == 94 && texture.name.StartsWith("portrait") && !texture.name.EndsWith("_emission"))
                            {
                                validTextures.Add(texture);
                                baseCount++;
                            }
                        }
                        int modCount = 0;
                        foreach (string file in Directory.GetFiles(Paths.PluginPath, "*.png", SearchOption.AllDirectories))
                        {

                            byte[] pngBytes = System.IO.File.ReadAllBytes(file);
                            Texture2D texture = new Texture2D(2, 2);
                            ImageConversion.LoadImage(texture, pngBytes);
                            texture.name = file;
                            if (texture.width == 114 && texture.height == 94 && !texture.name.EndsWith("_emission"))
                            {
                                validTextures.Add(texture);
                                modCount++;
                            }
                        }
                        // Portrait
                        yield return CreatePortrait(__instance, preview, validTextures, baseCount, modCount);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        // Rarity
                        yield return CreateRarity(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        // Complexity
                        yield return CreateComplexity(__instance, preview);
                        preview.Anim.PlayTransformAnimation();
                        yield return new WaitForSeconds(0.15f);
                        preview.SetInfo(preview.Info);
                        yield return new WaitForSeconds(0.25f);
                        // Move to center
                        Vector3 vector2 = GetCardIndexLoc(obj, 7);
                        Pixelplacement.Tween.Position(preview.transform, vector2, 0.25f, 0f);

                        // Enter name
                        ___keyboardInput.maxInputLength = 255;
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

            // TODO: Merge all duplicate code in CreateCard methods
            /// <see cref="CreateCard"/>
            private static SelectableCard CreatePreviewCard(DeathCardCreationSequencer __instance, Vector3 vector, CardInfo info)
            {
                SelectableCard selectableCard = (SelectableCard)__instance.GetType().GetMethod("SpawnCard", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { __instance.transform });
                selectableCard.RenderInfo.hiddenAttack = true;
                selectableCard.RenderInfo.hiddenHealth = true;
                selectableCard.gameObject.SetActive(value: true);
                selectableCard.SetInfo(info);
                selectableCard.SetInteractionEnabled(interactionEnabled: false);
                selectableCard.SetEnabled(enabled: false);
                selectableCard.transform.position = vector + Vector3.up * 2f + ((Vector3)__instance.GetType().GetField("THROW_CARDS_FROM", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance)) * 2f;
                selectableCard.transform.eulerAngles = new Vector3(90, 0, 0);
                Pixelplacement.Tween.Position(selectableCard.transform, vector, 0.1f, 0f);
                return selectableCard;
            }

            // TODO: Merge all duplicate code in Create[Info] methods
            /// <see cref="CreatePortrait"/>
            private static IEnumerator FinalizeCard(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                LookDown();
                SelectableCard selectedCard = null;
                List<CardInfo> choices = new List<CardInfo>();
                List<SelectableCard> cards = new List<SelectableCard>();
                Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                for (int i = 0; i <= 3; i++)
                {
                    Vector3 vector = GetCardIndexLoc(obj, i + 10);
                    CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                    CardModificationInfo addTo = new CardModificationInfo();
                    switch (i)
                    {
                        case 0:
                            addTo.nameReplacement = "export and quit";
                            break;
                        case 1:
                            addTo.nameReplacement = "export and create another";
                            break;
                        case 2:
                            addTo.nameReplacement = "quit without exporting";
                            break;
                        case 3:
                            addTo.nameReplacement = "create another without exporting";
                            break;
                    }
                    c.Mods.Add(addTo);
                    choices.Add(c);
                    SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
                    selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                    {
                        selectedCard = (SelectableCard)c2;
                    });
                }
                Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                Singleton<TextDisplayer>.Instance.ShowMessage("What should I do with this card?");
                foreach (SelectableCard item in cards)
                {
                    item.SetInteractionEnabled(interactionEnabled: true);
                    item.SetEnabled(enabled: true);
                }
                yield return new WaitUntil(() => selectedCard != null);
                Singleton<TextDisplayer>.Instance.Clear();
                if (selectedCard.Info.DisplayedNameEnglish.Contains("export "))
                {
                    // TODO: Improve JSON parser
                    yield return Singleton<TextDisplayer>.Instance.ShowMessage("Exporting card...");
                    string name = "IGCC_" + Regex.Replace(preview.Info.DisplayedNameEnglish.Replace(" ", "_"), "\\W", "_");
                    string json = "{\r\n";
                    json += GetJsonFromString("name", name) + ",\r\n";
                    json += GetJsonFromString("displayedName", preview.Info.DisplayedNameEnglish.Replace("\"", "'")) + ",\r\n";
                    json += GetJsonFromString("description", preview.Info.description) + ",\r\n";
                    if (preview.Info.metaCategories.Count > 0) json += GetJsonFromStringList("metaCategories", preview.Info.metaCategories) + ",\r\n";
                    json += GetJsonFromString("cardComplexity", preview.Info.cardComplexity.ToString()) + ",\r\n";
                    json += GetJsonFromString("temple", "Nature") + ",\r\n";
                    json += GetJsonFromInt("baseAttack", preview.Info.Attack) + ",\r\n";
                    json += GetJsonFromInt("baseHealth", preview.Info.Health) + ",\r\n";
                    if (preview.Info.BloodCost > 0 || (preview.Info.BonesCost == 0 && preview.Info.EnergyCost == 0)) json += GetJsonFromInt("cost", preview.Info.BloodCost) + ",\r\n";
                    if (preview.Info.BonesCost > 0) json += GetJsonFromInt("bonesCost", preview.Info.BonesCost) + ",\r\n";
                    if (preview.Info.EnergyCost > 0) json += GetJsonFromInt("energyCost", preview.Info.EnergyCost) + ",\r\n";
                    if (preview.Info.GemsCost.Count > 0) json += GetJsonFromStringList("gemsCost", preview.Info.GemsCost) + ",\r\n";
                    json += GetJsonFromStringList("tribes", preview.Info.tribes) + ",\r\n";
                    if (preview.Info.Abilities.Count > 0) json += GetJsonFromAbilityList(preview.Info.Abilities);
                    if (preview.Info.appearanceBehaviour.Count > 0) json += GetJsonFromStringList("appearanceBehavior", preview.Info.appearanceBehaviour) + ",\r\n";
                    if (preview.Info.SpecialStatIcon != SpecialStatIcon.None) json += GetJsonFromString("specialStatIcon", preview.Info.SpecialStatIcon.ToString()) + ",\r\n";
                    if (preview.Info.SpecialAbilities.Count > 0) json += GetJsonFromStringList("specialAbilities", preview.Info.SpecialAbilities) + ",\r\n";
                    if (preview.Info.traits.Count > 0) json += GetJsonFromStringList("traits", preview.Info.traits) + ",\r\n";
                    if (preview.Info.iceCubeParams != null)
                    {
                        json += "\"iceCube\": {\r\n";
                        json += "  " + GetJsonFromString("creatureWithin", preview.Info.iceCubeParams.creatureWithin.name) + "\r\n";
                        json += "},\r\n";
                    }
                    if (preview.Info.evolveParams != null)
                    {
                        json += "  \"evolution\": {\r\n";
                        json += "  " + GetJsonFromString("name", preview.Info.evolveParams.evolution.name) + ",\r\n";
                        json += "  " + GetJsonFromInt("turnsToEvolve", preview.Info.evolveParams.turnsToEvolve) + "\r\n";
                        json += "  },\r\n";
                    }
                    json += GetJsonFromString("texture", name + ".png");
                    json += "\r\n}";
                    System.IO.Directory.CreateDirectory(Paths.PluginPath + "/JSONLoader/Cards");
                    System.IO.File.WriteAllText(Paths.PluginPath + "/JSONLoader/Cards/" + name + ".json", json);
                    System.IO.Directory.CreateDirectory(Paths.PluginPath + "/JSONLoader/Artwork");
                    System.IO.File.WriteAllBytes(Paths.PluginPath + "/JSONLoader/Artwork/" + name + ".png", CloneTextureReadable(preview.Info.portraitTex.texture).EncodeToPNG());
                    JLPlugin.Data.CardData card = JLPlugin.Data.CardData.CreateFromJSON(json);
                    card.GenerateNew();
                    List<CardInfo> official = ScriptableObjectLoader<CardInfo>.AllData;
                    string orig = preview.Info.name;
                    APIPlugin.NewCard.cards[APIPlugin.NewCard.cards.Count - 1].evolveParams = preview.Info.evolveParams;
                    APIPlugin.NewCard.cards[APIPlugin.NewCard.cards.Count - 1].iceCubeParams = preview.Info.iceCubeParams;
                    if (preview.Info.metaCategories.Contains(CardMetaCategory.ChoiceNode) || preview.Info.metaCategories.Contains(CardMetaCategory.Rare))
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            official.Add(APIPlugin.NewCard.cards[APIPlugin.NewCard.cards.Count - 1]);
                        }
                        SaveManager.SaveFile.currentRun.playerDeck.AddCard(APIPlugin.NewCard.cards[APIPlugin.NewCard.cards.Count - 1]);
                        yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("The card has been created and added to your deck. It will also appear 10 times as often until the game is restarted.");
                    }
                    else
                    {
                        official.Add(APIPlugin.NewCard.cards[APIPlugin.NewCard.cards.Count - 1]);
                        yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("The card has been created.");
                    }
                    
                }
                if (selectedCard.Info.DisplayedNameEnglish.Contains("quit"))
                {
                    preview.Anim.PlayDeathAnimation();
                    UnityEngine.Object.Destroy(preview.gameObject, 2f);
                    DestroyAllCards(cards, true);
                    yield return new WaitForSeconds(0.5f);
                } else
                {
                    DestroyAllCards(cards, true);
                }
                
            }

            private static Texture2D CloneTextureReadable(Texture2D source)
            {
                RenderTexture renderTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(source, renderTex);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTex;
                Texture2D readableText = new Texture2D(source.width, source.height);
                readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                readableText.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTex);
                return readableText;
            }

            private static string GetJsonFromString(string key, string value)
            {
                return "  " + "\"" + key + "\": " + "\"" + value + "\"";
            }

            private static string GetJsonFromInt(string key, int value)
            {
                return "  " + "\"" + key + "\": " + value;
            }

            private static string GetJsonFromStringList<T>(string key, List<T> values)
            {
                return "  " + "\"" + key + "\": " + "[" + ParseListJson(values) + "]";
            }

            private static string GetJsonFromAbilityList(List<Ability> values)
            {
                string all = "";
                List <Ability> baseAbilities = new List<Ability>();
                List <Ability> modAbilites = new List<Ability>();
                foreach (Ability ability in values)
                {
                    if ((int)ability > 99) baseAbilities.Add(ability);
                    else modAbilites.Add(ability);
                }
                if (baseAbilities.Count > 0) all += "  " + "\"abilities\": [" + GetJsonFromAbilityList(baseAbilities) + "],\r\n";
                if (baseAbilities.Count > 0) all += "  " + "\"customAbilities\": [" + GetJsonFromAbilityList(baseAbilities) + "  ],\r\n";
                return all;
            }

            private static string GetJsonFromStringArray(string key, string[] values)
            {
                return "  " + "\"" + key + "\": " + "[" + ParseArrayJson(values) + "]";
            }

            private static string ParseListJson<T>(List<T> values)
            {
                string initial = "";
                foreach (T value in values)
                {
                    if (initial != "") initial += ",";
                    initial += " \"" + value + "\"";
                }
                return initial + " ";
            }

            private static string ParseListAbilityJson(List<Ability> values)
            {
                string initial = "";
                foreach (Ability value in values)
                {
                    if (initial != "") initial += ",";
                    initial += " \"" + value + "\"";
                }
                return initial + " ";
            }

            private static string ParseListModAbilityJson(List<Ability> values)
            {
                // guid and name are private
                string initial = "\r\n";
                foreach (Ability value in values)
                {
                    if (initial != "\r\n") initial += ",\r\n";
                    APIPlugin.AbilityIdentifier id = APIPlugin.NewAbility.abilities[(int)value - 100].id;
                    string guid = (string)id.GetType().GetField("guid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(id);
                    string name = (string)id.GetType().GetField("name", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(id);
                    initial += "    {\r\n";
                    initial += "    " + GetJsonFromString("name", name) + ",\r\n";
                    initial += "    " + GetJsonFromString("GUID", guid) + "\r\n";
                    initial += "    }\r\n";
                }
                return initial;
            }

            private static string ParseArrayJson(string[] values)
            {
                string initial = "";
                foreach (string value in values)
                {
                    if (initial != "") initial += ",";
                    initial += " \"" + value + "\"";
                }
                return initial + " ";
            }

            /// <see cref="CreatePortrait"/>
            private static IEnumerator CreateRarity(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                LookDown();
                SelectableCard selectedCard = null;
                List<CardInfo> choices = new List<CardInfo>();
                List<SelectableCard> cards = new List<SelectableCard>();
                Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                
                for (int i = 0; i <= 2; i++)
                {
                    Vector3 vector = GetCardIndexLoc(obj, i + 6);
                    CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                    CardModificationInfo addTo = new CardModificationInfo();
                    switch (i)
                    {
                        case 0:
                            {
                                addTo.nameReplacement = "regular";
                                List<CardMetaCategory> cats = new List<CardMetaCategory>
                                {
                                    CardMetaCategory.ChoiceNode,
                                    CardMetaCategory.TraderOffer
                                };
                                c.metaCategories = cats;
                                break;
                            }

                        case 1:
                            {
                                addTo.nameReplacement = "rare";
                                List<CardMetaCategory> cats = new List<CardMetaCategory>
                                {
                                    CardMetaCategory.Rare
                                };
                                c.metaCategories = cats;
                                c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.RareCardBackground);
                                break;
                            }

                        case 2:
                            addTo.nameReplacement = "hidden";
                            break;
                    }
                    c.Mods.Add(addTo);
                    choices.Add(c);
                    SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
                    selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                    {
                        selectedCard = (SelectableCard)c2;
                    });
                }
                Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                Singleton<TextDisplayer>.Instance.ShowMessage("Choose the [c:bR]type[c:] of card.");
                foreach (SelectableCard item in cards)
                {
                    item.SetInteractionEnabled(interactionEnabled: true);
                    item.SetEnabled(enabled: true);
                }
                yield return new WaitUntil(() => selectedCard != null);
                Singleton<TextDisplayer>.Instance.Clear();
                preview.Info.metaCategories = selectedCard.Info.metaCategories;
                preview.Info.appearanceBehaviour = selectedCard.Info.appearanceBehaviour;
                yield return PostCardSelect(cards, selectedCard, 4);
            }

            /// <see cref="CreatePortrait"/>
            private static IEnumerator CreateComplexity(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                LookDown();
                SelectableCard selectedCard = null;
                List<CardInfo> choices = new List<CardInfo>();
                List<SelectableCard> cards = new List<SelectableCard>();
                Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                for (int i = 0; i <= 3; i++)
                {
                    Vector3 vector = GetCardIndexLoc(obj, i + 5);
                    CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                    CardModificationInfo addTo = new CardModificationInfo();
                    addTo.nameReplacement = ((CardComplexity)i).ToString();
                    c.cardComplexity = ((CardComplexity)i);
                    c.Mods.Add(addTo);
                    choices.Add(c);
                    SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
                    selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                    {
                        selectedCard = (SelectableCard)c2;
                    });
                }
                Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                Singleton<TextDisplayer>.Instance.ShowMessage("Choose the [c:bR]complexity[c:] of card.");
                foreach (SelectableCard item in cards)
                {
                    item.SetInteractionEnabled(interactionEnabled: true);
                    item.SetEnabled(enabled: true);
                }
                yield return new WaitUntil(() => selectedCard != null);
                Singleton<TextDisplayer>.Instance.Clear();
                preview.Info.cardComplexity = selectedCard.Info.cardComplexity;
                yield return PostCardSelect(cards, selectedCard, 5);
            }

            /// <see cref="CreatePortrait"/>
            private static IEnumerator CreateCostCard(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                bool valid = false;
                int page = 0;
                SelectableCard selectedCard = null;
                List<SelectableCard> cards = new List<SelectableCard>();
                while (!valid)
                {
                    selectedCard = null;
                    cards.Clear();
                    LookDown();
                    List<CardInfo> choices = new List<CardInfo>();
                    Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                    for (int i = 0; i <= 16; i++)
                    {
                        if (page * 15 + i <= 23 || i == 15)
                        {
                            if (i <= 14 || (i == 15 && page > 0) || (i == 16 && page < 1))
                            {
                                Vector3 vector = GetCardIndexLoc(obj, i);
                                CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                                CardModificationInfo addTo = new CardModificationInfo();
                                if (i <= 14)
                                {
                                    if (page * 15 + i >= 21)
                                    {
                                        List<GemType> gems = new List<GemType>()
                                        {
                                            (GemType)(page * 15 + i - 21)
                                        };
                                        addTo.addGemCost = gems;
                                        addTo.nameReplacement = gems[0].ToString() + " gem";
                                    }
                                    else if (page * 15 + i >= 15)
                                    {
                                        addTo.energyCostAdjustment = (page * 15 + i - 14);
                                        addTo.nameReplacement = (page * 15 + i - 14) + " energy";
                                    }
                                    else if (page * 15 + i >= 5)
                                    {
                                        addTo.bonesCostAdjustment = (page * 15 + i - 4);
                                        if (page * 15 + i - 6 == 1) addTo.nameReplacement = (page * 15 + i - 6) + " bone";
                                        else addTo.nameReplacement = (page * 15 + i - 4) + " bones";
                                    }
                                    else if (page * 15 + i >= 1)
                                    {
                                        addTo.bloodCostAdjustment = (page * 15 + i);
                                        addTo.nameReplacement = (page * 15 + i) + " blood";
                                    }
                                    else
                                    {
                                        addTo.nameReplacement = "free";
                                    }
                                }
                                else
                                {
                                    if (i == 15) addTo.nameReplacement = "previous page";
                                    else if (i == 16) addTo.nameReplacement = "next page";
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainBackground);
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainLayout);
                                }
                                c.Mods.Add(addTo);
                                choices.Add(c);
                                SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
                                selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                                {
                                    selectedCard = (SelectableCard)c2;
                                });
                            }
                            else
                            {
                                choices.Add(null);
                            }
                        }
                        else
                        {
                            choices.Add(null);
                        }
                    }
                    Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                    Singleton<TextDisplayer>.Instance.ShowMessage("Please, choose a card to draw the [c:bR]cost[c:] from.");
                    foreach (SelectableCard item in cards)
                    {
                        item.SetInteractionEnabled(interactionEnabled: true);
                        item.SetEnabled(enabled: true);
                    }
                    yield return new WaitUntil(() => selectedCard != null);
                    if (selectedCard.Info.DisplayedNameEnglish.Equals("previous page"))
                    {
                        if (page > 0) page--;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("next page"))
                    {
                        if (page < 1) page++;
                        DestroyAllCards(cards, true);
                    }
                    else
                    {
                        valid = true;
                    }
                }
                Singleton<TextDisplayer>.Instance.Clear();
                CardModificationInfo addTo2 = new CardModificationInfo();
                if (selectedCard.Info.BloodCost > 0) addTo2.bloodCostAdjustment = selectedCard.Info.BloodCost;
                else if (selectedCard.Info.BonesCost > 0) addTo2.bonesCostAdjustment = selectedCard.Info.BonesCost;
                else if (selectedCard.Info.EnergyCost > 0) addTo2.energyCostAdjustment = selectedCard.Info.EnergyCost;
                else if (selectedCard.Info.GemsCost.Count > 0) addTo2.addGemCost = selectedCard.Info.GemsCost;
                preview.Info.Mods.Add(addTo2);
                yield return PostCardSelect(cards, selectedCard, 2);
            }

            private static IEnumerator CreatePortrait(DeathCardCreationSequencer __instance, SelectableCard preview, List<Texture2D> portraits, int baseCount, int modCount)
            {
                bool valid = false;
                int page = 0;
                SelectableCard selectedCard = null;
                List<SelectableCard> cards = new List<SelectableCard>();
                yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("I found [c:bR]" + baseCount + " loaded portraits[c:] and [c:bR]" + modCount + " portrait files[c:].");
                // Until a non-page card is selected
                while (!valid)
                {
                    selectedCard = null;
                    cards.Clear();
                    // Look at floor
                    LookDown();

                    List<CardInfo> choices = new List<CardInfo>();
                    // Cards are spawned relative to the first death card marker
                    Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                    
                    for (int i = 0; i <= 16; i++)
                    {
                        if (page * 15 + i < portraits.Count || i == 15)
                        {
                            if (i <= 14 || (i == 15 && page > 0) || (i == 16 && page < Math.Ceiling(portraits.Count / 15.0f)))
                            {
                                // Place card at location 'i'
                                Vector3 vector = GetCardIndexLoc(obj, i);
                                CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                                CardModificationInfo addTo = new CardModificationInfo();
                                if (i <= 14)
                                {
                                    // Set portrait
                                    c.portraitTex = Sprite.Create(portraits[page * 15 + i], new Rect(0.0f, 0.0f, 114.0f, 94.0f), new Vector2(0.5f, 0.5f));
                                    // Set name
                                    string name = "";
                                    if (portraits[page * 15 + i].name.StartsWith("portrait_")) name = portraits[page * 15 + i].name.Substring(9).Replace("_", " ");
                                    else name = portraits[page * 15 + i].name.Replace("_", " ");
                                    if (name.Contains(Path.DirectorySeparatorChar.ToString())) addTo.nameReplacement = name.Substring(name.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                                    else addTo.nameReplacement = name;
                                }
                                else
                                {
                                    // Page cards
                                    if (i == 15) addTo.nameReplacement = "previous page";
                                    else if (i == 16) addTo.nameReplacement = "next page";
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainBackground);
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainLayout);
                                }
                                c.Mods.Add(addTo);
                                choices.Add(c);
                                // Create card
                                SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
                                // Mouse listener
                                selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                                {
                                    selectedCard = (SelectableCard)c2;
                                });
                            }
                            else
                            {
                                // If page card should not exist
                                choices.Add(null);
                            }
                        }
                        else
                        {
                            // If card id >= card count
                            choices.Add(null);
                        }
                    }
                    // Look to the right
                    Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(75, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                    Singleton<TextDisplayer>.Instance.ShowMessage("Choose one.");
                    // Enable cards to be selected
                    foreach (SelectableCard item in cards)
                    {
                        item.SetInteractionEnabled(interactionEnabled: true);
                        item.SetEnabled(enabled: true);
                    }
                    yield return new WaitUntil(() => selectedCard != null);
                    if (selectedCard.Info.DisplayedNameEnglish.Equals("previous page"))
                    {
                        if (page > 0) page--;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("next page"))
                    {
                        if (page < Math.Ceiling(portraits.Count / 15.0f)) page++;
                        DestroyAllCards(cards, true);
                    }
                    else
                    {
                        // Non-page card selected
                        valid = true;
                    }
                }
                Singleton<TextDisplayer>.Instance.Clear();
                // Copy portrait to preview card
                preview.Info.portraitTex = Sprite.Create(selectedCard.Info.portraitTex.texture, new Rect(0.0f, 0.0f, 114.0f, 94.0f), new Vector2(0.5f, 0.5f));
                // Leshy comment about the choice
                yield return PostCardSelect(cards, selectedCard, 3);
            }

            /// <see cref="CreatePortrait"/>
            private static IEnumerator CreateEvolution(DeathCardCreationSequencer __instance, SelectableCard preview, int type)
            {
                bool valid = false;
                int page = 0;
                SelectableCard selectedCard = null;
                List<SelectableCard> cards = new List<SelectableCard>();
                List<CardInfo> allCards = ScriptableObjectLoader<CardInfo>.AllData;
                List<CardInfo> validCards = new List<CardInfo>();
                foreach (CardInfo card in allCards)
                {
                    if (card.temple == CardTemple.Nature)
                    {
                        if (!validCards.Contains(card)) validCards.Add(card);
                    }
                }
                while (!valid)
                {
                    selectedCard = null;
                    cards.Clear();
                    LookDown();
                    List<CardInfo> choices = new List<CardInfo>();
                    Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                    for (int i = 0; i <= 16; i++)
                    {
                        if (page * 15 + i < validCards.Count + 1 || i == 15)
                        {
                            if (i <= 14 || (i == 15 && page > 0) || (i == 16 && page < Math.Ceiling(validCards.Count + 1 / 15.0f)))
                            {
                                Vector3 vector = GetCardIndexLoc(obj, i);
                                CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                                CardModificationInfo addTo = new CardModificationInfo();
                                if (i <= 14)
                                {
                                    if (page * 15 + i == 0) {
                                        if (type == 0) addTo.nameReplacement = "default (+1/2)";
                                        else if (type == 1) addTo.nameReplacement = "default (opposum)";
                                    }
                                    else c = validCards[page * 15 + i - 1];
                                }
                                else
                                {
                                    if (i == 15) addTo.nameReplacement = "previous page";
                                    else if (i == 16) addTo.nameReplacement = "next page";
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainBackground);
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainLayout);
                                }
                                c.Mods.Add(addTo);
                                choices.Add(c);
                                SelectableCard selectableCard;
                                if (page * 15 + i == 0) selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
                                else selectableCard = CreateCard(__instance, choices, cards, vector, false, false, false);
                                selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                                {
                                    selectedCard = (SelectableCard)c2;
                                });
                            }
                            else
                            {
                                choices.Add(null);
                            }
                        }
                        else
                        {
                            choices.Add(null);
                        }
                    }
                    Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                    if (type == 0) Singleton<TextDisplayer>.Instance.ShowMessage("Choose which card it will evolve into.");
                    else if (type == 1) Singleton<TextDisplayer>.Instance.ShowMessage("Choose which card it will create upon death.");
                    foreach (SelectableCard item in cards)
                    {
                        item.SetInteractionEnabled(interactionEnabled: true);
                        item.SetEnabled(enabled: true);
                    }
                    yield return new WaitUntil(() => selectedCard != null);
                    if (selectedCard.Info.DisplayedNameEnglish.Equals("previous page"))
                    {
                        if (page > 0) page--;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("next page"))
                    {
                        if (page < Math.Ceiling((validCards.Count + 1) / 15.0f)) page++;
                        DestroyAllCards(cards, true);
                    }
                    else
                    {
                        valid = true;
                    }
                }
                Singleton<TextDisplayer>.Instance.Clear();
                // TODO: Improve logic in case someone creates a card named 'default (+1/2)' or 'default (opposum)'
                if (!selectedCard.Info.DisplayedNameEnglish.Equals("default (+1/2)") && !selectedCard.Info.DisplayedNameEnglish.Equals("default (opposum)"))
                {
                    if (type == 0)
                    {
                        preview.Info.evolveParams = new EvolveParams();
                        preview.Info.evolveParams.evolution = selectedCard.Info;
                        preview.Info.evolveParams.turnsToEvolve = 1;
                    }
                    else if (type == 1)
                    {
                        preview.Info.iceCubeParams = new IceCubeParams();
                        preview.Info.iceCubeParams.creatureWithin = selectedCard.Info;
                    }
                }
                yield return PostCardSelect(cards, selectedCard, 6);
            }

            /// <see cref="CreatePortrait"/>
            private static IEnumerator CreateEvolutionTurnCount(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                LookDown();
                SelectableCard selectedCard = null;
                List<CardInfo> choices = new List<CardInfo>();
                List<SelectableCard> cards = new List<SelectableCard>();
                Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                for (int i = 0; i <= 9; i++)
                {
                    Vector3 vector = GetCardIndexLoc(obj, i);
                    CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                    CardModificationInfo addTo = new CardModificationInfo();
                    if (i == 0) addTo.nameReplacement = "1 turn";
                    else addTo.nameReplacement = i + 1 + " turns";
                    addTo.abilities.Add(Ability.Evolve);
                    c.Mods.Add(addTo);
                    c.evolveParams = new EvolveParams();
                    c.evolveParams.turnsToEvolve = i + 1;
                    choices.Add(c);
                    SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
                    selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                    {
                        selectedCard = (SelectableCard)c2;
                    });
                }
                Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                Singleton<TextDisplayer>.Instance.ShowMessage("Please, choose a card to draw the [c:bR]cost[c:] from.");
                foreach (SelectableCard item in cards)
                {
                    item.SetInteractionEnabled(interactionEnabled: true);
                    item.SetEnabled(enabled: true);
                }
                yield return new WaitUntil(() => selectedCard != null);
                Singleton<TextDisplayer>.Instance.Clear();
                preview.Info.evolveParams.turnsToEvolve = selectedCard.Info.evolveParams.turnsToEvolve;
                yield return PostCardSelect(cards, selectedCard, 7);
            }

            /// <see cref="CreatePortrait"/>
            private static IEnumerator CreateAttackCard(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                bool valid = false;
                int page = 0;
                SelectableCard selectedCard = null;
                List<SelectableCard> cards = new List<SelectableCard>();
                int specialStatIcons = (int)SpecialStatIcon.NUM_ICONS - 1;
                while (!valid)
                {
                    selectedCard = null;
                    cards.Clear();
                    LookDown();
                    List<CardInfo> choices = new List<CardInfo>();
                    Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                    for (int i = 0; i <= 16; i++)
                    {
                        if (page * 15 + i <= 9999 + specialStatIcons || i == 15)
                        {
                            if (i <= 14 || (i == 15 && page > 0) || (i == 16 && page < Math.Ceiling((9999 + specialStatIcons) / 15.0f)))
                            {
                                Vector3 vector = GetCardIndexLoc(obj, i);
                                CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                                CardModificationInfo addTo = new CardModificationInfo();
                                if (page * 15 + i < specialStatIcons)
                                {
                                    addTo.nameReplacement = ((SpecialStatIcon)i + 1).ToString();
                                    addTo.statIcon = (SpecialStatIcon)i + 1;
                                    switch (addTo.statIcon)
                                    {
                                        case SpecialStatIcon.Ants:
                                            addTo.specialAbilities.Add(SpecialTriggeredAbility.Ant);
                                            break;
                                        case SpecialStatIcon.Bell:
                                            addTo.specialAbilities.Add(SpecialTriggeredAbility.BellProximity);
                                            break;
                                        case SpecialStatIcon.Bones:
                                            addTo.specialAbilities.Add(SpecialTriggeredAbility.Lammergeier);
                                            break;
                                        case SpecialStatIcon.CardsInHand:
                                            addTo.specialAbilities.Add(SpecialTriggeredAbility.CardsInHand);
                                            break;
                                        case SpecialStatIcon.GreenGems:
                                            addTo.specialAbilities.Add(SpecialTriggeredAbility.GreenMage);
                                            break;
                                        case SpecialStatIcon.Mirror:
                                            addTo.specialAbilities.Add(SpecialTriggeredAbility.Mirror);
                                            break;
                                    }
                                }
                                else if (i <= 14)
                                {
                                    addTo.attackAdjustment = page * 15 + i - specialStatIcons;
                                    addTo.nameReplacement = page * 15 + i - specialStatIcons + " power";
                                }
                                else
                                {
                                    if (i == 15) addTo.nameReplacement = "previous page";
                                    else if (i == 16) addTo.nameReplacement = "next page";
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainBackground);
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainLayout);
                                }
                                c.Mods.Add(addTo);
                                choices.Add(c);
                                SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, false, true, false);
                                selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                                {
                                    selectedCard = (SelectableCard)c2;
                                });
                            }
                            else
                            {
                                choices.Add(null);
                            }
                        }
                        else
                        {
                            choices.Add(null);
                        }
                    }
                    Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                    Singleton<TextDisplayer>.Instance.ShowMessage("And another. This time I will use its [c:bR]power[c:].");
                    foreach (SelectableCard item in cards)
                    {
                        item.SetInteractionEnabled(interactionEnabled: true);
                        item.SetEnabled(enabled: true);
                    }
                    yield return new WaitUntil(() => selectedCard != null);
                    if (selectedCard.Info.DisplayedNameEnglish.Equals("previous page"))
                    {
                        if (page > 0) page--;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("next page"))
                    {
                        if (page < Math.Ceiling((9999 + specialStatIcons) / 15.0f)) page++;
                        DestroyAllCards(cards, true);
                    }
                    else
                    {
                        valid = true;
                    }
                }
                Singleton<TextDisplayer>.Instance.Clear();
                CardModificationInfo addTo2 = new CardModificationInfo();
                addTo2.attackAdjustment = selectedCard.Info.Attack;
                addTo2.statIcon = selectedCard.Info.SpecialStatIcon;
                addTo2.specialAbilities = selectedCard.Info.SpecialAbilities;
                preview.Info.Mods.Add(addTo2);
                yield return PostCardSelect(cards, selectedCard, 1);
            }

            /// <see cref="CreatePortrait"/>
            private static IEnumerator CreateHealthCard(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                bool valid = false;
                int page = 0;
                SelectableCard selectedCard = null;
                List<SelectableCard> cards = new List<SelectableCard>();
                while (!valid)
                {
                    selectedCard = null;
                    cards.Clear();
                    LookDown();
                    List<CardInfo> choices = new List<CardInfo>();
                    Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                    for (int i = 0; i <= 16; i++)
                    {
                        if (page * 15 + i <= 9998 || i == 15)
                        {
                            if (i <= 14 || (i == 15 && page > 0) || (i == 16 && page < 667))
                            {
                                Vector3 vector = GetCardIndexLoc(obj, i);
                                CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                                CardModificationInfo addTo = new CardModificationInfo();
                                if (i <= 14)
                                {
                                    addTo.healthAdjustment = 1 + page * 15 + i;
                                    addTo.nameReplacement = 1 + page * 15 + i + " health";
                                }
                                else
                                {
                                    if (i == 15) addTo.nameReplacement = "previous page";
                                    else if (i == 16) addTo.nameReplacement = "next page";
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainBackground);
                                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainLayout);
                                }
                                c.Mods.Add(addTo);
                                choices.Add(c);
                                SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, false, false);
                                selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                                {
                                    selectedCard = (SelectableCard)c2;
                                });
                            }
                            else
                            {
                                choices.Add(null);
                            }
                        }
                        else
                        {
                            choices.Add(null);
                        }
                    }
                    Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                    Singleton<TextDisplayer>.Instance.ShowMessage("And another. This time I will use its [c:bR]health[c:].");
                    foreach (SelectableCard item in cards)
                    {
                        item.SetInteractionEnabled(interactionEnabled: true);
                        item.SetEnabled(enabled: true);
                    }
                    yield return new WaitUntil(() => selectedCard != null);
                    if (selectedCard.Info.DisplayedNameEnglish.Equals("previous page"))
                    {
                        if (page > 0) page--;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("next page"))
                    {
                        if (page < 667) page++;
                        DestroyAllCards(cards, true);
                    }
                    else
                    {
                        valid = true;
                    }
                }
                Singleton<TextDisplayer>.Instance.Clear();
                CardModificationInfo addTo2 = new CardModificationInfo();
                addTo2.healthAdjustment = selectedCard.Info.Health;
                preview.Info.Mods.Add(addTo2);
                yield return PostCardSelect(cards, selectedCard, 2);
            }

            private static IEnumerator CreateSigilCard(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                bool valid = false;
                bool skipRegenerate = false;
                bool instantSpawn = false;
                int page = 0;
                SelectableCard selectedCard = null;
                List<Ability> selectedSigils = new List<Ability>();
                List<SelectableCard> cards = new List<SelectableCard>();
                List<Ability> abilities = new List<Ability>();
                List<Ability> baseAbilities = new List<Ability>();

                // Load base abilities
                for (int i = 1; i < 99; i++)
                {
                   baseAbilities.Add((Ability)i);
                }
                // TODO: Uncomment when custom sigils are implemented
                //List<Ability> a1 = AbilitiesUtil.GetAbilities(false, categoryCriteria: AbilityMetaCategory.Part1Rulebook);
                //List<Ability> a2 = AbilitiesUtil.GetAbilities(false, categoryCriteria: AbilityMetaCategory.Part3Rulebook);
               
                Transform objP = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                
                // Create confirm card
                Vector3 vectorP = GetCardIndexLoc(objP, 18);
                CardInfo cP = ScriptableObject.CreateInstance<CardInfo>();
                CardModificationInfo addToP = new CardModificationInfo();
                addToP.nameReplacement = "confirm";
                cP.Mods.Add(addToP);
                cP.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.RareCardBackground);
                SelectableCard confirm = CreateConfirmCard(__instance, vectorP, cP);
                confirm.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(confirm.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                {
                    selectedCard = (SelectableCard)c2;
                });

                // Remove duplicates
                foreach (Ability a in baseAbilities)
                {
                    if (!abilities.Contains(a)) abilities.Add(a);
                }
                // TODO: Uncomment when custom sigils are implemented
                //foreach (Ability a in a1)
                //{
                //    if (!abilities.Contains(a)) abilities.Add(a);
                //}
                //foreach (Ability a in a2)
                //{
                //    if (!abilities.Contains(a)) abilities.Add(a);
                //}
                List<CardInfo> choices = null;
                Transform obj = null;
                // Until confirm is selected
                while (!valid)
                {
                    selectedCard = null;
                    if (!skipRegenerate)
                    {
                        cards.Clear();
                        LookDown();
                        choices = new List<CardInfo>();
                        obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                        for (int i = 0; i <= 16; i++)
                        {
                            if (page * 15 + i < abilities.Count || i == 15)
                            {
                                if (i <= 14 || (i == 15 && page > 0) || (i == 16 && page < Math.Ceiling(abilities.Count / 15.0f)))
                                {
                                    Vector3 vector = GetCardIndexLoc(obj, i);
                                    CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                                    CardModificationInfo addTo = new CardModificationInfo();
                                    if (i <= 14)
                                    {
                                        List<Ability> abl = new List<Ability>
                                        {
                                            abilities[page * 15 + i]
                                        };
                                        // Get name
                                        if (AbilitiesUtil.GetInfo(abl[0]).rulebookName != null && AbilitiesUtil.GetInfo(abl[0]).rulebookName != "") addTo.nameReplacement = "sigil of " + AbilitiesUtil.GetInfo(abl[0]).rulebookName;
                                        else addTo.nameReplacement = "sigil of " + AbilitiesUtil.GetInfo(abl[0]).name;
                                        addTo.abilities = abl;
                                    }
                                    else
                                    {
                                        if (i == 15) addTo.nameReplacement = "previous page";
                                        else if (i == 16) addTo.nameReplacement = "next page";
                                        c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainBackground);
                                        c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainLayout);
                                    }
                                    c.Mods.Add(addTo);
                                    choices.Add(c);
                                    if (i <= 14)
                                    {
                                        // If selected, make gold
                                        c.appearanceBehaviour = new List<CardAppearanceBehaviour.Appearance>();
                                        if (c.Abilities.Count > 0 && selectedSigils.Contains(c.Abilities[0]))
                                        {
                                            c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.GoldEmission);
                                        }
                                    }
                                    SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, instantSpawn);
                                    selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                                    {
                                        selectedCard = (SelectableCard)c2;
                                    });
                                }
                                else
                                {
                                    choices.Add(null);
                                }
                            }
                            else
                            {
                                choices.Add(null);
                            }
                        }
                        Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                        Singleton<TextDisplayer>.Instance.ShowMessage("Now choose some cards from which we will extract the [c:bR]sigils[c:].");
                        foreach (SelectableCard item in cards)
                        {
                            item.SetInteractionEnabled(interactionEnabled: true);
                            item.SetEnabled(enabled: true);
                        }
                    }
                    confirm.SetInteractionEnabled(interactionEnabled: true);
                    confirm.SetEnabled(enabled: true);
                    skipRegenerate = false;
                    yield return new WaitUntil(() => selectedCard != null);
                    if (selectedCard.Info.DisplayedNameEnglish.Equals("previous page"))
                    {
                        if (page > 0) page--;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("next page"))
                    {
                        if (page < Math.Ceiling(abilities.Count / 15.0f)) page++;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("confirm"))
                    {
                        valid = true;
                    }
                    else
                    {
                        if (selectedSigils.Contains(selectedCard.Info.Abilities[0]))
                        {
                            // Remove gold glow
                            selectedSigils.Remove(selectedCard.Info.Abilities[0]);
                            selectedCard.Anim.PlayDeathAnimation();
                            DestroyAllCards(cards, false);
                            instantSpawn = true;
                        }
                        else
                        {
                            // Add gold glow
                            selectedSigils.Add(selectedCard.Info.Abilities[0]);
                            selectedCard.Info.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.GoldEmission);
                            selectedCard.Anim.PlayTransformAnimation();
                            yield return new WaitForSeconds(0.15f);
                            selectedCard.SetInfo(selectedCard.Info);
                            yield return new WaitForSeconds(0.15f);
                            skipRegenerate = true;
                        }
                    }
                }
                Singleton<TextDisplayer>.Instance.Clear();
                CardModificationInfo addTo2 = new CardModificationInfo();
                addTo2.abilities = selectedSigils;
                preview.Info.Mods.Add(addTo2);
                confirm.Anim.PlayDeathAnimation();
                UnityEngine.Object.Destroy(confirm.gameObject, 2f);
                yield return PostMultiSigilCardSelect(cards, selectedSigils);
            }

            /// <see cref="CreateSigilCard"/>
            private static IEnumerator CreateSpecialAbilities(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                bool valid = false;
                bool skipRegenerate = false;
                bool instantSpawn = false;
                int page = 0;
                SelectableCard selectedCard = null;
                List<SpecialTriggeredAbility> selectedSAs = new List<SpecialTriggeredAbility>();
                List<Trait> selectedTraits = new List<Trait>();
                List<SelectableCard> cards = new List<SelectableCard>();
                int abilitiesCount = (int)SpecialTriggeredAbility.NUM_ABILITIES;
                int traitCount = (int)Trait.NUM_TRAITS;
                List<SpecialTriggeredAbility> validAbilities = new List<SpecialTriggeredAbility>();
                for (int i = 1; i < abilitiesCount; i++)
                {
                    if (((SpecialTriggeredAbility) i) != SpecialTriggeredAbility.Ant && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.BellProximity && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.CardsInHand && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.Mirror && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.Lammergeier && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.GreenMage && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.EMPTY3 && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.EMPTY4 && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.EMPTY5 && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.EMPTY6)
                    {
                        validAbilities.Add((SpecialTriggeredAbility)i);
                    }
                }
                Transform objP = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                Vector3 vectorP = GetCardIndexLoc(objP, 18);
                CardInfo cP = ScriptableObject.CreateInstance<CardInfo>();
                CardModificationInfo addToP = new CardModificationInfo();
                addToP.nameReplacement = "confirm";
                cP.Mods.Add(addToP);
                cP.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.RareCardBackground);
                SelectableCard confirm = CreateConfirmCard(__instance, vectorP, cP);
                confirm.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(confirm.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                {
                    selectedCard = (SelectableCard)c2;
                });
                List<CardInfo> choices = null;
                Transform obj = null;
                while (!valid)
                {
                    selectedCard = null;
                    if (!skipRegenerate)
                    {
                        cards.Clear();
                        LookDown();
                        choices = new List<CardInfo>();
                        obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                        for (int i = 0; i <= 16; i++)
                        {
                            if (page * 15 + i < validAbilities.Count + traitCount || i == 15)
                            {
                                if (i <= 14 || (i == 15 && page > 0) || (i == 16 && page < Math.Ceiling((validAbilities.Count + traitCount) / 15.0f)))
                                {
                                    Vector3 vector = GetCardIndexLoc(obj, i);
                                    CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                                    CardModificationInfo addTo = new CardModificationInfo();
                                    if (i <= 14)
                                    {
                                        if (page * 15 + i >= validAbilities.Count)
                                        {
                                            c.traits.Add((Trait)(page * 15 + i - validAbilities.Count));
                                            addTo.nameReplacement = ((Trait)(page * 15 + i - validAbilities.Count)).ToString();
                                        }
                                        else
                                        {
                                            List<SpecialTriggeredAbility> abl = new List<SpecialTriggeredAbility>
                                            {
                                                validAbilities[page * 15 + i]
                                            };
                                            addTo.nameReplacement = abl[0].ToString();
                                            addTo.specialAbilities = abl;
                                        }
                                    }
                                    else
                                    {
                                        if (i == 15) addTo.nameReplacement = "previous page";
                                        else if (i == 16) addTo.nameReplacement = "next page";
                                        c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainBackground);
                                        c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainLayout);
                                    }
                                    c.Mods.Add(addTo);
                                    choices.Add(c);
                                    if (i <= 14)
                                    {
                                        c.appearanceBehaviour = new List<CardAppearanceBehaviour.Appearance>();
                                        if (c.SpecialAbilities.Count > 0 && selectedSAs.Contains(c.SpecialAbilities[0]))
                                        {
                                            c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.GoldEmission);
                                        }
                                        else if (c.traits.Count > 0 && selectedTraits.Contains(c.traits[0]))
                                        {
                                            c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.GoldEmission);
                                        }
                                    }
                                    SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, instantSpawn);
                                    selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                                    {
                                        selectedCard = (SelectableCard)c2;
                                    });
                                }
                                else
                                {
                                    choices.Add(null);
                                }
                            }
                            else
                            {
                                choices.Add(null);
                            }
                        }
                        Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                        Singleton<TextDisplayer>.Instance.ShowMessage("Finally, choose the hidden [c:bR]special abilities[c:] and [c:bR]traits[c:].");
                        foreach (SelectableCard item in cards)
                        {
                            item.SetInteractionEnabled(interactionEnabled: true);
                            item.SetEnabled(enabled: true);
                        }
                    }
                    confirm.SetInteractionEnabled(interactionEnabled: true);
                    confirm.SetEnabled(enabled: true);
                    skipRegenerate = false;
                    yield return new WaitUntil(() => selectedCard != null);
                    if (selectedCard.Info.DisplayedNameEnglish.Equals("previous page"))
                    {
                        if (page > 0) page--;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("next page"))
                    {
                        if (page < Math.Ceiling((validAbilities.Count + traitCount) / 15.0f)) page++;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("confirm"))
                    {
                        valid = true;
                    }
                    else
                    {
                        if (selectedCard.Info.SpecialAbilities.Count > 0 && selectedSAs.Contains(selectedCard.Info.SpecialAbilities[0]) || selectedCard.Info.traits.Count > 0 && selectedTraits.Contains(selectedCard.Info.traits[0]))
                        {
                            selectedSAs.Remove(selectedCard.Info.SpecialAbilities[0]);
                            selectedCard.Anim.PlayDeathAnimation();
                            DestroyAllCards(cards, false);
                            instantSpawn = true;
                        }
                        else
                        {
                            selectedSAs.Add(selectedCard.Info.SpecialAbilities[0]);
                            selectedCard.Info.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.GoldEmission);
                            selectedCard.Anim.PlayTransformAnimation();
                            yield return new WaitForSeconds(0.15f);
                            selectedCard.SetInfo(selectedCard.Info);
                            yield return new WaitForSeconds(0.15f);
                            skipRegenerate = true;
                        }
                    }
                }
                Singleton<TextDisplayer>.Instance.Clear();
                CardModificationInfo addTo2 = new CardModificationInfo();
                addTo2.specialAbilities = selectedSAs;
                preview.Info.Mods.Add(addTo2);
                preview.Info.traits = selectedTraits;
                confirm.Anim.PlayDeathAnimation();
                UnityEngine.Object.Destroy(confirm.gameObject, 2f);
                yield return PostMultiSACardSelect(cards, selectedSAs, selectedTraits);
            }

            /// <see cref="CreateSigilCard"/>
            private static IEnumerator CreateTribeCard(DeathCardCreationSequencer __instance, SelectableCard preview)
            {
                bool valid = false;
                bool skipRegenerate = false;
                bool instantSpawn = false;
                int page = 0;
                SelectableCard selectedCard = null;
                List<Tribe> selectedTribes = new List<Tribe>();
                List<SelectableCard> cards = new List<SelectableCard>();
                int tribeCount = (int)Tribe.NUM_TRIBES;
                Transform objP = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                Vector3 vectorP = GetCardIndexLoc(objP, 18);
                CardInfo cP = ScriptableObject.CreateInstance<CardInfo>();
                CardModificationInfo addToP = new CardModificationInfo();
                addToP.nameReplacement = "confirm";
                cP.Mods.Add(addToP);
                cP.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.RareCardBackground);
                SelectableCard confirm = CreateConfirmCard(__instance, vectorP, cP);
                confirm.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(confirm.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                {
                    selectedCard = (SelectableCard)c2;
                });
                List<CardInfo> choices = null;
                Transform obj = null;
                while (!valid)
                {
                    selectedCard = null;
                    if (!skipRegenerate)
                    {
                        cards.Clear();
                        LookDown();
                        choices = new List<CardInfo>();
                        obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
                        for (int i = 0; i <= 16; i++)
                        {
                            if (page * 15 + i < tribeCount - 1 || i == 15)
                            {
                                if (i <= 14 || (i == 15 && page > 0) || (i == 16 && page < Math.Ceiling(tribeCount / 15.0f)))
                                {
                                    Vector3 vector = GetCardIndexLoc(obj, i);
                                    CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
                                    CardModificationInfo addTo = new CardModificationInfo();
                                    if (i <= 14)
                                    {
                                        List<Tribe> trb = new List<Tribe>
                                        {
                                            (Tribe)(page * 15 + i + 1)
                                        };
                                        addTo.nameReplacement = ((Tribe)(page * 15 + i + 1)).ToString() + " tribe";
                                        c.tribes = trb;
                                    }
                                    else
                                    {
                                        if (i == 15) addTo.nameReplacement = "previous page";
                                        else if (i == 16) addTo.nameReplacement = "next page";
                                        c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainBackground);
                                        c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.TerrainLayout);
                                    }
                                    c.Mods.Add(addTo);
                                    choices.Add(c);
                                    if (i <= 14)
                                    {
                                        c.appearanceBehaviour = new List<CardAppearanceBehaviour.Appearance>();
                                        if (c.tribes.Count > 0 && selectedTribes.Contains(c.tribes[0]))
                                        {
                                            c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.GoldEmission);
                                        }
                                    }
                                    SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, instantSpawn);
                                    selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
                                    {
                                        selectedCard = (SelectableCard)c2;
                                    });
                                }
                                else
                                {
                                    choices.Add(null);
                                }
                            }
                            else
                            {
                                choices.Add(null);
                            }
                        }
                        Pixelplacement.Tween.LocalRotation(Singleton<ViewManager>.Instance.CameraParent, new Vector3(90, 0, 0), 0.25f, 0f, Pixelplacement.Tween.EaseInOut);
                        Singleton<TextDisplayer>.Instance.ShowMessage("Now choose the [c:bR]tribes[c:].");
                        foreach (SelectableCard item in cards)
                        {
                            item.SetInteractionEnabled(interactionEnabled: true);
                            item.SetEnabled(enabled: true);
                        }
                    }
                    confirm.SetInteractionEnabled(interactionEnabled: true);
                    confirm.SetEnabled(enabled: true);
                    skipRegenerate = false;
                    yield return new WaitUntil(() => selectedCard != null);
                    if (selectedCard.Info.DisplayedNameEnglish.Equals("previous page"))
                    {
                        if (page > 0) page--;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("next page"))
                    {
                        if (page < Math.Ceiling(tribeCount / 15.0f)) page++;
                        DestroyAllCards(cards, true);
                    }
                    else if (selectedCard.Info.DisplayedNameEnglish.Equals("confirm"))
                    {
                        valid = true;
                    }
                    else
                    {
                        if (selectedTribes.Contains(selectedCard.Info.tribes[0]))
                        {
                            selectedTribes.Remove(selectedCard.Info.tribes[0]);
                            selectedCard.Anim.PlayDeathAnimation();
                            DestroyAllCards(cards, false);
                            instantSpawn = true;
                        }
                        else
                        {
                            selectedTribes.Add(selectedCard.Info.tribes[0]);
                            selectedCard.Info.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.GoldEmission);
                            selectedCard.Anim.PlayTransformAnimation();
                            yield return new WaitForSeconds(0.15f);
                            selectedCard.SetInfo(selectedCard.Info);
                            yield return new WaitForSeconds(0.15f);
                            skipRegenerate = true;
                        }
                    }
                }
                Singleton<TextDisplayer>.Instance.Clear();
                preview.Info.tribes = selectedTribes;
                if (preview.Info.tribes.Count == 0) preview.Info.tribes.Add(Tribe.None);
                confirm.Anim.PlayDeathAnimation();
                UnityEngine.Object.Destroy(confirm.gameObject, 2f);
                yield return PostMultiTribeCardSelect(cards, selectedTribes);
            }

            private static IEnumerator PostCardSelect(List<SelectableCard> cards, SelectableCard selectedCard, int mode)
            {
                
                // Destroy all cards that aren't selected
                foreach (SelectableCard item2 in cards)
                {
                    item2.SetInteractionEnabled(interactionEnabled: false);
                    item2.SetEnabled(enabled: false);
                    if (item2 != selectedCard)
                    {
                        item2.Anim.PlayDeathAnimation();
                        UnityEngine.Object.Destroy(item2.gameObject, 2f);
                    }
                }

                // Make comment about card
                string examineDialogue = "...";
                switch (mode)
                {
                    case 0:
                        if (selectedCard.Info.GemsCost.Count > 0) examineDialogue = string.Format("A [c:bR]{0} gem[c:].", selectedCard.Info.GemsCost[0].ToString());
                        else if (selectedCard.Info.EnergyCost > 0) examineDialogue = string.Format("A cost of [c:bR]{0} energy[c:].", selectedCard.Info.EnergyCost);
                        else if (selectedCard.Info.BonesCost > 1) examineDialogue = string.Format("A cost of[c:bR]{0} bones[c:].", selectedCard.Info.BonesCost);
                        else if (selectedCard.Info.BonesCost > 0) examineDialogue = string.Format("A cost of[c:bR]1 bone[c:].");
                        else if (selectedCard.Info.BloodCost > 0) examineDialogue = string.Format("A [c:bR]{0} blood[c:].", selectedCard.Info.BloodCost);
                        else examineDialogue = string.Format("A cost of... [c:bR]free[c:].");
                        break;
                    case 1:
                        if (selectedCard.Info.SpecialStatIcon != SpecialStatIcon.None) examineDialogue = string.Format("A special [c:bR]Power of {0}[c:].", selectedCard.Info.DisplayedNameEnglish);
                        else examineDialogue = string.Format(("[c:bR]{0} Power[c:]."), selectedCard.Info.Attack);
                        break;
                    case 2:
                        examineDialogue = string.Format(("[c:bR]{0} Health[c:]."), selectedCard.Info.Health);
                        break;
                    case 3:
                        examineDialogue = string.Format(("A portrait of [c:bR]{0}[c:]."), selectedCard.Info.DisplayedNameEnglish);
                        break;
                    case 4:
                        examineDialogue = string.Format(("A [c:bR]{0}[c:] card."), selectedCard.Info.DisplayedNameEnglish);
                        break;
                    case 5:
                        examineDialogue = string.Format(("A complexity of [c:bR]{0}[c:]."), selectedCard.Info.DisplayedNameEnglish);
                        break;
                    case 6:
                        examineDialogue = string.Format(("An evolution to [c:bR]{0}[c:]."), selectedCard.Info.DisplayedNameEnglish);
                        break;
                    case 7:
                        if (selectedCard.Info.evolveParams.turnsToEvolve == 1) examineDialogue = string.Format(("[c:bR]{0} turn[c:]."), selectedCard.Info.evolveParams.turnsToEvolve);
                        else examineDialogue = string.Format(("[c:bR]{0} turns[c:]."), selectedCard.Info.evolveParams.turnsToEvolve);
                        break;
                }
                // Destroy selected card
                LookUp();
                yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(examineDialogue);
                selectedCard.Anim.PlayDeathAnimation();
                Destroy(selectedCard.gameObject, 2f);
                yield return new WaitForSeconds(0.25f);
            }

            /// <see cref="PostCardSelect"/>
            private static IEnumerator PostMultiSACardSelect(List<SelectableCard> cards, List<SpecialTriggeredAbility> selectedSAs, List<Trait> selectedTraits)
            {
                foreach (SelectableCard item2 in cards)
                {
                    item2.SetInteractionEnabled(interactionEnabled: false);
                    item2.SetEnabled(enabled: false);
                    if (!(item2.Info.SpecialAbilities.Count > 0 && selectedSAs.Contains(item2.Info.SpecialAbilities[0]) || (item2.Info.traits.Count > 0 && selectedTraits.Contains(item2.Info.traits[0]))))
                    {
                        item2.Anim.PlayDeathAnimation();
                        UnityEngine.Object.Destroy(item2.gameObject, 2f);
                    }
                }
                string examineDialogue = "...";
                if (selectedSAs.Count > 0)
                {
                    string args = "";
                    int size = selectedSAs.Count;
                    for (int i = 0; i < size; i++)
                    {
                        args += (selectedSAs[i].ToString());
                        if (i < size - 1)
                        {
                            if (i == size - 2)
                            {
                                if (size == 2)
                                {
                                    args += "[c:] and [c:bR]";
                                }
                                else
                                {
                                    args += "[c:], and [c:bR]";
                                }
                            }
                            else
                            {
                                args += "[c:], [c:bR]";
                            }
                        }
                    }
                    examineDialogue = string.Format(Localization.Translate("A [c:bR]Special Ability of {0}[c:]."), args);
                }
                if (selectedTraits.Count > 0)
                {
                    string args = " ";
                    int size = selectedSAs.Count;
                    for (int i = 0; i < size; i++)
                    {
                        args += (selectedSAs[i].ToString());
                        if (i < size - 1)
                        {
                            if (i == size - 2)
                            {
                                if (size == 2)
                                {
                                    args += "[c:] and [c:bR]";
                                }
                                else
                                {
                                    args += "[c:], and [c:bR]";
                                }
                            }
                            else
                            {
                                args += "[c:], [c:bR]";
                            }
                        }
                    }
                    examineDialogue += string.Format(Localization.Translate("A [c:bR]Trait of {0}[c:]."), args);
                }
                else
                {
                    examineDialogue += string.Format(Localization.Translate("[c:bR]No Traits[c:]."));
                }
                LookUp();
                yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(examineDialogue);
                foreach (SelectableCard item2 in cards)
                {
                    if (item2 != null && (item2.Info.SpecialAbilities.Count > 0 && selectedSAs.Contains(item2.Info.SpecialAbilities[0]) || item2.Info.traits.Count > 0 && selectedTraits.Contains(item2.Info.traits[0])))
                    {
                        item2.Anim.PlayDeathAnimation();
                        UnityEngine.Object.Destroy(item2.gameObject, 2f);
                    }
                }
                yield return new WaitForSeconds(0.25f);
            }

            /// <see cref="PostCardSelect"/>
            private static IEnumerator PostMultiSigilCardSelect(List<SelectableCard> cards, List<Ability> selectedSigils)
            {
                foreach (SelectableCard item2 in cards)
                {
                    item2.SetInteractionEnabled(interactionEnabled: false);
                    item2.SetEnabled(enabled: false);
                    if (item2.Info.Abilities.Count == 0 || !selectedSigils.Contains(item2.Info.Abilities[0]))
                    {
                        item2.Anim.PlayDeathAnimation();
                        UnityEngine.Object.Destroy(item2.gameObject, 2f);
                    }
                }
                string examineDialogue = "...";
                if (selectedSigils.Count > 0)
                {
                    string args = "";
                    int size = selectedSigils.Count;
                    for (int i = 0; i < size; i++)
                    {
                        
                        if (AbilitiesUtil.GetInfo(selectedSigils[i]).rulebookName != null && AbilitiesUtil.GetInfo(selectedSigils[i]).rulebookName != "") args += (AbilitiesUtil.GetInfo(selectedSigils[i]).rulebookName);
                        else args += "sigil of " + AbilitiesUtil.GetInfo(selectedSigils[i]).name;
                        if (i < size - 1)
                        {
                            if (i == size - 2)
                            {
                                if (size == 2)
                                {
                                    args += "[c:] and [c:bR]";
                                }
                                else
                                {
                                    args += "[c:], and [c:bR]";
                                }
                            }
                            else
                            {
                                args += "[c:], [c:bR]";
                            }
                        }
                    }
                    examineDialogue = string.Format(Localization.Translate("A [c:bR]Sigil of {0}[c:]."), args);
                }
                else
                {
                    examineDialogue = string.Format(Localization.Translate("[c:bR]No Sigils[c:]."));
                }
                LookUp();
                yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(examineDialogue);
                foreach (SelectableCard item2 in cards)
                {
                    if (item2 != null && item2.Info.Abilities.Count > 0 && selectedSigils.Contains(item2.Info.Abilities[0]))
                    {
                        item2.Anim.PlayDeathAnimation();
                        UnityEngine.Object.Destroy(item2.gameObject, 2f);
                    }
                }
                yield return new WaitForSeconds(0.25f);
            }

            private static IEnumerator PostMultiTribeCardSelect(List<SelectableCard> cards, List<Tribe> selectedTribes)
            {
                foreach (SelectableCard item2 in cards)
                {
                    item2.SetInteractionEnabled(interactionEnabled: false);
                    item2.SetEnabled(enabled: false);
                    if (item2.Info.tribes.Count == 0 || !selectedTribes.Contains(item2.Info.tribes[0]))
                    {
                        item2.Anim.PlayDeathAnimation();
                        UnityEngine.Object.Destroy(item2.gameObject, 2f);
                    }
                }
                string examineDialogue = "...";
                if (selectedTribes.Count == (int)Tribe.NUM_TRIBES - 1)
                {
                    examineDialogue = string.Format(Localization.Translate("[c:bR]All Tribes[c:]."));
                }
                else if (selectedTribes[0] != Tribe.None)
                {
                    string args = "";
                    int size = selectedTribes.Count;
                    for (int i = 0; i < size; i++)
                    {
                        args += (Localization.Translate(selectedTribes[i].ToString()));
                        if (i < size - 1)
                        {
                            if (i == size - 2)
                            {
                                if (size == 2)
                                {
                                    args += "[c:] and [c:bR]";
                                }
                                else
                                {
                                    args += "[c:], and [c:bR]";
                                }
                            }
                            else
                            {
                                args += "[c:], [c:bR]";
                            }
                        }
                    }
                    examineDialogue = string.Format(Localization.Translate("[c:bR]{0} Tribe[c:]."), args);
                }
                else
                {
                    examineDialogue = string.Format(Localization.Translate("[c:bR]No Tribes[c:]."));
                }
                LookUp();
                yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(examineDialogue);
                foreach (SelectableCard item2 in cards)
                {
                    if (item2 != null && item2.Info.tribes.Count > 0 && selectedTribes.Contains(item2.Info.tribes[0]))
                    {
                        item2.Anim.PlayDeathAnimation();
                        UnityEngine.Object.Destroy(item2.gameObject, 2f);
                    }
                }
                yield return new WaitForSeconds(0.25f);
            }

            private static void DestroyAllCards(List<SelectableCard> cards, bool playDeathAnim)
            {
                foreach (SelectableCard item2 in cards)
                {
                    item2.SetInteractionEnabled(interactionEnabled: false);
                    item2.SetEnabled(enabled: false);
                    if (playDeathAnim)
                    {
                        item2.Anim.PlayDeathAnimation();
                        UnityEngine.Object.Destroy(item2.gameObject, 2f);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(item2.gameObject, 0f);
                    }
                }
            }

            private static Vector3 GetCardIndexLoc(Transform obj, int i)
            {
                Vector3 vector;
                if (i == 15)
                {
                    vector = obj.position + new Vector3(UnityEngine.Random.value * 0.05f - 2.075f, 0f, UnityEngine.Random.value * 0.05f + 3.0f);

                }
                else if (i == 16)
                {
                    vector = obj.position + new Vector3(UnityEngine.Random.value * 0.05f + 6.925f, 0f, UnityEngine.Random.value * 0.05f + 3.0f);
                }
                else if (i == 17)
                {
                    vector = obj.position + new Vector3(UnityEngine.Random.value * 0.05f - 2.075f, 0f, UnityEngine.Random.value * 0.05f + 5.0f);
                }
                else if (i == 18)
                {
                    vector = obj.position + new Vector3(UnityEngine.Random.value * 0.05f + 6.925f, 0f, UnityEngine.Random.value * 0.05f + 5.0f);
                }
                else
                {
                    int z = i / 5;
                    int x = i % 5;
                    vector = obj.position + new Vector3(UnityEngine.Random.value * 0.05f + x * 1.5f - 0.575f, 0f, UnityEngine.Random.value * 0.05f + 5.0f - z * 2.0f);
                }
                return vector;
            }

            private static SelectableCard CreateCard(DeathCardCreationSequencer __instance, List<CardInfo> choices, List<SelectableCard> cards, Vector3 vector, bool hiddenAttack, bool hiddenHealth, bool instantSpawn)
            {
                // Invoke DeathCardCreationSequencer.SpawnCard()
                SelectableCard selectableCard = (SelectableCard)__instance.GetType().GetMethod("SpawnCard", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { __instance.transform });
                if (hiddenAttack || choices.Count > 15) selectableCard.RenderInfo.hiddenAttack = true;
                if (hiddenHealth || choices.Count > 15) selectableCard.RenderInfo.hiddenHealth = true;
                selectableCard.gameObject.SetActive(value: true);
                selectableCard.SetInfo(choices[choices.Count - 1]);
                selectableCard.SetInteractionEnabled(interactionEnabled: false);
                selectableCard.SetEnabled(enabled: false);
                cards.Add(selectableCard);

                // Place at vector
                selectableCard.transform.position = vector + Vector3.up * 2f + ((Vector3)__instance.GetType().GetField("THROW_CARDS_FROM", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance)) * 2f;
                selectableCard.transform.eulerAngles = new Vector3(90, 0, 0);
                if (instantSpawn) Pixelplacement.Tween.Position(selectableCard.transform, vector, 0.0f, 0f);
                else Pixelplacement.Tween.Position(selectableCard.transform, vector, 0.1f, 0f);
                return selectableCard;
            }

            /// <see cref="CreateCard"/>
            private static SelectableCard CreateConfirmCard(DeathCardCreationSequencer __instance, Vector3 vector, CardInfo info)
            {
                SelectableCard selectableCard = (SelectableCard)__instance.GetType().GetMethod("SpawnCard", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { __instance.transform });
                selectableCard.RenderInfo.hiddenAttack = true;
                selectableCard.RenderInfo.hiddenHealth = true;
                selectableCard.gameObject.SetActive(value: true);
                selectableCard.SetInfo(info);
                selectableCard.SetInteractionEnabled(interactionEnabled: false);
                selectableCard.SetEnabled(enabled: false);
                selectableCard.transform.position = vector + Vector3.up * 2f + ((Vector3)__instance.GetType().GetField("THROW_CARDS_FROM", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance)) * 2f;
                selectableCard.transform.eulerAngles = new Vector3(90, 0, 0);
                Pixelplacement.Tween.Position(selectableCard.transform, vector, 0.1f, 0f);
                return selectableCard;
            }
        }
    }
}
