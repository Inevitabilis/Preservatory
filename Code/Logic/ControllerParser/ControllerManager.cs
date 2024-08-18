using PVStuffMod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Player;

namespace PVStuff.Logic.ControllerParser;

internal static class ControllerManager
{
    static void loginf(object e) => MainLogic.logger.LogInfo(e);
    static void logerr(object e) => MainLogic.logger.LogError(e);


    static class FileParser
    {
        static string FolderPath => string.Concat(ModManager.ActiveMods.FirstOrDefault(x => x.id == "preservatory").path, "scripts");
        static string GetScriptFileName(string filename) => string.Concat(FolderPath, filename, ".txt");

        static bool TryParseInstruction(string str, int lineNumber, string ID, out ControlInstruction instr)
        {
            instr = default;
            str.Split(':');
            

        }
        static bool TryParseTimeCode(string str, int lineNumber, string ID, out Span span, )
        {
            span = default;
            string[] times = str.Split('-');
            if(!(times.Length == 2 || times.Length == 1) ) return false; //only supports span (2 numbers) or single time event (1 number)
            if(times.Length == 2)
            {
                if (int.TryParse(times[0], out int start) && int.TryParse(times[1], out int end))
                {
                    span.start = start;
                    span.end = end;
                    return true;
                }
                else
                {
                    logerr($"Invalid timecode at ID {ID}, line {lineNumber}: either of timestamps not recognized as int");
                }
            }
            else if(times.Length == 1)
            {
                if (int.TryParse(times[0], out int time))
                {
                    span.start = span.end = time;
                    return true;
                }
                else logerr($"Invalid timecode at ID {ID}, line {lineNumber}: singular timestamp not recognized as int");
            }
            return false;
        }
        static  ParseScript(string scriptID)
        {

        }

    }


}
