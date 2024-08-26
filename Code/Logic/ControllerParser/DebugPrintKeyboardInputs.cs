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

    public static void Startup()
    {
        On.RainWorld.Update += static (orig, self) =>
        {
            orig(self);
            pressDelay--;
            if(Input.GetKeyDown(KeyCode.Backspace) && pressDelay < 0)
            {
                pressDelay = ticksBetweenPresses;
                logKeyboard = !logKeyboard;
            }
        };
        On.RWInput.PlayerInput_int += static (orig, number) =>
        {
            Player.InputPackage result = orig(number);
            if(logKeyboard)
            {
                ControlInstruction instruction = new(result);
                StaticStuff.loginf(instruction);
            }
            return result;
            
        };

        
    }
}
