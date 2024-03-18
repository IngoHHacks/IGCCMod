using BepInEx;
using DiskCardGame;
using UnityEngine;

namespace IGCCMod.Util;

public class PortraitLoader
{
    private static PortraitLoader instance;
    private readonly List<Texture2D> emissions = new();
    private readonly Dictionary<string, Texture2D> nameToEmission = new();

    private readonly List<Texture2D> portraits = new();

    private readonly Dictionary<Texture2D, string> portraitToName = new();

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
                    this.portraits.Add(texture);
                    this.portraitToName.Add(texture, texture.name + "_emission");
                }
                else
                {
                    c2++;
                    this.emissions.Add(texture);
                    this.nameToEmission.Add(texture.name, texture);
                }
            }
        }

        int c3 = 0;
        int c4 = 0;
        foreach (string file in Directory.GetFiles(Paths.PluginPath, "*.png", SearchOption.AllDirectories))
        {
            byte[] pngBytes = File.ReadAllBytes(file);
            Texture2D texture = new(2, 2);
            texture.LoadImage(pngBytes);
            texture.name = file.Substring(0, file.Length - 4);
            if (texture.width == 114 && texture.height == 94)
            {
                if (!texture.name.EndsWith("_emission"))
                {
                    c3++;
                    this.portraits.Add(texture);
                    this.portraitToName[texture] = texture.name + "_emission";
                }
                else
                {
                    c4++;
                    this.emissions.Add(texture);
                    this.nameToEmission[texture.name] = texture;
                }
            }
        }

        Singleton<TextDisplayer>.Instance.ShowUntilInput(
            $"Loaded {c1 + c3} portraits ({c3} modded) and {c2 + c4} emissions ({c4} modded)");
    }

    public static PortraitLoader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PortraitLoader();
            }

            return instance;
        }
    }

    public List<Texture2D> GetPortraits()
    {
        return this.portraits;
    }

    public List<Texture2D> GetEmissions()
    {
        return this.emissions;
    }

    public Texture2D GetEmissionForPortrait(Texture2D tex)
    {
        if (this.portraitToName.ContainsKey(tex))
        {
            if (this.nameToEmission.ContainsKey(this.portraitToName[tex]))
            {
                return this.nameToEmission[this.portraitToName[tex]];
            }
        }

        return null;
    }
}