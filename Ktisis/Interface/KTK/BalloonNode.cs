using System;
using System.Drawing;
using System.Numerics;

using Dalamud.Interface;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using KamiToolKit.Overlay.UiOverlay;
using KamiToolKit.Premade.Node.Simple;

namespace Ktisis.Interface.KTK;

public enum BalloonBackground {
	Say = 0,
	Party = 1,
	Tell = 2,
	Alliance = 3,
	Yell = 4,
	Shout = 5,
	FC = 6,
	LS = 7,
	CWLS = 8,
	Novice = 9,
	PVP = 10
}

public class BalloonNode : OverlayNode {
	private SimpleNineGridNode BalloonBg;
	private SimpleImageNode BalloonArrow;
	private TextNode TalkText;
	
	public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
	public override bool HideWithNativeUi => false;
	public BalloonBackground BgChoice;
	public string Dialog;
	public bool ArrowVisible;
	public float ArrowX;

	public BalloonNode(
		BalloonBackground bgChoice,
		string dialog,
		bool arrowVisible,
		float arrowX
	) {
		this.BgChoice = bgChoice;
		this.Dialog = dialog;
		this.ArrowVisible = arrowVisible;
		this.ArrowX = arrowX;

		this.BalloonBg = this.SetBalloonBg();
		this.BalloonArrow = this.SetBalloonArrow();
		this.TalkText = this.SetTalkText();
		
		this.BalloonBg.AttachNode(this);
		this.BalloonArrow.AttachNode(this);
		this.TalkText.AttachNode(this);
	}

	protected override void OnUpdate() {
		// update string
		this.TalkText.String = this.Dialog;
		// update bg texture
		this.BalloonBg.TextureCoordinates = this.CoordinatesForBg();
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
			FontSize = 12,
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
}
