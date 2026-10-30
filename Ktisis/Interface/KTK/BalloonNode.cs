using System;
using System.Drawing;
using System.Numerics;

using Dalamud.Interface;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using KamiToolKit.UiOverlay;

namespace Ktisis.Interface.KTK;

public enum BalloonBackground {
	Say,
	Party,
	Tell,
	Alliance,
	Yell,
	Shout,
	FC,
	LS,
	CWLS,
	Novice,
	PVP
}

public enum BalloonColor {
	Default,
	Lime,
	Orange,
	Violet,
	SkyBlue,
	Clay,
	LightJeans,
	GrassGreen,
	Gray,
	Pink,
	DarkJeans,
	Green,
	Purple,
	Brown,
	CloudyBlue,
	RoyalPurple
}

public class BalloonNode : OverlayNode {
	private SimpleNineGridNode BalloonBg;
	private SimpleNineGridNode BalloonGradient;
	private SimpleImageNode BalloonArrow;
	private TextNode TalkText;
	
	public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
	public override bool HideWithNativeUi => false;
	public override bool HideWithUiToggled => false;

	public BalloonBackground BgChoice;
	public BalloonColor ColorChoice;
	public string Dialog;
	public bool ArrowVisible;
	public float ArrowX;
	public uint FontSize;

	public BalloonNode(
		BalloonBackground bgChoice,
		BalloonColor colorChoice,
		string dialog,
		bool arrowVisible,
		float arrowX,
		uint fontSize
	) {
		this.BgChoice = bgChoice;
		this.ColorChoice = colorChoice;
		this.Dialog = dialog;
		this.ArrowVisible = arrowVisible;
		this.ArrowX = arrowX;
		this.FontSize = fontSize;

		this.BalloonBg = this.SetBalloonBg();
		this.BalloonGradient = this.SetBalloonGradient();
		this.BalloonArrow = this.SetBalloonArrow();
		this.TalkText = this.SetTalkText();

		this.BalloonBg.AttachNode(this);
		this.BalloonGradient.AttachNode(this);
		this.BalloonArrow.AttachNode(this);
		this.TalkText.AttachNode(this);
	}

	protected override void OnUpdate() {
		// update string
		this.TalkText.String = this.Dialog;
		this.TalkText.FontSize = this.FontSize;
		// update bg texture
		this.BalloonBg.TextureCoordinates = this.CoordinatesForBg();
		this.BalloonGradient.TextureCoordinates = this.CoordinatesForGradient();
		this.BalloonGradient.MultiplyColor = this.ColorForGradient();
		// update arrow visibility, position
		this.BalloonArrow.IsVisible = this.ArrowVisible;
		if (this.ArrowVisible)
			this.BalloonArrow.Position = new Vector2(Math.Clamp(this.ArrowX, 32.0f, 130.0f), 70.0f);
	}

	private SimpleNineGridNode SetBalloonBg() {
		return new SimpleNineGridNode() {
			TexturePath = "ui/uld/MiniTalkPlayer_hr1.tex",
			TextureSize = new Vector2(200.0f, 90.0f),
			TextureCoordinates = this.CoordinatesForBg(),
			Position = Vector2.Zero,
			Size = new Vector2(200.0f, 90.0f),
			TopOffset = 51.0f,
			BottomOffset = 37.0f,
			LeftOffset = 162.0f,
			RightOffset = 36.0f,
		};
	}

	private SimpleNineGridNode SetBalloonGradient() {
		return new SimpleNineGridNode() {
			TexturePath = "ui/uld/MiniTalkPlayer_hr1.tex",
			TextureSize = new Vector2(200.0f, 90.0f),
			TextureCoordinates = this.CoordinatesForGradient(),
			MultiplyColor = this.ColorForGradient(),
			Position = Vector2.Zero,
			Size = new Vector2(200.0f, 90.0f),
			TopOffset = 51.0f,
			BottomOffset = 37.0f,
			LeftOffset = 162.0f,
			RightOffset = 36.0f,
		};
	}

