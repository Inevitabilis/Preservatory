using PVStuffMod;
using System.Collections.Generic;
using System.Linq;
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
	void logerr(object e) => MainLogic.logger.LogError(e);

	public SlugController(string ID)
	{
		this.ID = ID;
		this.loader = new(this, ID);
		tickLimit = Mathf.Max(
			instantInstructions.Keys.Aggregate(0, Mathf.Max),
			spannedControlInstructions.Last().span.end
			);
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
		ControlInstruction output = GetCurrentInput();
		controllerTimer++;
		//loginf($"controller tick {controllerTimer}. returning " + output);
		return (output ?? new ControlInstruction()).ToPackage();
	}

	//shoutout to Bro for making this tenfold readable
	ControlInstruction GetCurrentInput()
	{
        if (instantInstructions.TryGetValue(controllerTimer, out var InstantInstruction)) //instant instructions are first priority
        {
            return InstantInstruction;
        }
        #region find next valid instruction if current has ended
        while (spanInstrIndex < spannedControlInstructions.Count && InstructionInRelationToTimer(CurrentSpanInstruction) == SpanInstrState.invalid) //if current instruction doesn't fit, find next
		{
			spanInstrIndex++;
		}

		if(spanInstrIndex >= spannedControlInstructions.Count) //if no valid ones were found, we stand and wait for all instant actions to work
		{
			if (controllerTimer >= tickLimit) EndLogic();
			return new();
		}
        #endregion

        #region execute current instruction
        //normal execution. if currently are no instant and no spanned actions, stand
        if (InstructionInRelationToTimer(CurrentSpanInstruction) == SpanInstrState.pending) return new();
		//finally, if current instruction is ongoing, work
		else if (InstructionInRelationToTimer(CurrentSpanInstruction) == SpanInstrState.ongoing) return CurrentSpanInstruction;
        #endregion
        logerr($"controller {ID} couldn't recognize what to do. current timer: {controllerTimer}, tick limit: {tickLimit} current index: {spanInstrIndex} (out of {spannedControlInstructions.Count-1})");
		return new();
    }

	private void EndLogic()
	{
		switch (endAction)
		{

			case EndAction.Loop:
				{
					spanInstrIndex = 0;
					controllerTimer = 0;
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

    SpanInstrState InstructionInRelationToTimer(SpannedControlInstruction spanInstr)
	{
		//when the instruction hasn't started, it is queued
		if (spanInstr.span.start > controllerTimer) return SpanInstrState.pending;
		//when the instruction started and hasn't ended, it's ongoing
		else if (spanInstr.span.start <= controllerTimer && controllerTimer < spanInstr.span.end) return SpanInstrState.ongoing;
		//the rest of cases we'd want to discard from future uses. either it has been played already or it's invalid
		else return SpanInstrState.invalid;
	}
}
