using PVStuffMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PVStuff.Logic.ControllerParser;

internal class SlugController : Player.PlayerController
{
    #region init
    public SlugController(string ID)
    {
        this.ID = ID;
        this.loader = new(this, ID);
    }
    public enum EndAction
    {
        Stand,
        DeleteController,
        Loop
    }


    private InstructionsLoader loader;
    public int? tickLimit = null;
    public string ID;
    public EndAction endAction = EndAction.Stand;
    public List<SpannedControlInstruction> spannedControlInstructions = [];
    public Dictionary<int, ControlInstruction> instantInstructions = new();

    #endregion


    int controllerTimer = 0;
    int spanInstrIndex = 0;

    public override Player.InputPackage GetInput()
    {


        Player.InputPackage? output = null;
        if(instantInstructions.TryGetValue(controllerTimer, out var InstantInstruction))
        {
            output = InstantInstruction.ToPackage();
        }
        else
        {
            output = spannedControlInstructions[spanInstrIndex].ToPackage();
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
