using System.Drawing;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using KamiToolKit.Timelines;
using KamiToolKit.UiOverlay;

namespace Ktisis.Interface.KTK;

public unsafe class HintNode : OverlayNode {
	private IconImageNode SpeakerImage;
	private SimpleNineGridNode BTextBg;
	private SimpleNineGridNode SpeakerBg;
	private TextNode SpeakerText;
	private TextNode BText;
	private ImageNode? Countdown;
	private SimpleComponentNode ComponentNode;

	public override OverlayLayer OverlayLayer => OverlayLayer.Foreground;
	public override bool HideWithNativeUi => false;
	public override bool HideWithUiToggled => false;

	protected override void OnUpdate() { }

	public HintNode(
		uint iconId,
		string hint,
		int hintNum,
		int? countdownFrames
	) {
		this.SpeakerImage = new IconImageNode() {
			IconId = iconId,
			TextureSize = new Vector2(640.0f, 512.0f),
			Size = new Vector2(320.0f, 256.0f),
			Position = new Vector2(-99.0f, -155.0f),
			FitTexture = true
		};
		this.BTextBg = new SimpleNineGridNode() {
			TexturePath = "ui/uld/BattleTalk_hr1.tex",
			TextureSize = new Vector2(128.0f, 48.0f),
			TopOffset = 20.0f,
			BottomOffset = 26.0f,
			LeftOffset = 48.0f,
			RightOffset = 48.0f,
			Size = new Vector2(625.0f, 64.0f),
			Position = new Vector2(0.0f, 12.0f)
		};
		this.SpeakerBg = new SimpleNineGridNode() {
			TexturePath = "ui/uld/BattleTalkNameBase_hr1.tex",
			TextureSize = new Vector2(188.0f, 18.0f),
			LeftOffset = 24.0f,
			Size = new Vector2(192.0f, 18.0f),
			Position = new Vector2(0.0f, 4.0f)
		};
		this.SpeakerText = new TextNode() {
			Size = new Vector2(167.0f, 25.0f),
			Position = new Vector2(5.0f, 0.0f),
			TextColor = KnownColor.White.Vector(),
			TextOutlineColor = KnownColor.Black.Vector(),
			FontType = FontType.Axis,
			FontSize = 14,
			AlignmentType = AlignmentType.Left,
			TextFlags = TextFlags.Edge,
			String = $"Ktisis Tip #{hintNum}"
		};
		this.BText = new TextNode() {
			Size = new Vector2(576.0f, 44.0f),
			Position = new Vector2(22.0f, 22.0f),
			TextColor = KnownColor.Black.Vector(),
			TextOutlineColor = KnownColor.Black.Vector(),
			FontType = FontType.Axis,
			FontSize = 16,
			AlignmentType = AlignmentType.Left,
			TextFlags = TextFlags.WordWrap | TextFlags.MultiLine | TextFlags.OverflowHidden,
			LineSpacing = 18,
			String = hint
		};
		this.ComponentNode = new SimpleComponentNode() {
			Position = new Vector2(-99.0f, -155.0f),
			Size = new Vector2(749.0f, 256.0f)
		};

		this.ComponentNode.AttachNode(this);
		this.SpeakerImage.AttachNode(this);
		this.BTextBg.AttachNode(this);
		this.SpeakerBg.AttachNode(this);
		this.SpeakerText.AttachNode(this);
		this.BText.AttachNode(this);
		if (countdownFrames != null) {
			this.SetCountdown(countdownFrames.Value);
			this.Countdown?.AttachNode(this);
			this.Timeline?.PlayAnimation(101);
		}

		this.ComponentNode.AddEvent(AtkEventType.MouseDown, this.DetachNode);
		this.AddEvent(AtkEventType.TimelineActiveLabelChanged, this.DetachNode);
	}

	private void SetCountdown(int countdownFrames) {
		this.Countdown = new ImageNode() {
			Size = new Vector2(20.0f, 20.0f),
			Position = new Vector2(592.0f, 40.0f),
			WrapMode = WrapMode.Tile,
			NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents
		};
		foreach (var yIndex in Enumerable.Range(0,9))
		foreach (var xIndex in Enumerable.Range(0,10)) {
			var coordinate = new Vector2(xIndex * 20.0f, yIndex * 20.0f);
			this.Countdown.AddPart(new Part() {
				TexturePath = "ui/uld/BattleTalk_Timer_hr1.tex",
				TextureCoordinates = coordinate,
				Size = new Vector2(20.0f, 20.0f),
				Id = (uint)(xIndex + yIndex)
			});
		}
		this.Countdown.AddTimeline(new TimelineBuilder()
			.BeginFrameSet(11, countdownFrames)
			.AddFrame(11, partId: 0)
			.AddFrame(countdownFrames, partId: 89)
			.EndFrameSet()
			.Build()
		);

		this.AddTimeline(new TimelineBuilder()
			.BeginFrameSet(11, countdownFrames)
			.AddLabel(11, 101, AtkTimelineJumpBehavior.Start, 0)
			.AddLabel(countdownFrames, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
			.EndFrameSet()
			.Build()
		);
	}
}
