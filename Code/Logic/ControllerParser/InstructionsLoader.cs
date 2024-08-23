using PVStuffMod;
using System;
using System.IO;
using System.Linq;

namespace PVStuff.Logic.ControllerParser;

internal class InstructionsLoader
{
    //preservatory specific path for scripts
    static string FolderPath => Path.Combine(ModManager.ActiveMods.FirstOrDefault(x => x.id == "preservatory").path, "scripts");
    //lookup scripts by ID
    static string GetScriptFileName(string filename) => Path.Combine(FolderPath, filename + ".txt");
    //shortcut to logging errors
    static void logerr(object e) => MainLogic.logger.LogError(e);
    //shortcut to logging errors WITH context of current execution, features filename, line and string where it failed, as well as allows additional context
    void notifyOfError(object e) => logerr($"instruction loading error: reading from file: {ID}, line {lineindex + 1}\n{filestrings[lineindex]}\n, additional context: {e}");

    //the loader should know two things: what to load and where to dump parsed progress
    public InstructionsLoader(SlugController slugController, string ID)
    {
        owner = slugController;
        this.ID = ID;
        ReadFromFile();
    }
    SlugController owner;
    private string ID;
    //readstate is responsible for figuring out whether we are reading the meta part or switched to commands
    enum ReadState
    {
        meta,
        commands
    }

    //by design it is intended that these three parameters are available across the whole class, which eases logging and passing them into functions
    int lineindex = 0;
    string[] filestrings = [];
    ReadState state = ReadState.meta;
    private void ReadFromFile()
    {
        if(!File.Exists(GetScriptFileName(ID)))
        {
            //the default state of controller is no commands and standing still on reaching end, so we can just
            logerr($"the slugcat script of filename {ID}.txt wasn't found in 'scripts' folder\n" +
                $"was looking for {GetScriptFileName(ID)}");
            return;
        }
        filestrings = File.ReadAllLines(GetScriptFileName(ID));

        for (lineindex = 0; lineindex < filestrings.Length; lineindex++)
        {
            //the comments shall not be read
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


    #region meta parsing
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

            ApplyMeta(arguments);
        }
        else notifyOfError("meta string didn't start with # and didn't contain ':'");


        void ApplyMeta(string[] arguments)
        {
            if (arguments.Length != 2)
            {
                notifyOfError("the amount of ':' at the string wasn't 1");
                return;
            }
            arguments[1] = arguments[1].TrimStart(' ');
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

    #endregion

    #region input instructions
    private void ParseInputInstruction()
    {
        string str = filestrings[lineindex];
        string[] arguments = str.Split(':');
        ApplyInput(arguments);
    }

    private void ApplyInput(string[] arguments)
    {
        if (arguments.Length != 2)
        {
            notifyOfError("the amount of ':' wasn't 1, unknown parsing request");
            return;
        }
        if(int.TryParse(arguments[0], out int value))
        {
            ApplyInstant(value, arguments[1]);
        }
        else if (arguments[0].Contains('-'))
        {
            ApplySpanInstruction(arguments[0], arguments[1]);
        }
        else
        {
            notifyOfError("the string wasn't recognized as either instant (is not number) or spanned (no '-')");
        }

    }

    private void ApplySpanInstruction(string timespan, string parameters)
    {
        string[] dates = timespan.Split('-');
        if (dates.Length != 2)
        {
            notifyOfError("for spanned instruction one '-' must be present");
            return;
        }
        if(!int.TryParse(dates[0], out int start) || !int.TryParse(dates[1], out int end))
        {
            notifyOfError("couldn't recognize int numbers for span");
            return;
        }
        Span span = new Span(start, end);
        string[] arguments = parameters.TrimStart(' ').Split(' ');
        SpannedControlInstruction instruction = new(span);
        Array.ForEach(arguments, argument => TryApplyToInstruction(instruction, argument));
        owner.spannedControlInstructions.Add(instruction);
    }

    private void ApplyInstant(int timestamp, string parameters)
    {
        string[] arguments = parameters.Split(' ');
        ControlInstruction instruction = new();
        Array.ForEach(arguments, argument => TryApplyToInstruction(instruction, argument));
        owner.instantInstructions.Add(timestamp, instruction);

    }

    private void TryApplyToInstruction(ControlInstruction instruction, string argument)
    {
        switch (argument)
        {
            case "":
                {
                    break;
                }
            case "left":
                {
                    instruction.horizontalDirection = ControlInstruction.HorizontalDirection.Left;
                    break;
                }
            case "right":
                {
                    instruction.horizontalDirection = ControlInstruction.HorizontalDirection.Right;
                    break;
                }
            case "up":
                {
                    instruction.verticalDirection = ControlInstruction.VerticalDirection.Up;
                    break;
                }
            case "down":
                {
                    instruction.verticalDirection = ControlInstruction.VerticalDirection.Down;
                    break;
                }
            case "jump":
            case "jmp":
                {
                    instruction.jmp = true;
                    break;
                }
            case "throw":
            case "thrw":
                {
                    instruction.thrw = true;
                    break;
                }
            case "pickup":
            case "pckup":
            case "pckp":
                {
                    instruction.pckp = true;
                    break;
                }
            default:
                {
                    notifyOfError($"argument '{argument}' was not found within the list of valid inputs");
                    break;
                }
        }

    }
    #endregion

}
