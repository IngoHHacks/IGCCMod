﻿using DiskCardGame;
using IGCCMod.Util;
using InscryptionAPI.Saves;
using UnityEngine;

namespace IGCCMod.JSON;

public class JsonParser
{
    public static string ParseCard(string name, SelectableCard preview)
    {
        string json = "{\r\n";
        json += GetJsonFromString("name", name) + ",\r\n";
        json += GetJsonFromString("modPrefix", "IGCC") + ",\r\n";
        json += GetJsonFromString("displayedName", preview.Info.DisplayedNameEnglish.Replace("\"", "'")) + ",\r\n";
        json += GetJsonFromString("description", preview.Info.description.Replace("\"", "'")) + ",\r\n";
        if (preview.Info.metaCategories.Count > 0)
        {
            json += GetJsonFromStringList("metaCategories", preview.Info.metaCategories) + ",\r\n";
        }

        json += GetJsonFromString("cardComplexity", preview.Info.cardComplexity.ToString()) + ",\r\n";
        json += GetJsonFromString("temple", "Nature") + ",\r\n";
        json += GetJsonFromInt("baseAttack", preview.Info.Attack) + ",\r\n";
        json += GetJsonFromInt("baseHealth", preview.Info.Health) + ",\r\n";
        if (preview.Info.BloodCost > 0)
        {
            json += GetJsonFromInt("bloodCost", preview.Info.BloodCost) + ",\r\n";
        }

        if (preview.Info.BonesCost > 0)
        {
            json += GetJsonFromInt("bonesCost", preview.Info.BonesCost) + ",\r\n";
        }

        if (preview.Info.EnergyCost > 0)
        {
            json += GetJsonFromInt("energyCost", preview.Info.EnergyCost) + ",\r\n";
        }

        if (preview.Info.GemsCost.Count > 0)
        {
            json += GetJsonFromStringList("gemsCost", preview.Info.GemsCost) + ",\r\n";
        }

        if (preview.Info.tribes.Count > 0)
        {
            json += GetJsonFromTribeList(preview.Info.tribes);
        }

        if (preview.Info.Abilities.Count > 0)
        {
            json += GetJsonFromAbilityList(preview.Info.Abilities);
        }

        if (preview.Info.appearanceBehaviour.Count > 0)
        {
            json += GetJsonFromStringList("appearanceBehaviour", preview.Info.appearanceBehaviour) + ",\r\n";
        }

        if (preview.Info.SpecialStatIcon != SpecialStatIcon.None)
        {
            json += GetJsonFromString("specialStatIcon", preview.Info.SpecialStatIcon.ToString()) + ",\r\n";
        }

        if (preview.Info.SpecialAbilities.Count > 0)
        {
            json += GetJsonFromStringList("specialAbilities", preview.Info.SpecialAbilities) + ",\r\n";
        }

        if (preview.Info.traits.Count > 0)
        {
            json += GetJsonFromStringList("traits", preview.Info.traits) + ",\r\n";
        }

        if (preview.Info.iceCubeParams != null)
        {
            json += GetJsonFromString("iceCubeName", preview.Info.iceCubeParams.creatureWithin.name) + ",\r\n";
        }

        if (preview.Info.evolveParams != null)
        {
            json += GetJsonFromString("evolveIntoName", preview.Info.evolveParams.evolution.name) + ",\r\n";
            json += GetJsonFromInt("evolveTurns", preview.Info.evolveParams.turnsToEvolve) + ",\r\n";
        }

        if (preview.Info.alternatePortrait != null)
        {
            json += GetJsonFromString("altTexture", name + "_alt.png") + ",\r\n";
        }

        Texture2D emission = PortraitLoader.Instance.GetEmissionForPortrait(preview.Info.portraitTex.texture);
        if (emission != null)
        {
            json += GetJsonFromString("emissionTexture", name + "_emission.png") + ",\r\n";
        }

        Texture2D altEmission = preview.Info.alternatePortrait != null
            ? PortraitLoader.Instance.GetEmissionForPortrait(preview.Info.alternatePortrait.texture)
            : null;
        if (altEmission != null)
        {
            json += GetJsonFromString("altEmissionTexture", name + "_alt_emission.png") + ",\r\n";
        }

        json += GetJsonFromString("texture", name + ".png");
        json += "\r\n}";
        return json;
    }

