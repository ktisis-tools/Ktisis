using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Attachment;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Scene.Types;
using Ktisis.Structs.Characters;
using Attach = Ktisis.Structs.Attachment.Attach;

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

	public EntityPose? Pose { get; private set; }

	public override void Setup() {
		base.Setup();
		this.Pose = this._pose.Add(this);
	}

	public override void Update() {
		if (this.IsDrawing())
			base.Update();
	}

	public unsafe bool IsDrawing() {
		var ptr = this.GetCharacter();
		if (ptr == null) return false;
		return ((ulong)ptr->StateFlags & 0x00_00_00_FF_00) != 0;
	}
	
	// Character
	
	public unsafe CharacterBaseEx* CharacterBaseEx => (CharacterBaseEx*)this.GetCharacter();
	
	public unsafe virtual CharacterBase* GetCharacter() => (CharacterBase*)this.GetObject();
	
	// BoneAttach

	public unsafe Attach* GetAttach() {
		if (this.CharacterBaseEx == null) return null;
		var attach = &this.CharacterBaseEx->Attach;
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
		if (parentSkele == null || parentSkele->PartialSkeletons == null || parentSkele->PartialSkeletons->HavokPoses.IsEmpty)
			return null;
		
		var parentPose = parentSkele->PartialSkeletons[0].GetHavokPose(0);
		if (parentPose == null || parentPose->Skeleton == null) return null;

		if (!AttachUtility.TryGetParentBoneIndex(attach, out var parentId)) return null;
		
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
		if (attach != null) AttachUtility.Detach(attach);
	}
	
	// Transform

	public unsafe override void SetTransform(Transform trans) {
		var attach = this.GetAttach();
		if (attach != null && attach->IsActive()) {
			var source = this.GetTransform()!;
			AttachUtility.SetTransformRelative(attach, trans, source);
			if (source.Scale == trans.Scale) return;
			source.Scale = trans.Scale;
			base.SetTransform(source);
		} else {
			base.SetTransform(trans);
		}
	}
}
