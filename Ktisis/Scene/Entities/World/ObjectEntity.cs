using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using KamiToolKit.Extensions;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;
using Ktisis.Structs.Objects;

using DrawObject = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.DrawObject;
using Transform = Ktisis.Common.Utility.Transform;

namespace Ktisis.Scene.Entities.World;

public class ObjectEntity : WorldEntity, IHideable {
	public WorldObject? Object;
	public bool NeedsCulling;
	private ByteColor? OriginalStain;
	private ByteColor? OriginalColor;
	private byte? OriginalIntensity;

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

	public unsafe Vector4 GetStain() {
		if (this.CanStain()) {
			if (this.OriginalStain == null) {
				this.OriginalStain = ((BgObject*)this.Address)->StainBuffer->SrgbByteColor;
			}
			return ((BgObject*)this.Address)->StainBuffer->LinearFloatColor;
		}
		return Vector4.Zero;
	}
	
	public unsafe bool TrySetStainColor(ByteColor srgb) => ((BgObject*)this.Address)->TrySetStainColor(srgb);
	public unsafe bool CanStain() => ((BgObject*)this.Address)->StainBuffer != null;
	public override void SetTransform(Transform trans) {
		base.SetTransform(trans);
		this.Object?.Update();
		this.NeedsCulling = true;
	}
	public unsafe Vector4 Color {
		get {
			if (this.OriginalColor == null) {
				this.OriginalColor = ((BgObject*)this.Address)->Color;
			}
			return ((BgObject*)this.Address)->Color.ToVector4();
		} 
		set => ((BgObject*)this.Address)->Color = ByteColorExtensions.ToByteColor(value);
	}

	public unsafe byte ColorIntensity {
		get {
			if (this.OriginalIntensity == null) {
				this.OriginalIntensity = ((BgObject*)this.Address)->ColorIntensity;
			}
			return ((BgObject*)this.Address)->ColorIntensity;
		}
		set => ((BgObject*)this.Address)->ColorIntensity = value;
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
		if (this.CanStain() && this.OriginalStain != null)
			this.TrySetStainColor(this.OriginalStain.Value);
		if (this.OriginalIntensity != null && this.OriginalIntensity != ((BgObject*)this.Address)->ColorIntensity)
			((BgObject*)this.Address)->ColorIntensity = this.OriginalIntensity.Value;
		if (this.OriginalColor != null)
			((BgObject*)this.Address)->Color = this.OriginalColor.Value;
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
