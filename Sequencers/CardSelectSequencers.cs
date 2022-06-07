using APIPlugin;
using BepInEx;
using BepInEx.Logging;
using DiskCardGame;
using HarmonyLib;
using IGCCMod;
using IGCCMod.JSON;
using IGCCMod.Util;
using InscryptionAPI.Card;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using static InscryptionAPI.Card.AbilityManager;


public class CardSelectSequencers
{

    /// <see cref="CreatePortrait"/>
    public static IEnumerator CreateRarity(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        IGCC.LookDown();
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
    public static IEnumerator CreateComplexity(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        IGCC.LookDown();
        SelectableCard selectedCard = null;
        List<CardInfo> choices = new List<CardInfo>();
        List<SelectableCard> cards = new List<SelectableCard>();
        Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
        for (int i = 0; i <= 3; i++)
        {
            Vector3 vector = GetCardIndexLoc(obj, i + 5);
            CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
            CardModificationInfo addTo = new CardModificationInfo
            {
                nameReplacement = ((CardComplexity)i).ToString()
            };
            c.cardComplexity = ((CardComplexity)i);
            c.Mods.Add(addTo);
            choices.Add(c);
            SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
            selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
            {
                selectedCard = (SelectableCard)c2;
            });
        }
        Singleton<TextDisplayer>.Instance.ShowMessage("Choose the [c:bR]complexity[c:] of card. This determines when it is unlocked in the tutorial.");
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
    public static IEnumerator CreateCostCard(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        bool valid = false;
        int page = 0;
        SelectableCard selectedCard = null;
        List<SelectableCard> cards = new List<SelectableCard>();
        IGCC.LookDown();
        while (!valid)
        {
            selectedCard = null;
            cards.Clear();
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
                                if (page * 15 + i - 4 == 1) addTo.nameReplacement = "1 bone";
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
        preview.Info.cost = selectedCard.Info.BloodCost;
        preview.Info.bonesCost = selectedCard.Info.BonesCost;
        preview.Info.energyCost = selectedCard.Info.EnergyCost;
        preview.Info.gemsCost = selectedCard.Info.GemsCost;
        yield return PostCardSelect(cards, selectedCard, 0);
    }

    /// <see cref="CreatePortrait"/>
    public static IEnumerator CreateCostCardAdditive(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        bool valid = false;
        int page = 0;
        SelectableCard selectedCard = null;
        List<SelectableCard> cards = new List<SelectableCard>();
        IGCC.LookDown();
        while (!valid)
        {
            selectedCard = null;
            cards.Clear();
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
                                if (page * 15 + i - 4 == 1) addTo.nameReplacement = "1 bone";
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
            Singleton<TextDisplayer>.Instance.ShowMessage("Do you want to add another [c:bR]cost[c:]?");
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

        if (selectedCard.Info.BloodCost > 0) preview.Info.cost += selectedCard.Info.BloodCost;
        else if (selectedCard.Info.BonesCost > 0) preview.Info.bonesCost += selectedCard.Info.BonesCost;
        else if (selectedCard.Info.EnergyCost > 0) preview.Info.energyCost += selectedCard.Info.EnergyCost;
        else if (selectedCard.Info.GemsCost.Count > 0)
        {
            if (preview.Info.gemsCost == null) preview.Info.gemsCost = new List<GemType>();
        }
        preview.Info.gemsCost.AddRange(selectedCard.Info.GemsCost);
        yield return PostCardSelect(cards, selectedCard, 0);
    }

    public static IEnumerator CreatePortrait(DeathCardCreationSequencer __instance, SelectableCard preview, List<Texture2D> portraits, int type)
    {
        bool valid = false;
        int page = 0;
        SelectableCard selectedCard = null;
        List<SelectableCard> cards = new List<SelectableCard>();
        // Look at floor
        IGCC.LookDown();
        // Until a non-page card is selected
        while (!valid)
        {
            selectedCard = null;
            cards.Clear();

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
        if (type == 0)
        {
            preview.Info.portraitTex = Sprite.Create(selectedCard.Info.portraitTex.texture, new Rect(0.0f, 0.0f, 114.0f, 94.0f), new Vector2(0.5f, 0.5f));
        }
        else
        {
            preview.Info.alternatePortrait = Sprite.Create(selectedCard.Info.portraitTex.texture, new Rect(0.0f, 0.0f, 114.0f, 94.0f), new Vector2(0.5f, 0.5f));
        }
        // Leshy comment about the choice
        yield return PostCardSelect(cards, selectedCard, 3);
    }

    /// <see cref="CreatePortrait"/>
    public static IEnumerator CreateEvolution(DeathCardCreationSequencer __instance, SelectableCard preview, int type)
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
        IGCC.LookDown();
        while (!valid)
        {
            selectedCard = null;
            cards.Clear();
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
                            if (page * 15 + i == 0)
                            {
                                if (type == 0) addTo.nameReplacement = "default (+1/2)";
                                else if (type == 1) addTo.nameReplacement = "default (opposum)";
                            }
                            else c = validCards[page * 15 + i - 1];
                            c.specialAbilities = new List<SpecialTriggeredAbility>();
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
                preview.Info.evolveParams = new EvolveParams
                {
                    evolution = selectedCard.Info,
                    turnsToEvolve = 1
                };
            }
            else if (type == 1)
            {
                preview.Info.iceCubeParams = new IceCubeParams
                {
                    creatureWithin = selectedCard.Info
                };
            }
        }
        yield return PostCardSelect(cards, selectedCard, 6);
    }

    /// <see cref="CreatePortrait"/>
    public static IEnumerator CreateEvolutionTurnCount(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        IGCC.LookDown();
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
            c.evolveParams = new EvolveParams
            {
                turnsToEvolve = i + 1
            };
            choices.Add(c);
            SelectableCard selectableCard = CreateCard(__instance, choices, cards, vector, true, true, false);
            selectableCard.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(selectableCard.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
            {
                selectedCard = (SelectableCard)c2;
            });
        }
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
    public static IEnumerator CreateAttackCard(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        bool valid = false;
        int page = 0;
        SelectableCard selectedCard = null;
        List<SelectableCard> cards = new List<SelectableCard>();
        int specialStatIcons = (int)SpecialStatIcon.NUM_ICONS - 1;
        IGCC.LookDown();
        while (!valid)
        {
            selectedCard = null;
            cards.Clear();
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
        preview.Info.baseAttack = selectedCard.Info.Attack;
        preview.Info.specialStatIcon = selectedCard.Info.SpecialStatIcon;
        if (preview.Info.specialAbilities == null)
        {
            preview.Info.specialAbilities = new List<SpecialTriggeredAbility>();
        }
        else
        {
            if (preview.Info.SpecialStatIcon != null)
            {
                ResetAttackAbility(preview.Info.SpecialStatIcon, preview.Info);
            }
        }
        preview.Info.specialAbilities.AddRange(selectedCard.Info.SpecialAbilities);
        yield return PostCardSelect(cards, selectedCard, 1);
    }

    private static void ResetAttackAbility(SpecialStatIcon icon, CardInfo card)
    {
        switch (icon)
        {
            case SpecialStatIcon.Ants:
                card.specialAbilities.Remove(SpecialTriggeredAbility.Ant);
                break;
            case SpecialStatIcon.Bell:
                card.specialAbilities.Remove(SpecialTriggeredAbility.BellProximity);
                break;
            case SpecialStatIcon.Bones:
                card.specialAbilities.Remove(SpecialTriggeredAbility.Lammergeier);
                break;
            case SpecialStatIcon.CardsInHand:
                card.specialAbilities.Remove(SpecialTriggeredAbility.CardsInHand);
                break;
            case SpecialStatIcon.GreenGems:
                card.specialAbilities.Remove(SpecialTriggeredAbility.GreenMage);
                break;
            case SpecialStatIcon.Mirror:
                card.specialAbilities.Remove(SpecialTriggeredAbility.Mirror);
                break;
        }
    }

    /// <see cref="CreatePortrait"/>
    public static IEnumerator CreateHealthCard(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        bool valid = false;
        int page = 0;
        SelectableCard selectedCard = null;
        List<SelectableCard> cards = new List<SelectableCard>();
        IGCC.LookDown();
        while (!valid)
        {
            selectedCard = null;
            cards.Clear();
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
        preview.Info.baseHealth = selectedCard.Info.Health;
        yield return PostCardSelect(cards, selectedCard, 2);
    }

    public static IEnumerator CreateSigilCard(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        bool valid = false;
        bool skipRegenerate = false;
        bool instantSpawn = false;
        int page = 0;
        SelectableCard selectedCard = null;
        List<Ability> selectedSigils = new List<Ability>();
        List<SelectableCard> cards = new List<SelectableCard>();

        List<Ability> allAbilities = Enumerable.Range(0, (int)Ability.NUM_ABILITIES).Select(a => (Ability)a).ToList();

        List<Ability> abilities = allAbilities.Where(a => AbilitiesUtil.GetInfo(a) != null && (AbilitiesUtil.GetInfo(a).metaCategories.Contains(AbilityMetaCategory.Part1Rulebook) || AbilitiesUtil.GetInfo(a).metaCategories.Contains(AbilityMetaCategory.Part3Rulebook))).ToList();

        List<AbilityManager.FullAbility> moddedAbilities = AbilityManager.AllAbilities;

        abilities.AddRange(moddedAbilities.Where(na => na.Info != null && !allAbilities.Contains(na.Id) && (na.Info.metaCategories.Contains(AbilityMetaCategory.Part1Rulebook) || na.Info.metaCategories.Contains(AbilityMetaCategory.Part3Rulebook))).Select(na => na.Id));

        Transform objP = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];

        // Create confirm card
        Vector3 vectorP = GetCardIndexLoc(objP, 18);
        CardInfo cP = ScriptableObject.CreateInstance<CardInfo>();
        CardModificationInfo addToP = new CardModificationInfo
        {
            nameReplacement = "confirm"
        };
        cP.Mods.Add(addToP);
        cP.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.RareCardBackground);
        SelectableCard confirm = CreateConfirmCard(__instance, vectorP, cP);
        confirm.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(confirm.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
        {
            selectedCard = (SelectableCard)c2;
        });

        List<CardInfo> choices = null;
        Transform obj = null;
        IGCC.LookDown();
        // Until confirm is selected
        while (!valid)
        {
            selectedCard = null;
            if (!skipRegenerate)
            {
                cards.Clear(); ;
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
                Singleton<TextDisplayer>.Instance.ShowMessage("Now choose some cards from which we will extract the [c:bR]sigils[c:]. Note that some may not work correctly.");
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
        preview.Info.abilities = selectedSigils;
        confirm.Anim.PlayDeathAnimation();
        UnityEngine.Object.Destroy(confirm.gameObject, 2f);
        yield return PostMultiSigilCardSelect(cards, selectedSigils);
    }

    /// <see cref="CreateSigilCard"/>
    public static IEnumerator CreateSpecialAbilities(DeathCardCreationSequencer __instance, SelectableCard preview)
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
        int traitCount = (int)Trait.NUM_TRAITS - 1;
        List<SpecialTriggeredAbility> validAbilities = new List<SpecialTriggeredAbility>();
        for (int i = 1; i < abilitiesCount; i++)
        {
            if (((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.Ant && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.BellProximity && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.CardsInHand && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.Mirror && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.Lammergeier && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.GreenMage && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.EMPTY3 && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.EMPTY4 && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.EMPTY5 && ((SpecialTriggeredAbility)i) != SpecialTriggeredAbility.EMPTY6)
            {
                validAbilities.Add((SpecialTriggeredAbility)i);
            }
        }
        Transform objP = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
        Vector3 vectorP = GetCardIndexLoc(objP, 18);
        CardInfo cP = ScriptableObject.CreateInstance<CardInfo>();
        CardModificationInfo addToP = new CardModificationInfo
        {
            nameReplacement = "confirm"
        };
        cP.Mods.Add(addToP);
        cP.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.RareCardBackground);
        SelectableCard confirm = CreateConfirmCard(__instance, vectorP, cP);
        confirm.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(confirm.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
        {
            selectedCard = (SelectableCard)c2;
        });
        List<CardInfo> choices = null;
        Transform obj = null;
        IGCC.LookDown();
        while (!valid)
        {
            selectedCard = null;
            if (!skipRegenerate)
            {
                cards.Clear();
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
                                    c.traits.Add((Trait)(page * 15 + i - validAbilities.Count + 1));
                                    addTo.nameReplacement = ((Trait)(page * 15 + i - validAbilities.Count + 1)).ToString();
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
                Singleton<TextDisplayer>.Instance.ShowMessage("Finally, choose the hidden [c:bR]special abilities[c:] and [c:bR]traits[c:]. Again, some may not function properly.");
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
                    if (selectedCard.Info.SpecialAbilities.Count > 0) selectedSAs.Remove(selectedCard.Info.SpecialAbilities[0]);
                    else selectedTraits.Remove(selectedCard.Info.traits[0]);
                    selectedCard.Anim.PlayDeathAnimation();
                    DestroyAllCards(cards, false);
                    instantSpawn = true;
                }
                else
                {
                    if (selectedCard.Info.SpecialAbilities.Count > 0) selectedSAs.Add(selectedCard.Info.SpecialAbilities[0]);
                    else selectedTraits.Add(selectedCard.Info.traits[0]);
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
        if (preview.Info.specialAbilities == null)
        {
            preview.Info.specialAbilities = new List<SpecialTriggeredAbility>();
        }
        if (preview.Info.specialStatIcon != SpecialStatIcon.None) SetAttackAbility(preview.Info.specialStatIcon, preview.Info);
        preview.Info.specialAbilities.AddRange(selectedSAs);
        preview.Info.traits = selectedTraits;
        confirm.Anim.PlayDeathAnimation();
        UnityEngine.Object.Destroy(confirm.gameObject, 2f);
        yield return PostMultiSACardSelect(cards, selectedSAs, selectedTraits);
    }

    private static void SetAttackAbility(SpecialStatIcon icon, CardInfo card)
    {
        switch (icon)
        {
            case SpecialStatIcon.Ants:
                card.specialAbilities.Add(SpecialTriggeredAbility.Ant);
                break;
            case SpecialStatIcon.Bell:
                card.specialAbilities.Add(SpecialTriggeredAbility.BellProximity);
                break;
            case SpecialStatIcon.Bones:
                card.specialAbilities.Add(SpecialTriggeredAbility.Lammergeier);
                break;
            case SpecialStatIcon.CardsInHand:
                card.specialAbilities.Add(SpecialTriggeredAbility.CardsInHand);
                break;
            case SpecialStatIcon.GreenGems:
                card.specialAbilities.Add(SpecialTriggeredAbility.GreenMage);
                break;
            case SpecialStatIcon.Mirror:
                card.specialAbilities.Add(SpecialTriggeredAbility.Mirror);
                break;
        }
    }

    /// <see cref="CreateSigilCard"/>
    public static IEnumerator CreateTribeCard(DeathCardCreationSequencer __instance, SelectableCard preview)
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
        CardModificationInfo addToP = new CardModificationInfo
        {
            nameReplacement = "confirm"
        };
        cP.Mods.Add(addToP);
        cP.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.RareCardBackground);
        SelectableCard confirm = CreateConfirmCard(__instance, vectorP, cP);
        confirm.CursorSelectEnded = (Action<MainInputInteractable>)Delegate.Combine(confirm.CursorSelectEnded, (Action<MainInputInteractable>)delegate (MainInputInteractable c2)
        {
            selectedCard = (SelectableCard)c2;
        });
        List<CardInfo> choices = null;
        Transform obj = null;
        IGCC.LookDown();
        while (!valid)
        {
            selectedCard = null;
            if (!skipRegenerate)
            {
                cards.Clear();
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
        confirm.Anim.PlayDeathAnimation();
        UnityEngine.Object.Destroy(confirm.gameObject, 2f);
        yield return PostMultiTribeCardSelect(cards, selectedTribes);
    }

    public static IEnumerator PostCardSelect(List<SelectableCard> cards, SelectableCard selectedCard, int mode)
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
                if (selectedCard.Info.GemsCost.Count > 0) examineDialogue = string.Format("A cost of a [c:bR]{0} gem[c:].", selectedCard.Info.GemsCost[0].ToString());
                else if (selectedCard.Info.EnergyCost > 0) examineDialogue = string.Format("A cost of [c:bR]{0} energy[c:].", selectedCard.Info.EnergyCost);
                else if (selectedCard.Info.BonesCost > 1) examineDialogue = string.Format("A cost of [c:bR]{0} bones[c:].", selectedCard.Info.BonesCost);
                else if (selectedCard.Info.BonesCost > 0) examineDialogue = string.Format("A cost of [c:bR]1 bone[c:].");
                else if (selectedCard.Info.BloodCost > 0) examineDialogue = string.Format("A cost of [c:bR]{0} blood[c:].", selectedCard.Info.BloodCost);
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
        IGCC.LookUp();
        yield return new WaitForSeconds(0.25f);
        yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(examineDialogue);
        IGCC.LookDown();
        selectedCard.Anim.PlayDeathAnimation();
        UnityEngine.Object.Destroy(selectedCard.gameObject, 2f);
        yield return new WaitForSeconds(0.4f);
    }

    /// <see cref="PostCardSelect"/>
    public static IEnumerator PostMultiSACardSelect(List<SelectableCard> cards, List<SpecialTriggeredAbility> selectedSAs, List<Trait> selectedTraits)
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
        string examineDialogue = "";
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
            string args = "";
            int size = selectedTraits.Count;
            for (int i = 0; i < size; i++)
            {
                args += (selectedTraits[i].ToString());
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
            if (examineDialogue != "") examineDialogue += " ";
            examineDialogue += string.Format(Localization.Translate("A [c:bR]Trait of {0}[c:]."), args);
        }
        if (selectedSAs.Count == 0 && selectedTraits.Count == 0) examineDialogue = string.Format(Localization.Translate("[c:bR]No Special Abilities. No Traits[c:]."));
        IGCC.LookUp();
        yield return new WaitForSeconds(0.25f);
        yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(examineDialogue);
        IGCC.LookDown();
        foreach (SelectableCard item2 in cards)
        {
            if (item2 != null && (item2.Info.SpecialAbilities.Count > 0 && selectedSAs.Contains(item2.Info.SpecialAbilities[0]) || item2.Info.traits.Count > 0 && selectedTraits.Contains(item2.Info.traits[0])))
            {
                item2.Anim.PlayDeathAnimation();
                UnityEngine.Object.Destroy(item2.gameObject, 2f);
            }
        }
        yield return new WaitForSeconds(0.4f);
    }

    /// <see cref="PostCardSelect"/>
    public static IEnumerator PostMultiSigilCardSelect(List<SelectableCard> cards, List<Ability> selectedSigils)
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
        IGCC.LookUp();
        yield return new WaitForSeconds(0.25f);
        yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(examineDialogue);
        IGCC.LookDown();
        foreach (SelectableCard item2 in cards)
        {
            if (item2 != null && item2.Info.Abilities.Count > 0 && selectedSigils.Contains(item2.Info.Abilities[0]))
            {
                item2.Anim.PlayDeathAnimation();
                UnityEngine.Object.Destroy(item2.gameObject, 2f);
            }
        }
        yield return new WaitForSeconds(0.4f);
    }

    public static IEnumerator PostMultiTribeCardSelect(List<SelectableCard> cards, List<Tribe> selectedTribes)
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
        else if (selectedTribes.Count > 0)
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
        IGCC.LookUp();
        yield return new WaitForSeconds(0.25f);
        yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(examineDialogue);
        IGCC.LookDown();
        foreach (SelectableCard item2 in cards)
        {
            if (item2 != null && item2.Info.tribes.Count > 0 && selectedTribes.Contains(item2.Info.tribes[0]))
            {
                item2.Anim.PlayDeathAnimation();
                UnityEngine.Object.Destroy(item2.gameObject, 2f);
            }
        }
        yield return new WaitForSeconds(0.4f);
    }

    public static void DestroyAllCards(List<SelectableCard> cards, bool playDeathAnim)
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

    public static Vector3 GetCardIndexLoc(Transform obj, int i)
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
        else if (i == 19)
        {
            vector = obj.position + new Vector3(UnityEngine.Random.value * 0.05f - 2.075f, 0f, UnityEngine.Random.value * 0.05f + 1.0f);
        }
        else if (i == 20)
        {
            vector = obj.position + new Vector3(UnityEngine.Random.value * 0.05f + 6.925f, 0f, UnityEngine.Random.value * 0.05f + 1.0f);
        }
        else
        {
            int z = i / 5;
            int x = i % 5;
            vector = obj.position + new Vector3(UnityEngine.Random.value * 0.05f + x * 1.5f - 0.575f, 0f, UnityEngine.Random.value * 0.05f + 5.0f - z * 2.0f);
        }
        return vector;
    }

    public static SelectableCard CreateCard(DeathCardCreationSequencer __instance, List<CardInfo> choices, List<SelectableCard> cards, Vector3 vector, bool hiddenAttack, bool hiddenHealth, bool instantSpawn)
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
    public static SelectableCard CreateConfirmCard(DeathCardCreationSequencer __instance, Vector3 vector, CardInfo info)
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

    // TODO: Merge all duplicate code in CreateCard methods
    /// <see cref="CreateCard"/>
    public static SelectableCard CreatePreviewCard(DeathCardCreationSequencer __instance, Vector3 vector, CardInfo info)
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
    public static IEnumerator FinalizeCard(DeathCardCreationSequencer __instance, SelectableCard preview)
    {
        IGCC.LookDown();
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
            string json = JsonParser.ParseCard(name, preview);
            Directory.CreateDirectory(Paths.PluginPath + "/IGCCExports/Cards");
            File.WriteAllText(Paths.PluginPath + "/IGCCExports/Cards/" + name + "_card.jldr2", json);
            Directory.CreateDirectory(Paths.PluginPath + "/IGCCExports/Artwork");
            File.WriteAllBytes(Paths.PluginPath + "/IGCCExports/Artwork/" + name + ".png", CloneTextureReadable(preview.Info.portraitTex.texture).EncodeToPNG());
            Texture2D emission = PortraitLoader.Instance.GetEmissionForPortrait(preview.Info.portraitTex.texture);
            if (emission != null)
            {
                File.WriteAllBytes(Paths.PluginPath + "/IGCCExports/Artwork/" + name + "_emission.png", CloneTextureReadable(emission).EncodeToPNG());
            }
            if (preview.Info.alternatePortrait != null)
            {
                File.WriteAllBytes(Paths.PluginPath + "/IGCCExports/Artwork/" + name + "_alt.png", CloneTextureReadable(preview.Info.alternatePortrait.texture).EncodeToPNG());
                Texture2D altEmission = PortraitLoader.Instance.GetEmissionForPortrait(preview.Info.alternatePortrait.texture);
                if (altEmission != null)
                {
                    File.WriteAllBytes(Paths.PluginPath + "/IGCCExports/Artwork/" + name + "_alt_emission.png", CloneTextureReadable(altEmission).EncodeToPNG());
                }
            }
            yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("The card has been created and exported to IGCCExports inside the BepInEx plugins folder. You need to restart the game for it to be added.");

        }
        if (selectedCard.Info.DisplayedNameEnglish.Contains("quit"))
        {
            preview.Anim.PlayDeathAnimation();
            UnityEngine.Object.Destroy(preview.gameObject, 0.5f);
            DestroyAllCards(cards, true);
            yield return new WaitForSeconds(0.55f);
        }
        else
        {
            DestroyAllCards(cards, true);
        }

    }

