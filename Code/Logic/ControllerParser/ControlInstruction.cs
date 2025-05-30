﻿namespace PVStuff.Logic.ControllerParser;

public class ControlInstruction
{
	public int HorizontalComponent => ((int)horizontalDirection) - 1;
	public int VerticalComponent => ((int)verticalDirection) - 1;

	public bool jmp = false;
	public bool thrw = false;
	public bool pckp = false;
	public HorizontalDirection horizontalDirection = HorizontalDirection.None;
	public VerticalDirection verticalDirection = VerticalDirection.None;

	public enum HorizontalDirection
	{
		Left = 0,
		None = 1,
		Right = 2
	}
	public enum VerticalDirection
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
	public ControlInstruction(Player.InputPackage package)
	{
		jmp = package.jmp;
		thrw = package.thrw;
		pckp = package.pckp;
		horizontalDirection = (HorizontalDirection)(package.x + 1);
		verticalDirection = (VerticalDirection)(package.y + 1);
	}
	public ControlInstruction() { }

	public override string ToString()
	{
		return string.Concat(jmp ? "jmp " : "",
			thrw ? "thrw " : "",
			pckp ? "pckp " : "",
			horizontalDirection != HorizontalDirection.None ? horizontalDirection + " " : "",
			verticalDirection != VerticalDirection.None ? verticalDirection + " " : "");
	}
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
	public Span span;
	public override string ToString()
	{
		return span.start + "-" + span.end + ": " + base.ToString();
	}
}

