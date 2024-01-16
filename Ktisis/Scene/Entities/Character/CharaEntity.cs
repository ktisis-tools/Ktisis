using System;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Posing.Partials;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Structs.Characters;

namespace Ktisis.Scene.Entities.Character;

public abstract class CharaEntity : WorldEntity, ICharacter, IAttachable {
	private readonly IPoseBuilder _pose;

	protected CharaEntity(
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
		if (attach->ParentSkeleton == null || attach->ChildSkeleton == null || attach->Param == null)
			return null;
		return attach;
	}

	public unsafe virtual bool IsAttached() {
		var attach = this.GetAttach();
		return attach != null && attach->IsActive();
	}

	public unsafe PartialBoneInfo? GetParentBone() {
		var attach = this.GetAttach();
		if (attach == null) return null;

		var parentPose = attach->ParentSkeleton->PartialSkeletons[0].GetHavokPose(0);
		if (parentPose == null || parentPose->Skeleton == null) return null;

		var index = attach->Param->ParentBoneId;
		var skeleton = parentPose->Skeleton;
		return new PartialBoneInfo {
			Name = skeleton->Bones[index].Name.String ?? string.Empty,
			BoneIndex = index,
			ParentIndex = skeleton->ParentIndices[index],
			PartialIndex = 0
		};
	}

	public virtual void Detach() {
		; // TODO
	}
	
	// Transform

	public unsafe override void SetTransform(Transform trans) {
		var attach = this.GetAttach();
		if (attach != null && attach->IsActive())
			this.SetAttachTransform(attach, trans);
		else
			base.SetTransform(trans);
	}

	private unsafe void SetAttachTransform(Attach* attach, Transform target) {
		var partials = attach->ParentSkeleton->PartialSkeletons;
		if (partials == null || partials->HavokPoses == null) return;

		var parentPose = partials[0].GetHavokPose(0);
		if (parentPose == null) return;
		
		var parentModel = HavokPoseUtil.GetWorldTransform(
			attach->ParentSkeleton,
			parentPose,
			attach->Param->ParentBoneId
		)!;

		var source = this.GetTransform()!;
		var deltaRot = Quaternion.Inverse(parentModel.Rotation);
		var transform = new Transform(attach->Param->Transform);
		transform.Position += Vector3.Transform(target.Position - source.Position, deltaRot);
		transform.Rotation = deltaRot * target.Rotation;
		transform.Scale += Vector3.Transform(target.Scale - source.Scale, deltaRot);
		attach->Param->Transform = transform;
	}
}
