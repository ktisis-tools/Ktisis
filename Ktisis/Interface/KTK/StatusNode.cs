using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

using Dalamud.Interface;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Ktisis.Common.Utility;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using KamiToolKit.Overlay.UiOverlay;
using KamiToolKit.Premade.Node.Simple;

namespace Ktisis.Interface.KTK;

public enum StatusType {
	None = -1,
	Buff = 0,
	Debuff = 1,
	Falloff = 2,
}

public class StatusNode : OverlayNode {
	private SimpleImageNode StatusIcon;
	private TextNode StatusText;
	public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
	public override bool HideWithNativeUi => false;
	public override bool HideWithUiToggled => false;

	public StatusType Type;
	public string Text;
	public string IconPath;

	public StatusNode(
		StatusType type,
		string text,
		string iconPath
	) {
		this.Type = type;
		this.Text = text;
		this.IconPath = iconPath;

		this.StatusIcon = this.SetStatusIcon();
		this.StatusText = this.SetStatusText();
		
		this.StatusIcon.AttachNode(this);
		this.StatusText.AttachNode(this);
	}

	protected override void OnUpdate() {
		// update text
		this.StatusText.String = this.GetTextForType();
		this.StatusText.TextColor = GetTextColorForType(this.Type);
		this.StatusText.TextOutlineColor = GetEdgeColorForType(this.Type);
		// update icon
		this.StatusIcon.TexturePath = this.IconPath;
	}

	private SimpleImageNode SetStatusIcon() {
		return new SimpleImageNode() {
			TexturePath = this.IconPath,
			TextureSize = new Vector2(24.0f, 32.0f),
			TextureCoordinates = Vector2.Zero,
			Position = Vector2.Zero,
			Size = new Vector2(24.0f, 32.0f)
		};
	}

	private TextNode SetStatusText() {
		return new TextNode() {
			Size = new Vector2(660.0f, 28.0f),
			Position = new Vector2(27.0f, 2.0f),
			TextColor = GetTextColorForType(this.Type),
			TextOutlineColor = GetEdgeColorForType(this.Type),
			FontType = FontType.Axis,
			TextFlags = TextFlags.Edge | TextFlags.Ellipsis,
			AlignmentType = AlignmentType.Left,
			FontSize = 18,
			LineSpacing = 16,
			String = this.GetTextForType()
		};
	}

	private string GetTextForType() => this.Type switch {
		StatusType.Buff => "+ " + this.Text,
		StatusType.Debuff => "+ " + this.Text,
		StatusType.Falloff => "- " + this.Text,
		_ => this.Text
	};

	private static Vector4 GetTextColorForType(StatusType type) => type switch {
		StatusType.Falloff => GuiHelpers.VectorColorFromString("#CCCCCCFF"),
		_ => KnownColor.White.Vector()
	};
	private static Vector4 GetEdgeColorForType(StatusType type) => type switch {
		StatusType.Buff => GuiHelpers.VectorColorFromString("#2A5D00FF"),
		StatusType.Debuff => GuiHelpers.VectorColorFromString("#8A0000FF"),
		StatusType.Falloff => GuiHelpers.VectorColorFromString("#454545FF"),
		_ => KnownColor.Black.Vector()
	};
}
