using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVStuff.Logic.ControllerParser;

internal class SlugController : Player.PlayerController
{
    class Immutable
    {
        public bool repeated = false;
        public SpannedControlInstruction[] spannedControlInstructions = [];
        public Dictionary<int, ControlInstruction> instantInstructions = new();
    }
    int controllerTimer = 0;
    int spanInstrIndex = 0;
    Immutable data;

    public override Player.InputPackage GetInput()
    {
        Player.InputPackage? output = null;
        if(data.instantInstructions.TryGetValue(controllerTimer, out var InstantInstruction))
        {
            output = InstantInstruction.ToPackage();
        }
        else
        {
            output = data.spannedControlInstructions[spanInstrIndex].ToPackage();
        }
        controllerTimer++;
        return output ?? new ControlInstruction().ToPackage();

    }

    private bool IncrementInstruction()
    {
        var array = data.spannedControlInstructions;
        for(int i = spanInstrIndex; i<array.Length; i++)
        {
            if (array[i].IsWithinTime(controllerTimer))
            {
                spanInstrIndex = i;
                return true;
            }
        }
        if (data.repeated) spanInstrIndex = 0;
    }
}
