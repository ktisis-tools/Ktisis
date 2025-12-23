using System.Numerics;

using Ktisis.Data.Config;
using Ktisis.Interface.KTK;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;
using Ktisis.Services.Game;

namespace Ktisis.Scene.Entities.Utility;

public class TalkOverlay : OverlayEntity {
	private override TalkNode Node;
	
	public TalkOverlay(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.TalkOverlay;

		this.Node = new TalkNode(
			TalkBackground.Basic,
			TalkCursor.Pin,
			"Cool Bunny",
			"I am One Cool Bunny..."
		) {
			Size = new Vector2(680.0f, 180.0f),
			Position = new Vector2(500.0f, 500.0f),
			EnableMoving = false,
			IsVisible = true
		};
		this.Scene.Overlay.AddNode(this.Node);
	}

	public string Speaker {
		get => this.Node.Speaker;
		set => this.Node.Speaker = value;
	}
	public string Dialog {
		get => this.Node.Dialog;
		set => this.Node.Dialog = value;
	}
	public TalkBackground Background {
		get => this.Node.BgChoice;
		set => this.Node.BgChoice = value;
	}
	public TalkCursor Cursor {
		get => this.Node.CursorChoice;
		set => this.Node.CursorChoice = value;
	}
}
