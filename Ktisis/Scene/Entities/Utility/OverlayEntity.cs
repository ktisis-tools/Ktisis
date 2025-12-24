using System.Numerics;

using KamiToolKit.Overlay;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Utility;

public abstract class OverlayEntity : SceneEntity, IVisibility, IDeletable {
	protected abstract OverlayNode Node { get; }
	private bool _visible = true;

	public OverlayEntity(
		ISceneManager scene
	) : base(scene) {
		// this.Type = EntityType.TalkOverlay;
	}

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
	public float Scale {
		get => this.Node.ScaleX;
		set {
			this.Node.ScaleX = value;
			this.Node.ScaleY = value;
		}
	}
	public Vector2 Size {
		get => this.Node.Size;
		private set => this.Node.Size = value;
	}
	public bool Draggable {
		get => this.Node.EnableMoving;
		set => this.Node.EnableMoving = value;
	}

	public bool Delete() {
		this.Scene.Overlay.RemoveNode(this.Node);
		this.Node.Dispose();
		this.Remove();
		return true;
	}
}
