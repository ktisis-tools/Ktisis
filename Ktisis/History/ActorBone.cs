using Ktisis.Localization;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

using System;
using System.Numerics;

using static FFXIVClientStructs.Havok.hkaPose;
using static Ktisis.Overlay.Skeleton;

namespace Ktisis.History {
	public class ActorBone : HistoryItem {
		public Matrix4x4 TransformationMatrix { get; private set; }
		public Bone? Bone { get; private set; }
		public unsafe Actor* Actor { get; private set; }
		public bool ParentingState { get; private set; }
		public SiblingLink SiblingLinkType { get; private set; }

		public unsafe ActorBone(Matrix4x4 transformationMatrix, Bone? bone, bool parentingState, SiblingLink siblingLinkType) {
			this.TransformationMatrix = transformationMatrix;
			this.Bone = bone;
			this.Actor = (Actor*)Ktisis.GPoseTarget!.Address;
			this.ParentingState = parentingState;
			this.SiblingLinkType = siblingLinkType;
		}

		public override unsafe HistoryItem Clone() {
			return new ActorBone(TransformationMatrix, Bone, ParentingState, SiblingLinkType);
		}

		public unsafe override void Update() {
			var historyToUndo = this;
			var transformToRollbackTo = historyToUndo.TransformationMatrix;
			var historyBone = historyToUndo.Bone!;
			var isGlobalRotation = historyBone is null;
			var model = historyToUndo.Actor->Model;

			if (model is null) return;
			if (isGlobalRotation) { //There is no bone if you have a global rotation.
				Interop.Alloc.SetMatrix(&model->Transform, transformToRollbackTo);
				return;
			}

			var bone = model->Skeleton->GetBone(historyBone!.Partial, historyBone!.Index);
			var boneName = bone.HkaBone.Name.String;
			var boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);

			// Write our updated matrix to memory.
			var initialRot = boneTransform->Rotation.ToQuat();
			var initialPos = boneTransform->Translation.ToVector3();
			Interop.Alloc.SetMatrix(boneTransform, transformToRollbackTo);

			if (ParentingState)
				bone!.PropagateChildren(boneTransform, initialPos, initialRot);

			if (boneName.EndsWith("_l") || boneName.EndsWith("_r")) {
				var siblingBone = bone!.GetMirrorSibling();
				if (siblingBone != null)
					siblingBone.PropagateSibling(boneTransform->Rotation.ToQuat() / initialRot, SiblingLinkType);
			}

		}

		public unsafe override string DebugPrint() {
			var str = "";

			if (Bone == null)
				str += $"Bone Global";
			else if ((IntPtr)Bone.Pose == IntPtr.Zero || Bone.Pose->Skeleton == null)
				str += "<Invalid>";
			else
				str += $"Bone {Locale.GetBoneName(Bone.HkaBone.Name.String)}";

			str += $"ParentingState: {ParentingState} - SiblingLink: {SiblingLinkType}";

			return str;
		}
	}
}
