using PVStuffMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVStuff.Logic.ControllerParser;

internal class InstructionsLoader
{
    static string FolderPath => string.Concat(ModManager.ActiveMods.FirstOrDefault(x => x.id == "preservatory").path, "scripts");
    static string GetScriptFileName(string filename) => string.Concat(FolderPath, filename, ".txt");
    static void logerr(object e) => MainLogic.logger.LogError(e);
    void notifyOfError(object e) => logerr($"instruction loading error: reading from file: {ID}, line {lineindex + 1}\n{filestrings[lineindex]}\n, additional context: {e}");
    public InstructionsLoader(SlugController slugController, string ID)
    {
        owner = slugController;
        this.ID = ID;
    }
    SlugController owner;
    private string ID;

    enum ReadState
    {
        meta,
        commands
    }


    int lineindex = 0;
    string[] filestrings;
    ReadState state = ReadState.meta;
    private void ReadFromFile()
    {
        if(!File.Exists(GetScriptFileName(ID)))
        {
            logerr($"the slugcat script of filename {ID}.txt wasn't found in 'scripts' folder");
            return;
        }
        filestrings = File.ReadAllLines(GetScriptFileName(ID));

        for (lineindex = 0; lineindex < filestrings.Length; lineindex++)
        {
            if (filestrings[lineindex].StartsWith("//")) continue;
            switch (state)
            {
                case ReadState.meta:
                    {
                        ParseMetaInstruction();
                        break;
                    }
                case ReadState.commands:
                    {
                        ParseInputInstruction();
                        break;
                    }
                default:
                    {
                        notifyOfError("unknown readstate");
                        break;
                    }
            }

        }
    }

    private void ParseInputInstruction()
    {
        throw new NotImplementedException();
    }

    private void ParseMetaInstruction()
    {
        var str = filestrings[lineindex];
        if (str.StartsWith("#"))
        {
            state = ReadState.commands;
        }
        else if (str.Contains(":"))
        {
            var arguments = str.Split(':');
            if (arguments.Length != 2)
            {
                notifyOfError("the amount of ':' at the string wasn't 1");
                return;
            }
            ApplyMeta(arguments);
        }
        else notifyOfError("meta string didn't start with # and didn't contain ':'");


        void ApplyMeta(string[] arguments)
        {
            if (arguments[0] == "end action")
            {
                if (!TryParseAsEndAction(arguments[1], out SlugController.EndAction endAction))
                {
                    notifyOfError($"the argument \"{arguments[1]}\" isn't a valid end action");
                    return;
                }
                owner.endAction = endAction;
            }
            else if (arguments[0] == "instruction limit")
            {
                if (!int.TryParse(arguments[1], out int tickLimit))
                {
                    notifyOfError($"the argument \"{arguments[1]}\" isn't a valid int");
                    return;
                }
                owner.tickLimit = tickLimit;
            }
            else notifyOfError($"unknown meta command");
        }

    }
    bool TryParseAsEndAction(string str, out SlugController.EndAction endAction)
    {
        endAction = default;
        if (str == "loop")
        {
            endAction = SlugController.EndAction.Loop;
            return true;
        }
        else if (str == "stand")
        {
            endAction = SlugController.EndAction.Stand;
            return true;
        }
        else if (str == "terminate control")
        {
            endAction = SlugController.EndAction.DeleteController;
            return true;
        }
        return false;
    }


}
