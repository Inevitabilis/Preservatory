using PVStuffMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVStuff.Logic.ControllerParser;

internal static class _ControllerMeta
{
    public static void Startup()
    {
        SlugController.Hook();
        if(StaticStuff.devBuild)    DebugPrintKeyboardInputs.Startup();
    }
}
