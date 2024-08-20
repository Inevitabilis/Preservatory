using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVStuff.Logic.ControllerParser;

public class ControlInstruction
{
    public int HorizontalComponent => ((int)horizontalDirection) - 1;
    public int VerticalComponent => ((int)verticalDirection) - 1;

    public bool jmp = false;
    public bool thrw = false;
    public bool pckp = false;
    HorizontalDirection horizontalDirection = HorizontalDirection.None;
    VerticalDirection verticalDirection = VerticalDirection.None;

    enum HorizontalDirection
    {
        Left = 0,
        None = 1,
        Right = 2
    }
    enum VerticalDirection
    {
        Down = 0,
        None = 1,
        Up = 2,
    }
    public Player.InputPackage ToPackage()
    {
        return new Player.InputPackage(
            gamePad: false,
            controllerType: Options.ControlSetup.Preset.None,
            x: HorizontalComponent,
            y: VerticalComponent,
            jmp: this.jmp,
            thrw: this.thrw,
            pckp: this.pckp,
            mp: false,
            crouchToggle: false);
    }
    public ControlInstruction() { }
}

public struct Span
{
    public int start;
    public int end;
    public Span(int start, int end)
    {
        this.start = start;
        this.end = end;
    }
}
public class SpannedControlInstruction : ControlInstruction
{
    public SpannedControlInstruction(Span span) 
    {
        this.span = span;
    }
    Span span;
    public bool PastEnd(int timer) => span.end <= timer;
    public bool IsWithinTime(int timer) => span.start <= timer && timer <= span.end;
}

