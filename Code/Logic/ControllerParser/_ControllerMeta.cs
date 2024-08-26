using PVStuffMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVStuff.Logic.ControllerParser;

internal class _ControllerMeta
{
    public void Startup()
    {
        SlugController.Hook();
        if(StaticStuff.devBuild)    DebugPrintKeyboardInputs.Startup();
    }
}
