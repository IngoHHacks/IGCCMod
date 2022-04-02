using DiskCardGame;
using InscryptionAPI.Card;
using InscryptionAPI.Guid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static InscryptionAPI.Card.AbilityManager;

namespace IGCCMod.JSON
{
    public class JsonParser
    {

        public static string ParseCard(string name, SelectableCard preview)
        {
            string json = "{\r\n";
            json += GetJsonFromString("name", name) + ",\r\n";
            json += GetJsonFromString("modPrefix", "IGCC") + ",\r\n";
            json += GetJsonFromString("displayedName", preview.Info.DisplayedNameEnglish.Replace("\"", "'")) + ",\r\n";
            json += GetJsonFromString("description", preview.Info.description.Replace("\"", "'")) + ",\r\n";
            if (preview.Info.metaCategories.Count > 0) json += GetJsonFromStringList("metaCategories", preview.Info.metaCategories) + ",\r\n";
            json += GetJsonFromString("cardComplexity", preview.Info.cardComplexity.ToString()) + ",\r\n";
            json += GetJsonFromString("temple", "Nature") + ",\r\n";
            json += GetJsonFromInt("baseAttack", preview.Info.Attack) + ",\r\n";
            json += GetJsonFromInt("baseHealth", preview.Info.Health) + ",\r\n";
            if (preview.Info.BloodCost > 0) json += GetJsonFromInt("bloodCost", preview.Info.BloodCost) + ",\r\n";
            if (preview.Info.BonesCost > 0) json += GetJsonFromInt("bonesCost", preview.Info.BonesCost) + ",\r\n";
            if (preview.Info.EnergyCost > 0) json += GetJsonFromInt("energyCost", preview.Info.EnergyCost) + ",\r\n";
            if (preview.Info.GemsCost.Count > 0) json += GetJsonFromStringList("gemsCost", preview.Info.GemsCost) + ",\r\n";
            json += GetJsonFromStringList("tribes", preview.Info.tribes) + ",\r\n";
            if (preview.Info.Abilities.Count > 0) json += GetJsonFromAbilityList(preview.Info.Abilities);
            if (preview.Info.appearanceBehaviour.Count > 0) json += GetJsonFromStringList("appearanceBehaviour", preview.Info.appearanceBehaviour) + ",\r\n";
            if (preview.Info.SpecialStatIcon != SpecialStatIcon.None) json += GetJsonFromString("specialStatIcon", preview.Info.SpecialStatIcon.ToString()) + ",\r\n";
            if (preview.Info.SpecialAbilities.Count > 0) json += GetJsonFromStringList("specialAbilities", preview.Info.SpecialAbilities) + ",\r\n";
            if (preview.Info.traits.Count > 0) json += GetJsonFromStringList("traits", preview.Info.traits) + ",\r\n";
            // TODO:
            if (preview.Info.iceCubeParams != null)
            {
                json += GetJsonFromString("iceCubeName", preview.Info.iceCubeParams.creatureWithin.name) + ",\r\n";
            }
            if (preview.Info.evolveParams != null)
            {
                json += GetJsonFromString("evolveIntoName", preview.Info.evolveParams.evolution.name) + ",\r\n";
                json += GetJsonFromInt("evolveTurns", preview.Info.evolveParams.turnsToEvolve) + ",\r\n";
            }
            json += GetJsonFromString("texture", name + ".png");
            json += "\r\n}";
            return json;
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
            if (values.Count > 0) all += "  " + "\"abilities\": [" + ParseListAbilityJson(values) + "],\r\n";
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
            foreach (Dictionary<string, string> dict in InscryptionAPI.Saves.ModdedSaveManager.SaveData.SaveData.Values)
            {
                List<KeyValuePair<string, string>> pairs = dict.ToList();
                foreach (KeyValuePair<string, string> pair in pairs)
                {
                    if (pair.Value == value.ToString())
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
                Plugin.Log.LogError("Ability " + value.ToString() + " not found!");
            }
            return guid.Substring(8);
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
    }
}