    // TODO: Merge all duplicate code in Create[Info] methods
    /// <see cref="CreatePortrait"/>
    public static IEnumerator CreateStepCard(DeathCardCreationSequencer __instance, IntRef ir)
    {
        IGCC.LookDown();
        SelectableCard selectedCard = null;
        List<CardInfo> choices = new List<CardInfo>();
        List<SelectableCard> cards = new List<SelectableCard>();
        Transform obj = ((List<Transform>)__instance.GetType().GetField("cardPositionMarkers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))[0];
        for (int i = 0; i <= 11; i++)
        {
            Vector3 vector = GetCardIndexLoc(obj, i < 11 ? i : 14);
            CardInfo c = ScriptableObject.CreateInstance<CardInfo>();
            CardModificationInfo addTo = new CardModificationInfo();
            switch (i)
            {
                case 0:
                    addTo.nameReplacement = "cost (set)";
                    break;
                case 1:
                    addTo.nameReplacement = "cost (add)";
                    break;
                case 2:
                    addTo.nameReplacement = "power";
                    break;
                case 3:
                    addTo.nameReplacement = "health";
                    break;
                case 4:
                    addTo.nameReplacement = "sigils";
                    break;
                case 5:
                    addTo.nameReplacement = "tribes";
                    break;
                case 6:
                    addTo.nameReplacement = "sp. abilities";
                    break;
                case 7:
                    addTo.nameReplacement = "portrait";
                    break;
                case 8:
                    addTo.nameReplacement = "rarity";
                    break;
                case 9:
                    addTo.nameReplacement = "complexity";
                    break;
                case 10:
                    addTo.nameReplacement = "alternate portrait";
                    break;
                //case 11:
                //     addTo.nameReplacement = "decals";
                //     break;
                case 11:
                    addTo.nameReplacement = "finish";
                    c.appearanceBehaviour.Add(CardAppearanceBehaviour.Appearance.RareCardBackground);
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
        Singleton<TextDisplayer>.Instance.ShowMessage("Choose a property.");
        foreach (SelectableCard item in cards)
        {
            item.SetInteractionEnabled(interactionEnabled: true);
            item.SetEnabled(enabled: true);
        }
        yield return new WaitUntil(() => selectedCard != null);
        Singleton<TextDisplayer>.Instance.Clear();
        ir.Value = cards.IndexOf(selectedCard) + 1;
        if (ir.Value == 12) ir.Value = -1;
        DestroyAllCards(cards, true);
        yield return new WaitForSeconds(0.55f);
    }

    public static Texture2D CloneTextureReadable(Texture2D source)
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
}