    private static string GetJsonFromTribeList(List<Tribe> values)
    {
        string all = "";
        if (values.Count > 0)
        {
            all += "  " + "\"tribes\": [" + ParseListTribeJson(values) + "],\r\n";
        }

        return all;
    }

    private static object ParseListTribeJson(List<Tribe> values)
    {
        string initial = "";
        foreach (Tribe value in values)
        {
            if (initial != "")
            {
                initial += ",";
            }

            if (value < Tribe.NUM_TRIBES)
            {
                initial += " \"" + value + "\"";
            }
            else
            {
                initial += " \"" + GetModdedGuid(value) + "\"";
            }
        }

        return initial + " ";
    }


    public static string GetModdedGuid(Tribe value)
    {
        bool found = false;
        string guid = null;
        Dictionary<string, Dictionary<string, object>>.ValueCollection values =
            ModdedSaveManager.SaveData.SaveData.Values;
        foreach (Dictionary<string, object> dict in values)
        {
            List<KeyValuePair<string, object>> pairs = dict.ToList();
            foreach (KeyValuePair<string, object> pair in pairs)
            {
                if (pair.Value.ToString() == value.ToString())
                {
                    found = true;
                    guid = pair.Key;
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }

        if (guid == null)
        {
            IGCC.Log.LogError("Tribe " + value + " not found!");
        }

        return guid.Substring(6);
    }

    public static string GetJsonFromString(string key, string value)
    {
        return "  " + "\"" + key + "\": " + "\"" + value + "\"";
    }

    public static string GetJsonFromInt(string key, int value)
    {
        return "  " + "\"" + key + "\": " + value;
    }

    public static string GetJsonFromStringList<T>(string key, List<T> values)
    {
        return "  " + "\"" + key + "\": " + "[" + ParseListJson(values) + "]";
    }

    public static string GetJsonFromAbilityList(List<Ability> values)
    {
        string all = "";
        if (values.Count > 0)
        {
            all += "  " + "\"abilities\": [" + ParseListAbilityJson(values) + "],\r\n";
        }

        return all;
    }

    public static string GetJsonFromStringArray(string key, string[] values)
    {
        return "  " + "\"" + key + "\": " + "[" + ParseArrayJson(values) + "]";
    }

    private static string ParseListJson<T>(List<T> values)
    {
        string initial = "";
        foreach (T value in values)
        {
            if (initial != "")
            {
                initial += ",";
            }

            initial += " \"" + value + "\"";
        }

        return initial + " ";
    }

    private static string ParseListAbilityJson(List<Ability> values)
    {
        string initial = "";
        foreach (Ability value in values)
        {
            if (initial != "")
            {
                initial += ",";
            }

            if (value < Ability.NUM_ABILITIES)
            {
                initial += " \"" + value + "\"";
            }
            else
            {
                initial += " \"" + GetModdedGuid(value) + "\"";
            }
        }

        return initial + " ";
    }


    private static string GetModdedGuid(Ability value)
    {
        bool found = false;
        string guid = null;
        Dictionary<string, Dictionary<string, object>>.ValueCollection values =
            ModdedSaveManager.SaveData.SaveData.Values;
        foreach (Dictionary<string, object> dict in values)
        {
            List<KeyValuePair<string, object>> pairs = dict.ToList();
            foreach (KeyValuePair<string, object> pair in pairs)
            {
                if (pair.Value.ToString() == value.ToString())
                {
                    found = true;
                    guid = pair.Key;
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }

        if (guid == null)
        {
            IGCC.Log.LogError("Ability " + value + " not found!");
        }

        return guid.Substring(8);
    }

    private static string ParseArrayJson(string[] values)
    {
        string initial = "";
        foreach (string value in values)
        {
            if (initial != "")
            {
                initial += ",";
            }

            initial += " \"" + value + "\"";
        }

        return initial + " ";
    }
}