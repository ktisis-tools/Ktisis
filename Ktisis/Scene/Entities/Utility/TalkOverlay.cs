using System.Numerics;

using Ktisis.Data.Config;
using Ktisis.Interface.KTK;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;
using Ktisis.Services.Game;

namespace Ktisis.Scene.Entities.Utility;

public class TalkOverlay : SceneEntity, IVisibility, IDeletable {
	private TalkNode Node;
	private bool _visible = true;
	
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

	// todo: make these generic
	public bool Visible {
		get => this._visible;
		set {
			this._visible = value;
			this.Node.IsVisible = value;
		}
	}
	public Vector2 Position {
		get => this.Node.Position;
		set => this.Node.Position = value;
	}
	public bool Draggable {
		get => this.Node.EnableMoving;
		set => this.Node.EnableMoving = value;
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

	public bool Delete() {
		this.Scene.Overlay.RemoveNode(this.Node);
		this.Node.Dispose();
		this.Remove();
		return true;
	}
}
