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
        tickLimit = Mathf.Max(
            instantInstructions.Keys.Aggregate(0, Mathf.Max),
            spannedControlInstructions.Last().span.end
            );
        this.loader = new(this, ID);
    }
    public enum EndAction
    {
        Stand,
        DeleteController,
        Loop
    }
    public bool SlatedForDeletion = false;


    private InstructionsLoader loader;
    internal int tickLimit;
    internal string ID;
    internal EndAction endAction = EndAction.Stand;
    internal List<SpannedControlInstruction> spannedControlInstructions = [];
    internal Dictionary<int, ControlInstruction> instantInstructions = new();

    #endregion


    int controllerTimer = 0;
    int spanInstrIndex = 0;

    SpannedControlInstruction CurrentSpanInstruction => spannedControlInstructions[spanInstrIndex];

    public override Player.InputPackage GetInput()
    {
        ControlInstruction? output = null;
        if(instantInstructions.TryGetValue(controllerTimer, out var InstantInstruction)) //instant instructions are first priority
        {
            output = InstantInstruction;
        }
        else
        {
            if (instructionInRelationToTimer(CurrentSpanInstruction) == SpanInstrState.invalid)
            {
                //if current instruction is invalid, we try to find next that can be used in the future or now
                for (; spanInstrIndex < spannedControlInstructions.Count; spanInstrIndex++)
                {
                    if (instructionInRelationToTimer(CurrentSpanInstruction) == SpanInstrState.ongoing
                        || instructionInRelationToTimer(CurrentSpanInstruction) == SpanInstrState.pending) break;
                }
            }

            //if index didn't reach end, we found it
            //and if it was never invalid in the first place, this works as normal
            if(spanInstrIndex < spannedControlInstructions.Count)
            {
                //if next span instruction is in the future and currently it's not instant, slugcat stands
                if (instructionInRelationToTimer(CurrentSpanInstruction) == SpanInstrState.pending)
                {
                    output = new();
                }
                else if (instructionInRelationToTimer(CurrentSpanInstruction) == SpanInstrState.ongoing)
                { //and only now we get to do the normal logic "do thing if this is what you're currently doing"
                    output = CurrentSpanInstruction;
                }
            }
            else //if you looped through the rest of instructions and didn't find a valid one
            {
                output = new(); //this is a tradeoff. i suppose it could be done that looping immediately tries to find a new valid action,
                                //but in that i risk catching an infinite loop
                EndLogic();
            }
        }
        controllerTimer++;
        loginf("controller update tick fired. returning " + output);
        return (output ?? new ControlInstruction()).ToPackage();

    }

    private void EndLogic()
    {
        switch (endAction)
        {

            case EndAction.Loop:
                {
                    spanInstrIndex = 0;
                    break;
                }
            case EndAction.DeleteController:
                {
                    SlatedForDeletion = true;
                    break;
                }
            case EndAction.Stand:
            default:
                {
                    break;
                }
        }
    }
    enum SpanInstrState
    {
        pending = 1,
        ongoing = 2,
        invalid = 4
    }
    
    SpanInstrState instructionInRelationToTimer(SpannedControlInstruction spanInstr)
    {
        //when the instruction hasn't started, it is queued
        if (spanInstr.span.start > controllerTimer) return SpanInstrState.pending;
        //when the instruction started and hasn't ended, it's ongoing
        else if (spanInstr.span.start <= controllerTimer && controllerTimer < spanInstr.span.end) return SpanInstrState.ongoing;
        //the rest of cases we'd want to discard from future uses. either it has been played already or it's invalid
        else return SpanInstrState.invalid;
    }
}
