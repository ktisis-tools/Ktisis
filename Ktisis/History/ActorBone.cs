﻿using System.Numerics;

using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

using static FFXIVClientStructs.Havok.hkaPose;

namespace Ktisis.History {
	public class ActorBone : HistoryItem {
		public Bone? Bone { get; private set; }
		public unsafe Actor* Actor { get; private set; }
		public bool ParentingState { get; private set; }
		public SiblingLink SiblingLinkType { get; private set; }

		public Matrix4x4 StartMatrix { get; set; }
		public Matrix4x4 EndMatrix { get; set; }

		public unsafe ActorBone(Bone? bone, bool parentingState, SiblingLink siblingLinkType) {
			Bone = bone;
			Actor = (Actor*)Ktisis.GPoseTarget!.Address;
			ParentingState = parentingState;
			SiblingLinkType = siblingLinkType;
		}

		public override unsafe HistoryItem Clone() {
			var b = new ActorBone(Bone, ParentingState, SiblingLinkType);
			b.StartMatrix = StartMatrix;
			b.EndMatrix = EndMatrix;
			return b;
		}

		public unsafe override void Update(bool undo) {
			var historyToUndo = this;
			var transformToRollbackTo = undo ? historyToUndo.StartMatrix : historyToUndo.EndMatrix;
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

		public unsafe bool SetMatrix(bool start = true) {
			if (Bone == null) return false;
			var boneTransform = Bone.AccessModelSpace(PropagateOrNot.DontPropagate);
			var matrix = Interop.Alloc.GetMatrix(boneTransform);

			if (start)
				StartMatrix = matrix;
			else
				EndMatrix = matrix;

			return true;
		}
	}
}