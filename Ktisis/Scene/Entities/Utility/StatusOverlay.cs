using System.Numerics;

using Ktisis.Interface.KTK;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Utility;

public class StatusOverlay : OverlayEntity {
	protected override sealed StatusNode Node { get; }

	public StatusOverlay(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.StatusOverlay;

		this.Node = new StatusNode(
			StatusType.Buff,
			"Bunnymoded",
			"ui/icon/213000/213001_hr1.tex"
		) {
			Size = new Vector2(247.0f, 32.0f),
			Position = new Vector2(500.0f, 500.0f),
			EnableMoving = false,
			IsVisible = true
		};
		this.Scene.Overlay.AddNode(this.Node);
	}

	public string StatusText {
		get => this.Node.Text;
		set => this.Node.Text = value;
	}
	public StatusType StatusType {
		get => this.Node.Type;
		set => this.Node.Type = value;
	}
	public string IconPath {
		get => this.Node.IconPath;
		set => this.Node.IconPath = value;
	}
}
