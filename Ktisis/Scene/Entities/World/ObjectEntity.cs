using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;
using Ktisis.Structs.Objects;

using DrawObject = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.DrawObject;

namespace Ktisis.Scene.Entities.World;

public class ObjectEntity : WorldEntity, IHideable {
	public WorldObject? Object;
	public bool NeedsCulling;

	public ObjectEntity(
		ISceneManager scene,
		WorldObject obj
	) : base(scene) {
		this.Type = EntityType.Model;
		this.Object = obj;
		this.Visible = true;
	}

	public ObjectEntity(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.Model;
		this.Visible = true;
	}

	public unsafe override void Update() {
		base.Update();
		if (this.Object != null && !this.NeedsCulling) return;

		// for spawned objects, check loadstate and resourcehandle, then update culling+render
		var obj = (BgObject*)this.Address;
		if (obj is null || obj->ModelResourceHandle is null) return;
		if (obj->ModelResourceHandle->LoadState == 7) {
			obj->UpdateCulling();
			obj->UpdateRender();
			obj->UpdateMaterials();
			obj->UpdateTransforms(false);
			this.NeedsCulling = false;
		}
	}

	public override void SetTransform(Transform trans) {
		base.SetTransform(trans);
		this.Object?.Update();
		this.NeedsCulling = true;
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

	public unsafe string GetPath() {
		if (this.Object is not null)
			return this.Object.Value.Path;

		var obj = (BgObject*)this.Address;
		if (obj is null || obj->ModelResourceHandle is null) return "N/A";
		return obj->ModelResourceHandle->FileName.ToString();
	}

	public bool IsWorldObject() => this.Object is not null;

	public void ToggleHidden() => this.IsHidden = !this.IsHidden;

	public unsafe void Reset() {
		if (this.Object is null) return;
		this.SetTransform(this.Object.Value.InitialTransform);

		if (this.Object.Value.ObjectType != ObjectType.BgObject || this.Object.Value.InitialFlags == null) return;

		var drawPtr = (DrawObject*)this.Address;
		drawPtr->Flags = this.Object.Value.InitialFlags.Value;
	}

	public unsafe bool Despawn() {
		if (this.Object is not null) return false;

		var obj = (BgObject*)this.Address;
		obj->CleanupRender();
		obj->Dtor(1);

		return true;
	}

	public override void Remove() {
		try {
			this.Reset();
			this.Despawn();
		} finally {
			base.Remove();
		}
	}
}
