using System.Numerics;

using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

using static FFXIVClientStructs.Havok.hkaPose;

namespace Ktisis.History {
	public class ActorBone : HistoryItem {
		private Bone? Bone { get; set; } // This should never have been introduced here.

		public unsafe Bone? GetBone() {
			// Shit workaround for the shit Bone implementation.

			if (Bone == null) return null;

			if (Actor == null) return null;

			var model = Actor->Model;
			if (model == null) return null;

			var skele = model->Skeleton;
			if (skele == null) return null;

			return model->Skeleton->GetBone(Bone.Partial, Bone.Index);
		}

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

		public override HistoryItem Clone() {
			var b = new ActorBone(Bone, ParentingState, SiblingLinkType);
			b.StartMatrix = StartMatrix;
			b.EndMatrix = EndMatrix;
			return b;
		}

		public unsafe override void Update(bool undo) {
			var historyToUndo = this;
			var transformToRollbackTo = undo ? historyToUndo.StartMatrix : historyToUndo.EndMatrix;
			var historyBone = historyToUndo.GetBone();
			if (historyBone == null) return;
			var model = historyToUndo.Actor->Model;

			if (model is null) return;

			var bone = model->Skeleton->GetBone(historyBone.Partial, historyBone.Index);
			var boneName = bone.HkaBone.Name.String ?? "";
			var boneTransform = bone.AccessModelSpace(PropagateOrNot.DontPropagate);

			// Write our updated matrix to memory.
			var initialRot = boneTransform->Rotation.ToQuat();
			var initialPos = boneTransform->Translation.ToVector3();
			Interop.Alloc.SetMatrix(boneTransform, transformToRollbackTo);

			if (ParentingState)
				bone.PropagateChildren(boneTransform, initialPos, initialRot);

			if (boneName.EndsWith("_l") || boneName.EndsWith("_r")) {
				var siblingBone = bone.GetMirrorSibling();
				siblingBone?.PropagateSibling(boneTransform->Rotation.ToQuat() / initialRot, SiblingLinkType);
			}
		}

		public unsafe bool SetMatrix(bool start = true) {
			var bone = GetBone();
			if (bone == null) return false;

			var boneTransform = bone.AccessModelSpace(PropagateOrNot.DontPropagate);
			var matrix = Interop.Alloc.GetMatrix(boneTransform);

			if (start)
				StartMatrix = matrix;
			else
				EndMatrix = matrix;

			return true;
		}
	}
}