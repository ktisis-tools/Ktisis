using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;
using Ktisis.Structs.Objects;

using DrawObject = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.DrawObject;

namespace Ktisis.Scene.Entities.World;

public class ObjectEntity : WorldEntity, IHideable {
	public WorldObject Object;

	public ObjectEntity(
		ISceneManager scene,
		WorldObject obj
	) : base(scene) {
		this.Type = EntityType.Model;
		this.Object = obj;
		this.Visible = true;
	}

	public override void SetTransform(Transform trans) {
		base.SetTransform(trans);
		this.Object.Update();
	}

	public unsafe bool IsHidden {
		get {
			var drawPtr = (DrawObject*)this.Address;
			return drawPtr != null && !drawPtr->IsVisible;
		}
		set {
			var drawPtr = (DrawObject*)this.Address;
			if (drawPtr != null)
				drawPtr->IsVisible = !drawPtr->IsVisible;
		}
	}
	public void ToggleHidden() => this.IsHidden = !this.IsHidden;

	public unsafe void Reset() {
		this.SetTransform(this.Object.InitialTransform);

		if (this.Object.ObjectType != ObjectType.BgObject || this.Object.InitialFlags == null) return;

		var drawPtr = (DrawObject*)this.Address;
		drawPtr->Flags = this.Object.InitialFlags.Value;
	}

	public override void Remove() {
		try {
			this.Reset();
		} finally {
			base.Remove();
		}
	}
}
