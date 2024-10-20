using PVStuffMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PVStuffMod.StaticStuff;

namespace PVStuff.Logic.ControllerParser;

internal static class DebugPrintKeyboardInputs
{
	//so you don't press backspace once and get flashing between testing and not
	const int ticksBetweenPresses = 20;

	static int pressDelay = 0;
	static bool logKeyboard;

	static ControlInstruction lastInstruction = new();
	static uint lastTickWithChangedInstruction = 0;
	static uint tickCounter = 0;

	static List<string> instructionReader = [];
	public static void Startup()
	{
		On.RainWorldGame.Update += static (orig, self) =>
		{
			orig(self);
			pressDelay--;
			//loginf("am i pressing B? " + Input.GetKeyDown(KeyCode.B));
			if(Input.GetKeyDown(KeyCode.B) && pressDelay < 0)
			{
				pressDelay = ticksBetweenPresses;

				if(logKeyboard)
				{
                    StaticStuff.loginf("the instructions of slugcat movement are: \n" + instructionReader.Aggregate(func: (acc, x) => acc += (x + "\n"), seed: ""));
					//reset logging state
					instructionReader = [];
					lastInstruction = new();
					lastTickWithChangedInstruction = 0;
                }
				logKeyboard = !logKeyboard;
			}
		};
		On.RWInput.PlayerInput_int += static (orig, number) =>
		{
			Player.InputPackage result = orig(number);
			if(logKeyboard && number == 0)
			{
				ControlInstruction thisTickInstruction = new(result);
				if(thisTickInstruction.ToString() != lastInstruction.ToString())
				{
					instructionReader.Add($"{lastTickWithChangedInstruction}-{tickCounter}: {lastInstruction}".ToLower());
					lastTickWithChangedInstruction = tickCounter;
					lastInstruction = thisTickInstruction;
				}
				tickCounter++;
				
			}
			return result;
			
		};

		
	}
}
