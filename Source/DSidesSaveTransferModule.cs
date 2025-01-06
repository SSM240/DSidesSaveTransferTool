using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.DSidesSaveTransferTool;

public class DSidesSaveTransferModule : EverestModule
{
    public static DSidesSaveTransferModule Instance { get; private set; }

    //public override Type SettingsType => typeof(DSidesSaveTransferSettings);
    //public static DSidesSaveTransferSettings Settings => (DSidesSaveTransferSettings) Instance._Settings;

    public DSidesSaveTransferModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(DSidesSaveTransferModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(DSidesSaveTransferModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {

    }

    public override void LoadContent(bool firstLoad)
    {
        base.LoadContent(firstLoad);
    }

    public override void Unload()
    {

    }
}

public static class Commands
{
    [Command("transfer_dside_data", "Transfers data from old D-Sides to new D-Sides")]
    public static void CmdTransfer(string param = "")
    {
        if (SaveData.Instance == null)
        {
            Engine.Commands.Log("No save file selected.", Color.Yellow);
            return;
        }
        LevelSetStats nameGuyLevelSetStats = SaveData.Instance.LevelSetRecycleBin.Find(s => s.Name == "nameguysdsidespack/0");
        LevelSetStats nameGuyMuseumLevelSetStats = SaveData.Instance.LevelSetRecycleBin.Find(s => s.Name == "nameguysdsidespack/1");
        LevelSetStats monikaLevelSetStats = SaveData.Instance.LevelSets.Find(s => s.Name == "monikadsidespack/0");
        LevelSetStats monikaMuseumLevelSetStats = SaveData.Instance.LevelSets.Find(s => s.Name == "monikadsidespack/1");
        if (nameGuyLevelSetStats == null)
        {
            Engine.Commands.Log("Could not find old D-Sides save data.", Color.Yellow);
            return;
        }
        if (monikaLevelSetStats == null)
        {
            Engine.Commands.Log("Could not find new D-Sides data. Are you on the most recent D-Sides version?", Color.Yellow);
            return;
        }
        if ((Engine.Scene is Overworld) && ((SaveData.Instance?.LevelSet ?? "Celeste") == "monikadsidespack/1"))
        {
            Engine.Commands.Log("Please view a different level set, as using the command in the Museum level set crashes for some reason.", Color.Yellow);
            return;
        }
        if (param != "confirm")
        {
            string warning = "";
            if (monikaLevelSetStats.UnlockedAreas > 0 || monikaMuseumLevelSetStats.UnlockedAreas > 0)
            {
                warning = "\n- RUNNING THIS COMMAND WILL OVERWRITE YOUR SAVE DATA FOR THE CURRENT D-SIDES. PROCEED WITH CAUTION.";
            }
            string message = $"""
            PLEASE NOTE: {warning}
            - Make sure you've backed up your save file first, in case anything goes wrong unexpectedly.
              The current save file is `{SaveData.Instance.FileSlot}.celeste` in the Saves folder.
                
            To proceed, use the command `transfer_dside_data confirm`.
            """;
            Engine.Commands.Log(message, Calc.HexToColor("eeb4ff"));
            return;
        }
        try
        {
            List<AreaStats> newAreas = new();
            foreach (AreaStats area in nameGuyLevelSetStats.Areas)
            {
                string monikaSID = area.SID.Replace("nameguys", "monika");
                int id = monikaLevelSetStats.Areas.Find(area => area.SID == monikaSID).ID;
                AreaStats newArea = area.CloneWithID(id); // this automagically changes the SID too apparently
                newAreas.Add(newArea);
            }
            monikaLevelSetStats.Areas = newAreas;
            monikaLevelSetStats.UnlockedAreas = nameGuyLevelSetStats.UnlockedAreas;
            monikaLevelSetStats.TotalStrawberries = nameGuyLevelSetStats.TotalStrawberries;

            List<AreaStats> newAreasMuseum = new();
            foreach (AreaStats area in nameGuyMuseumLevelSetStats.Areas)
            {
                string monikaSID = area.SID.Replace("nameguys", "monika");
                int id = monikaMuseumLevelSetStats.Areas.Find(area => area.SID == monikaSID).ID;
                AreaStats newArea = area.CloneWithID(id);
                newAreasMuseum.Add(newArea);
            }
            monikaMuseumLevelSetStats.Areas = newAreasMuseum;
            monikaMuseumLevelSetStats.UnlockedAreas = nameGuyMuseumLevelSetStats.UnlockedAreas;
            monikaMuseumLevelSetStats.TotalStrawberries = nameGuyMuseumLevelSetStats.TotalStrawberries;
        }
        catch (Exception e)
        {
            Engine.Commands.Log("Something went wrong with the transfer. You should revert the file to the backup you made.", Color.Yellow);
            Engine.Commands.Log("Stack trace:", Color.Yellow);
            Engine.Commands.Log(e.ToString(), Color.Yellow);
            Logger.Log(LogLevel.Error, nameof(DSidesSaveTransferModule), "Error when transferring save data:");
            Logger.Log(LogLevel.Error, nameof(DSidesSaveTransferModule), e.ToString());
            return;
        }
        if (Engine.Scene is Overworld overworld && overworld.IsCurrent<OuiChapterSelect>())
        {
            Engine.Commands.Log("Transfer successful! Returning to main menu to properly save changes...", Calc.HexToColor("44ff7c"));
            Audio.Play("event:/ui/main/button_back");
            overworld.Goto<OuiMainMenu>();
            overworld.Maddy.Hide();
        }
        else
        {
            Engine.Commands.Log("Transfer successful! Return to file select to properly save the changes.", Calc.HexToColor("44ff7c"));
        }
    }

    public static AreaStats CloneWithID(this AreaStats stats, int id)
    {
        AreaStats areaStats = new AreaStats
        {
            ID = id,
            Cassette = stats.Cassette
        };
        for (int i = 0; i < stats.Modes.Length; i++)
        {
            areaStats.Modes[i] = stats.Modes[i].Clone();
        }

        return areaStats;
    }
}
