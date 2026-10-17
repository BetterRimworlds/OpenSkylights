/*
 * This file is part of BetterRimworlds.OpenSkylights, a Better Rimworlds Project.
 *
 * Copyright © 2026 Theodore R. Smith
 * Author: Theodore R. Smith <hopeseekr@gmail.com>
 *   GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41
 *   https://github.com/BetterRimworlds/BetterRimworlds.OpenSkylights
 *
 * This file is licensed under the MIT License.
 */

using System.IO;
using System.Xml;
using UnityEngine;
using Verse;

namespace BetterRimworlds.OpenSkylights;

public class Settings : ModSettings
{
    public bool debugMode = false;

    public bool hideWindows = false;

    public bool sunlightHeatsRoom = false;

    public bool importedDubsSettings = false;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref debugMode, "brw.BetterRimworlds.OpenSkylights.debugMode", false);
        Scribe_Values.Look(ref hideWindows, "HideWindows", false);
        Scribe_Values.Look(ref sunlightHeatsRoom, "SunlightHeatsRoom", false);
        Scribe_Values.Look(ref importedDubsSettings, "importedDubsSettings", false);
    }

    public void ImportDubsOnce()
    {
        if (importedDubsSettings)
            return;

        if (hideWindows || sunlightHeatsRoom)
        {
            importedDubsSettings = true;
            TryWrite();
            return;
        }

        if (!TryReadDubs(out bool hide, out bool heat))
            return;

        hideWindows = hide;
        sunlightHeatsRoom = heat;
        importedDubsSettings = true;
        TryWrite();
    }

    void TryWrite()
    {
        if (Mod == null)
            return;

        try
        {
            Write();
        }
        catch (System.Exception ex)
        {
            Log.Warning("[OpenSkylights] Could not save imported settings: " + ex);
        }
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing_Standard = new Listing_Standard();
        listing_Standard.Begin(inRect);

        bool hideBefore = hideWindows;
        listing_Standard.CheckboxLabeled("hideWindows".Translate(), ref hideWindows, "hideWindowsTip".Translate());
        if (hideWindows != hideBefore)
            MapComp_Skylights.DirtyAllFrames();

        listing_Standard.CheckboxLabeled("SunlightHeatsRoom".Translate(), ref sunlightHeatsRoom, "SunlightHeatsRoomTip".Translate());
        listing_Standard.CheckboxLabeled("Print debug messages?", ref debugMode);

        listing_Standard.End();

        OpenSkylights.Settings.debugMode = debugMode;
    }

    static bool TryReadDubs(out bool hide, out bool heat)
    {
        hide = false;
        heat = false;
        string config = GenFilePaths.ConfigFolderPath;
        if (config.NullOrEmpty() || !Directory.Exists(config))
            return false;

        foreach (string file in Directory.GetFiles(config, "Mod_*.xml"))
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                XmlNodeList hideNodes = doc.GetElementsByTagName("HideWindows");
                XmlNodeList heatNodes = doc.GetElementsByTagName("SunlightHeatsRoom");
                if (hideNodes.Count == 0 || heatNodes.Count == 0)
                    continue;

                string name = Path.GetFileName(file);
                string body = doc.DocumentElement?.InnerXml ?? "";
                if (!IsDubsSettings(name, body))
                    continue;

                hide = IsTrue(hideNodes[0].InnerText);
                heat = IsTrue(heatNodes[0].InnerText);
                return true;
            }
            catch
            {
                // A foreign settings file can be unreadable. Keep looking.
            }
        }

        return false;
    }

    static bool IsDubsSettings(string name, string body)
    {
        string hay = name + "\n" + body;
        bool dubs = hay.IndexOf("Dubwise", System.StringComparison.OrdinalIgnoreCase) >= 0
            || hay.IndexOf("Dubs_Skylight", System.StringComparison.OrdinalIgnoreCase) >= 0
            || hay.IndexOf("DubsSkylight", System.StringComparison.OrdinalIgnoreCase) >= 0
            || (hay.IndexOf("Dubs", System.StringComparison.OrdinalIgnoreCase) >= 0
                && hay.IndexOf("Skylight", System.StringComparison.OrdinalIgnoreCase) >= 0);
        if (!dubs)
            return false;

        // Our own file also contains "Skylight". Never read it back as the Dubs file.
        if (hay.IndexOf("BetterRimworlds.OpenSkylights", System.StringComparison.OrdinalIgnoreCase) >= 0
            || hay.IndexOf("OpenSkylights", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return hay.IndexOf("Dubwise", System.StringComparison.OrdinalIgnoreCase) >= 0
                || hay.IndexOf("Dubs_Skylight", System.StringComparison.OrdinalIgnoreCase) >= 0;

        return true;
    }

    static bool IsTrue(string text)
    {
        if (text == null)
            return false;

        text = text.Trim();
        return text == "True" || text == "true" || text == "1";
    }
}