	private SimpleImageNode SetBalloonArrow() {
		return new SimpleImageNode() {
			TexturePath = "ui/uld/MiniTalkPlayer_hr1.tex",
			TextureSize = new Vector2(32.0f, 32.0f),
			TextureCoordinates = new Vector2(0, 992.0f),
			Position = new Vector2(49, 70),
			Size = new Vector2(32.0f, 32.0f)
		};
	}

	private TextNode SetTalkText() {
		return new TextNode() {
			Size = new Vector2(151.0f, 17.0f),
			Position = new Vector2(24.0f, 43.0f),
			TextColor = KnownColor.Black.Vector(),
			FontType = FontType.Axis,
			TextFlags = TextFlags.Ellipsis,
			AlignmentType = AlignmentType.Center,
			FontSize = this.FontSize,
			LineSpacing = 14,
			String = this.Dialog
		};
	}

	private Vector2 CoordinatesForBg() => this.BgChoice switch {
		BalloonBackground.Say => new Vector2(0, 0),
		BalloonBackground.Party => new Vector2(0, 90*1),
		BalloonBackground.Tell => new Vector2(0, 90*2),
		BalloonBackground.Alliance => new Vector2(0, 90*3),
		BalloonBackground.Yell => new Vector2(0, 90*4),
		BalloonBackground.Shout => new Vector2(0, 90*5),
		BalloonBackground.FC => new Vector2(0, 90*6),
		BalloonBackground.LS => new Vector2(0, 90*7),
		BalloonBackground.CWLS => new Vector2(0, 90*8),
		BalloonBackground.Novice => new Vector2(0, 90*9),
		BalloonBackground.PVP => new Vector2(0, 90*10),
		_ => new Vector2()
	};

	private Vector2 CoordinatesForGradient() => this.BgChoice switch {
		BalloonBackground.Say => new Vector2(200, 0),
		BalloonBackground.Party => new Vector2(200, 90 * 1),
		BalloonBackground.Tell => new Vector2(200, 90 * 2),
		BalloonBackground.Alliance => new Vector2(200, 90 * 3),
		BalloonBackground.Yell => new Vector2(200, 90 * 4),
		BalloonBackground.Shout => new Vector2(200, 90 * 5),
		BalloonBackground.FC => new Vector2(200, 90 * 6),
		BalloonBackground.LS => new Vector2(200, 90 * 7),
		BalloonBackground.CWLS => new Vector2(200, 90 * 8),
		BalloonBackground.Novice => new Vector2(200, 90 * 9),
		BalloonBackground.PVP => new Vector2(200, 90 * 10),
		_ => new Vector2()
	};

	private Vector3 ColorForGradient() => this.ColorChoice switch {
		BalloonColor.Default => new Vector3(83, 76, 58),
		BalloonColor.Lime => new Vector3(74, 74, 0),
		BalloonColor.Orange => new Vector3(87, 60, 28),
		BalloonColor.Violet => new Vector3(76, 48, 63),
		BalloonColor.SkyBlue => new Vector3(39, 70, 78),
		BalloonColor.Clay => new Vector3(72, 40, 22),
		BalloonColor.LightJeans => new Vector3(43, 58, 62),
		BalloonColor.GrassGreen => new Vector3(47, 62, 11),
		BalloonColor.Gray => new Vector3(50, 50, 50),
		BalloonColor.Pink => new Vector3(78, 50, 50),
		BalloonColor.DarkJeans => new Vector3(27, 39, 51),
		BalloonColor.Green => new Vector3(36, 58, 36),
		BalloonColor.Purple => new Vector3(40, 32, 46),
		BalloonColor.Brown => new Vector3(54, 44, 26),
		BalloonColor.CloudyBlue => new Vector3(40, 63, 80),
		BalloonColor.RoyalPurple => new Vector3(51, 29, 41),
		_ => new Vector3()
	} / 100.0f; // node MultiplyColor expects value from 0.0f to 1.0f
}
