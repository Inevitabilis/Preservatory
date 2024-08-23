using PVStuffMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace PVStuff.Logic.ControllerParser;

internal class SlugController : Player.PlayerController
{
    #region init
    public static void Hook()
    {
        On.Player.Update += static (orig, self, eu) =>
        {
            orig(self, eu);
            if (self.controller is SlugController c && c.SlatedForDeletion) self.controller = null;
        };
    }
    void loginf(object e) => MainLogic.logger.LogInfo(e);

    public SlugController(string ID)
    {
        this.ID = ID;
        this.loader = new(this, ID);
#warning as of currently ticklimit is only impactful towards instant functions: the ticklimit is unused until we ran out of span instructions
        tickLimit ??= instantInstructions.Keys.Aggregate(0, Mathf.Max);
        loginf("SPANNED CONTROL INSTRUCTIONS");
        spannedControlInstructions.ForEach(x => loginf(x));
        loginf("INSTANT CONTROL INSTRUCTIONS");
        foreach (var key in instantInstructions.Keys)
        {
            loginf($"{key} - {instantInstructions[key]}");
        }
    }
    public enum EndAction
    {
        Stand,
        DeleteController,
        Loop
    }
    public bool SlatedForDeletion = false;


    private InstructionsLoader loader;
    internal int? tickLimit;
    internal string ID;
    internal EndAction endAction = EndAction.Stand;
    internal List<SpannedControlInstruction> spannedControlInstructions = [];
    internal Dictionary<int, ControlInstruction> instantInstructions = new();

    #endregion


    int controllerTimer = 0;
    int spanInstrIndex = 0;

    public override Player.InputPackage GetInput()
    {
        ControlInstruction? output = null;
        if(instantInstructions.TryGetValue(controllerTimer, out var InstantInstruction))
        {
            output = InstantInstruction;
        }
        else if(controllerTimer < tickLimit)
        {
            output = HandleSpannedLogic();
        }
        controllerTimer++;
        loginf("controller update tick fired. returning " + output);
        return (output ?? new ControlInstruction()).ToPackage();

    }

    private ControlInstruction HandleSpannedLogic()
    {
        //when the current instruction is valid, there's no need for any actions
        if(!InstructionReachedEnd)
        {
            return spannedControlInstructions[spanInstrIndex];
        }
        else
        {
            //in other case, we are looking for the next instruction with valid timespan
            for(int i = spanInstrIndex; i < spannedControlInstructions.Count; i++)
            {
                if(IsValidInstructionToUse(spannedControlInstructions[i]))
                {
                    spanInstrIndex = i;
                    return spannedControlInstructions[spanInstrIndex];
                }
            }
            //implicitly logic goes here when the replacement isn't found
            //we check whether there's customly defined tick limit and if its logic is applicable to fill the lack of input with standing
            if(controllerTimer < tickLimit)
            {
                return new();
            }
            //by now function officially reached end. we treat it accordingly
            switch(endAction)
            {
                case EndAction.Stand:
                    {
                        return new();
                    }
                case EndAction.Loop:
                    {
                        spanInstrIndex = 0;
                        return spannedControlInstructions[spanInstrIndex];
                    }
                case EndAction.DeleteController:
                    {
                        SlatedForDeletion = true;
                        return new();
                    }
                default:
                    {
                        return new();
                    }
            }
        }
    }
    bool InstructionReachedEnd => spannedControlInstructions[spanInstrIndex].span.end < controllerTimer;
    bool IsValidInstructionToUse(SpannedControlInstruction spanInstr) => spanInstr.span.start <= controllerTimer && controllerTimer < spanInstr.span.end; 
}
