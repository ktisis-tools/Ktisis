using Ktisis.Scene.Types;
using Ktisis.Structs.Objects;

namespace Ktisis.Scene.Entities.World;

public class ObjectEntity : WorldEntity {
	public readonly WorldObject Object;

	public ObjectEntity(
		ISceneManager scene,
		WorldObject obj
	) : base(scene) {
		this.Type = EntityType.Models;
		this.Object = obj;
		this.Visible = true;
	}

	public void Reset() => this.SetTransform(this.Object.InitialTransform);

	public override void Remove() {
		try {
			this.Reset();
		} finally {
			base.Remove();
		}
	}
}
