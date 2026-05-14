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

public enum TalkBackground {
	// Talk_Basic_hr1
	Basic = 0,
	Thought = 1,
	Echo = 2,
	// Talk_Other_hr1
	Computer = 3,
	Yell = 4,
	Parchment = 5,
	Dragonspeak = 6,
	Linkpearl = 7,
	Narration = 8
}

public enum TalkCursor {
	// Talk_hr1
	None = 0,
	Pin = 1,
	Loop = 2
}

public class TalkNode : OverlayNode {
	private SimpleImageNode TalkBgNode;
	private NineGridNode SpeakerBgNode;
	private SimpleImageNode ClickyNode;
	private TextNode TalkText;
	private TextNode SpeakerText;

	public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
	public override bool HideWithNativeUi => false;
	public override bool HideWithUiToggled => false;

	public TalkBackground BgChoice;
	public TalkCursor CursorChoice;
	public string Speaker;
	public string Dialog;

	public TalkNode(
		TalkBackground bgChoice,
		TalkCursor cursorChoice,
		string speaker,
		string dialog
	) {
		this.BgChoice = bgChoice;
		this.CursorChoice = cursorChoice;
		this.Speaker = speaker;
		this.Dialog = dialog;

		this.TalkBgNode = this.SetTalkBg();
		this.SpeakerBgNode = this.SetSpeakerBg();
		this.ClickyNode = this.SetClicky();
		this.TalkText = this.SetTalkText();
		this.SpeakerText = this.SetSpeakerText();

		this.TalkBgNode.AttachNode(this);
		this.SpeakerBgNode.AttachNode(this);
		this.ClickyNode.AttachNode(this);
		this.TalkText.AttachNode(this);
		this.SpeakerText.AttachNode(this);
	}

	protected override void OnUpdate() {
		// update strings, font color
		this.TalkText.String = this.Dialog;
		this.TalkText.TextColor = this.TextColorForBg();
		this.SpeakerText.String = this.Speaker;

		// update clicky texture/visiblity
		this.ClickyNode.TextureCoordinates = this.CoordinatesForCursor();
		this.ClickyNode.IsVisible = this.CursorChoice != TalkCursor.None;

		// update bg texture/coordinates
		this.TalkBgNode.TexturePath = (int)this.BgChoice <= 2 ? "ui/uld/Talk_Basic_hr1.tex" : "ui/uld/Talk_Other_hr1.tex";
		this.TalkBgNode.TextureCoordinates = this.CoordinatesForBg();
	}

	private SimpleImageNode SetTalkBg() {
		return new SimpleImageNode() {
			Size = new Vector2(544.0f, 144.0f),
			WrapMode = WrapMode.Stretch,
			Scale = new Vector2(1.25f),
			TexturePath = (int)this.BgChoice <= 2 ? "ui/uld/Talk_Basic_hr1.tex" : "ui/uld/Talk_Other_hr1.tex",
			TextureCoordinates = this.CoordinatesForBg(),
			TextureSize = new Vector2(544.0f, 144.0f),
		};
	}

	private unsafe NineGridNode SetSpeakerBg() {
		var node = new NineGridNode() {
			Size = new Vector2(288.0f, 36.0f),
			Position = new Vector2(18.0f, 0.0f),
			Scale = new Vector2(1.25f),
			TopOffset = 0.0f,
			LeftOffset = 50.0f,
			RightOffset = 1.0f,
			BottomOffset = 0.0f
		};
		node.AddPart(new Part {
			TexturePath = "ui/uld/Talk_hr1.tex",
			TextureCoordinates = new Vector2(0.0f, 0.0f),
			Size = new Vector2(288.0f, 36.0f),
			Id = 0
		});

		return node;
	}

	private SimpleImageNode SetClicky() {
		return new SimpleImageNode() {
			Size = new Vector2(18.0f, 24.0f),
			Position = new Vector2(614.0f, 104.0f),
			WrapMode = WrapMode.Tile,
			TexturePath = "ui/uld/Talk_hr1.tex",
			TextureCoordinates = this.CoordinatesForCursor(),
			TextureSize = new Vector2(16.0f, 24.0f),
			IsVisible = this.CursorChoice != TalkCursor.None
		};
	}

	private TextNode SetTalkText() {
		return new TextNode() {
			Size = new Vector2(556.0f, 90.0f),
			Position = new Vector2(62.0f, 42.0f),
			TextColor = this.TextColorForBg(),
			FontType = FontType.Axis,
			TextFlags = TextFlags.WordWrap | TextFlags.MultiLine | TextFlags.OverflowHidden,
			FontSize = 14,
			LineSpacing = 18,
			String = this.Dialog
		};
	}

	private TextNode SetSpeakerText() {
		return new TextNode() {
			Size = new Vector2(300.0f, 36.0f),
			Position = new Vector2(60.0f, 2.0f),
			TextColor = KnownColor.White.Vector(),
			TextOutlineColor = KnownColor.Black.Vector(),
			FontType = FontType.Axis,
			FontSize = 18,
			AlignmentType = AlignmentType.Left,
			TextFlags = TextFlags.Edge | TextFlags.Ellipsis,
			String = this.Speaker
		};
	}


	private Vector4 TextColorForBg() => this.BgChoice is not (TalkBackground.Echo or TalkBackground.Computer or TalkBackground.Narration) ? KnownColor.Black.Vector() : KnownColor.White.Vector();

	private Vector2 CoordinatesForBg() => this.BgChoice switch {
		TalkBackground.Basic => new Vector2(0, 144*0),
		TalkBackground.Thought => new Vector2(0, 144*1),
		TalkBackground.Echo => new Vector2(0, 144*2),
		TalkBackground.Computer => new Vector2(0, 144*0),
		TalkBackground.Yell => new Vector2(0, 144*1),
		TalkBackground.Parchment => new Vector2(0, 144*2),
		TalkBackground.Dragonspeak => new Vector2(0, 144*3),
		TalkBackground.Linkpearl => new Vector2(0, 144*4),
		TalkBackground.Narration => new Vector2(0, 144*5),
		_ => new Vector2()
	};

	private Vector2 CoordinatesForCursor() => this.CursorChoice switch {
		TalkCursor.Pin => new Vector2(288, 0),
		TalkCursor.Loop => new Vector2(306, 0),
		_ => new Vector2()
	};
}
