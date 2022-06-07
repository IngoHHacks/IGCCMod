using BepInEx;
using DiskCardGame;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IGCCMod.Util
{
    public class PortraitLoader
    {
        private static PortraitLoader instance;

        public static PortraitLoader Instance
        {
            get { 
                if (instance == null)
                {
                    instance = new PortraitLoader();
                }
                return instance; 
            }
        }

        private List<Texture2D> portraits = new List<Texture2D>();
        private List<Texture2D> emissions = new List<Texture2D>();

        private Dictionary<Texture2D, string> portraitToName = new Dictionary<Texture2D, string>();
        private Dictionary<string, Texture2D> nameToEmission = new Dictionary<string, Texture2D>();

        public PortraitLoader()
        {
            // Load textures from cards
            // TODO: Remove duplicates
            Singleton<TextDisplayer>.Instance.ShowMessage("Loading portraits.");
            Resources.LoadAll("art/cards/");
            Texture2D[] textures = (Texture2D[])Resources.FindObjectsOfTypeAll(typeof(Texture2D));
            int c1 = 0;
            int c2 = 0;
            foreach (Texture2D texture in textures)
            {
                if (texture.width == 114 && texture.height == 94 && texture.name.StartsWith("portrait"))
                {
                    if (!texture.name.EndsWith("_emission"))
                    {
                        c1++;
                        portraits.Add(texture);
                        portraitToName.Add(texture, texture.name + "_emission");
                    }
                    else
                    {
                        c2++;
                        emissions.Add(texture);
                        nameToEmission.Add(texture.name, texture);
                    }
                }
            }
            int c3 = 0;
            int c4 = 0;
            foreach (string file in Directory.GetFiles(Paths.PluginPath, "*.png", SearchOption.AllDirectories))
            {

                byte[] pngBytes = File.ReadAllBytes(file);
                Texture2D texture = new Texture2D(2, 2);
                ImageConversion.LoadImage(texture, pngBytes);
                texture.name = file.Substring(0, file.Length-4);
                if (texture.width == 114 && texture.height == 94)
                {
                    if (!texture.name.EndsWith("_emission"))
                    {
                        c3++;
                        portraits.Add(texture);
                        portraitToName.Add(texture, texture.name + "_emission");
                    }
                    else
                    {
                        c4++;
                        emissions.Add(texture);
                        nameToEmission.Add(texture.name, texture);
                    }
                }
            }
            Singleton<TextDisplayer>.Instance.ShowUntilInput($"Loaded {c1 + c3} portraits ({c3} modded) and {c2 + c4} emissions ({c4} modded)");
        }

        public List<Texture2D> GetPortraits()
        {
            return portraits;
        }

        public List<Texture2D> GetEmissions()
        {
            return emissions;
        }

        public Texture2D GetEmissionForPortrait(Texture2D tex)
        {
            if (portraitToName.ContainsKey(tex))
            {
                if (nameToEmission.ContainsKey(portraitToName[tex]))
                {
                    return nameToEmission[portraitToName[tex]];
                }
            }
            return null;
        }
    }
}
