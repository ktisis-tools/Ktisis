using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Game;

using Ktisis.Data.Config;
using Ktisis.Interface.KTK;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;
using Ktisis.Services.Game;

namespace Ktisis.Scene.Entities.Utility;

public class BalloonOverlay : OverlayEntity {
	protected override sealed BalloonNode Node { get; }
	public readonly uint[] FontSizes = [8, 9, 10, 11, 12, 14, 16, 18];

	public BalloonOverlay(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.BalloonOverlay;

		this.Node = new BalloonNode(
			BalloonBackground.Say,
			BalloonColor.Default,
			"New dialog...",
			true,
			130.0f,
			12
		) {
			Size = new Vector2(200.0f, 90.0f),
			Position = new Vector2(500.0f, 500.0f),
			EnableMoving = false,
			IsVisible = true
		};
		this.Scene.Overlay.AddNode(this.Node);
	}

	public string Dialog {
		get => this.Node.Dialog;
		set => this.Node.Dialog = value;
	}
	public BalloonBackground Background {
		get => this.Node.BgChoice;
		set => this.Node.BgChoice = value;
	}
	public BalloonColor Color {
		get => this.Node.ColorChoice;
		set => this.Node.ColorChoice = value;
	}
	public bool Arrow {
		get => this.Node.ArrowVisible;
		set => this.Node.ArrowVisible = value;
	}
	public float ArrowX {
		get => this.Node.ArrowX;
		set => this.Node.ArrowX = value;
	}
	public uint FontSize {
		get => this.Node.FontSize;
		set => this.Node.FontSize = value;
	}
}
