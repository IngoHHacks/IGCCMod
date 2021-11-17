using DiskCardGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IGCCMod.JSON
{
    public class JsonParser
    {

        public static string ParseCard(string name, SelectableCard preview)
        {
            string json = "{\r\n";
            json += GetJsonFromString("name", name) + ",\r\n";
            json += GetJsonFromString("displayedName", preview.Info.DisplayedNameEnglish.Replace("\"", "'")) + ",\r\n";
            json += GetJsonFromString("description", preview.Info.description.Replace("\"", "'")) + ",\r\n";
            if (preview.Info.metaCategories.Count > 0) json += GetJsonFromStringList("metaCategories", preview.Info.metaCategories) + ",\r\n";
            json += GetJsonFromString("cardComplexity", preview.Info.cardComplexity.ToString()) + ",\r\n";
            json += GetJsonFromString("temple", "Nature") + ",\r\n";
            json += GetJsonFromInt("baseAttack", preview.Info.Attack) + ",\r\n";
            json += GetJsonFromInt("baseHealth", preview.Info.Health) + ",\r\n";
            if (preview.Info.BloodCost > 0 || preview.Info.BonesCost == 0 && preview.Info.EnergyCost == 0) json += GetJsonFromInt("cost", preview.Info.BloodCost) + ",\r\n";
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
            List<Ability> baseAbilities = new List<Ability>();
            List<Ability> modAbilities = new List<Ability>();
            foreach (Ability ability in values)
            {
                if ((int)ability <= 99) baseAbilities.Add(ability);
                else modAbilities.Add(ability);
            }
            if (baseAbilities.Count > 0) all += "  " + "\"abilities\": [" + ParseListAbilityJson(baseAbilities) + "],\r\n";
            if (modAbilities.Count > 0) all += "  " + "\"customAbilities\": [" + ParseListModAbilityJson(modAbilities) + "  ],\r\n";
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
                string guid = id.ToString().Split(new char[] { '(' })[0];
                string name = id.ToString().Split(new char[] { '(' })[1].Split(new char[] { ')' })[0];
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
    }
}
