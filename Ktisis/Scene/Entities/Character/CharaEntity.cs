using System;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Partials;
using Ktisis.Editor.Transforms;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Structs.Characters;

namespace Ktisis.Scene.Entities.Character;

public class CharaEntity : WorldEntity, IAttachable {
	private readonly IPoseBuilder _pose;

	public CharaEntity(
		ISceneManager scene,
		IPoseBuilder pose
	) : base(scene) {
		this._pose = pose;
	}
	
	// Setup & update handling

	public override void Setup() {
		base.Setup();
		this._pose.Add(this);
	}

	public override void Update() {
		if (this.IsDrawing())
			base.Update();
	}

	public unsafe bool IsDrawing() {
		var ptr = this.GetCharacter();
		if (ptr == null) return false;
		return (ptr->UnkFlags_01 & 2) != 0 && ptr->UnkFlags_02 != 0;
	}
	
	// Character
	
	public unsafe virtual CharacterBase* GetCharacter() => (CharacterBase*)this.GetObject();
	
	public unsafe Customize? GetCustomize() {
		var ptr = this.GetCharacter();
		if (ptr == null) return null;
		return CharacterEx.From(ptr)->Customize;
	}

	public unsafe EquipmentModelId[]? GetEquipment() {
		var ptr = CharacterEx.From(this.GetCharacter());
		if (ptr == null) return null;
		return new Span<EquipmentModelId>(ptr->HumanEquip, 10).ToArray();
	}
	
	// BoneAttach

	public unsafe Attach* GetAttach() {
		var chara = CharacterEx.From(this.GetCharacter());
		if (chara == null) return null;
		
		var attach = &chara->Attach;
		return attach->Param != null ? attach : null;
	}

	public unsafe virtual bool IsAttached() {
		var attach = this.GetAttach();
		return attach != null && attach->IsActive();
	}

	public unsafe PartialBoneInfo? GetParentBone() {
		var attach = this.GetAttach();
		if (attach == null) return null;

		var parentSkele = attach->GetParentSkeleton();
		if (parentSkele == null || parentSkele->PartialSkeletons == null || parentSkele->PartialSkeletons->HavokPoses == null)
			return null;
		
		var parentPose = parentSkele->PartialSkeletons[0].GetHavokPose(0);
		if (parentPose == null || parentPose->Skeleton == null) return null;

		if (!AttachUtil.TryGetParentBoneIndex(attach, out var parentId)) return null;
		
		var skeleton = parentPose->Skeleton;
		return new PartialBoneInfo {
			Name = skeleton->Bones[parentId].Name.String ?? string.Empty,
			BoneIndex = parentId,
			ParentIndex = skeleton->ParentIndices[parentId],
			PartialIndex = 0
		};
	}

	public unsafe virtual void Detach() {
		var attach = this.GetAttach();
		if (attach != null) AttachUtil.Detach(attach);
	}
	
	// Transform

	public unsafe override void SetTransform(Transform trans) {
		var attach = this.GetAttach();
		if (attach != null && attach->IsActive()) {
			var source = this.GetTransform()!;
			AttachUtil.SetTransformRelative(attach, trans, source);
			if (source.Scale == trans.Scale) return;
			source.Scale = trans.Scale;
			base.SetTransform(source);
		} else {
			base.SetTransform(trans);
		}
	}
}
