using System;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Modules.Lights;
using Ktisis.Scene.Types;
using Ktisis.Structs.Lights;

using Attach = Ktisis.Structs.Attachment.Attach;

namespace Ktisis.Scene.Entities.World;

[Flags]
public enum LightEntityFlags {
	None = 0,
	Update = 1
}

public class LightEntity : WorldEntity, IDeletable, IHideable, IAttachable {
	public LightEntityFlags Flags { get; set; } = LightEntityFlags.None;

	public unsafe bool IsHidden {
		get {
			var ptr = this.GetObject();
			return ptr != null && !ptr->DrawObject.IsVisible;
		}
		set {
			var ptr = this.GetObject();
			if (ptr != null)
				ptr->DrawObject.IsVisible = !ptr->DrawObject.IsVisible;
		}
	}

	public unsafe new SceneLight* GetObject() => (SceneLight*)base.GetObject();

	// IAttachable
	private IAttachTarget? _attachTarget;
	public void SetAttach(IAttachTarget attachTarget) => this._attachTarget = attachTarget;
	public bool IsAttached() => this._attachTarget != null;
	public unsafe Attach* GetAttach() => null;
	public PartialBoneInfo? GetParentBone() {
		if (this._attachTarget is BoneNode bone)
			return bone.Info;
		if (this._attachTarget is BoneNodeGroup group)
			return group.GetIndividualBones().Where(b => b.Info.PartialIndex == 0).MinBy(b => b.Info.BoneIndex)?.Info;

		return null;
	}
	public void Detach() => this._attachTarget = null;
	public unsafe CharacterBase* GetCharacter() => null;
	
	public LightEntity(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.Light;
	}
	
	private LightModule GetModule() => this.Scene.GetModule<LightModule>();

	public unsafe void SetType(LightType type) {
		var ptr = this.GetObject();
		if (ptr == null || ptr->RenderLight == null) return;
		ptr->RenderLight->LightType = type;
	}

	public override void Update() {
		if (!this.IsValid) return;

		if (this.IsAttached() && this._attachTarget is ITransform transform && transform.GetTransform() is { } trans)
			base.SetTransform(trans);
		
		if (this.Flags.HasFlag(LightEntityFlags.Update))
			this.GetModule().UpdateLightObject(this);
		
		base.Update();
	}

	public override void SetTransform(Transform trans) {
		base.SetTransform(trans);
		this.Flags |= LightEntityFlags.Update;
	}

	public void ToggleHidden() => this.IsHidden = !this.IsHidden;

	public bool Delete() {
		this.GetModule().Delete(this);
		return this.Address == nint.Zero;
	}
}
