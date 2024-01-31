using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVStuffMod;

internal class PlayerLogic
{
    internal static void ApplyHooks()
    {
        On.Player.Update += Player_Update;
    }
    #region Hooks
    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (!(self.room.world.name == "PV")) return;
        DeepDiveLogic(self);
    }
    #endregion
    #region Methods
    private static void DeepDiveLogic(Player player)
    {
        if (player.abstractCreature.Room.name != StaticStuff.stasisRoomName) return;

#warning hardcode
        if (player.mainBodyChunk.pos.y < 400) Array.ForEach(player.abstractCreature.Room.world.game.cameras, x => x.voidSeaMode = true);
        else Array.ForEach(player.abstractCreature.Room.world.game.cameras, x => x.voidSeaMode = false);



    }
    #endregion
}
