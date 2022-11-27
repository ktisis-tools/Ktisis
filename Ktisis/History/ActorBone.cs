using Ktisis.Localization;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using static FFXIVClientStructs.Havok.hkaPose;

namespace Ktisis.History {
	public class ActorBone : HistoryItem {
		public Matrix4x4 TransformationMatrix { get; private set; }
		public Bone? Bone { get; private set; }
		public unsafe Actor* Actor { get; private set; }

		public unsafe ActorBone(Matrix4x4 transformationMatrix, Bone? bone) {
			this.TransformationMatrix = transformationMatrix;
			this.Bone = bone;
			this.Actor = (Actor*)Ktisis.GPoseTarget!.Address;
		}

		public override unsafe HistoryItem Clone() {
			return new ActorBone(TransformationMatrix, Bone);
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
			var boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);

			// Write our updated matrix to memory.
			var initialRot = boneTransform->Rotation.ToQuat();
			var initialPos = boneTransform->Translation.ToVector3();

			Interop.Alloc.SetMatrix(boneTransform, transformToRollbackTo);

			// Bone parenting
			// Adapted from Anamnesis Studio code shared by Yuki - thank you!

			var sourcePos = boneTransform->Translation.ToVector3();
			var deltaRot = boneTransform->Rotation.ToQuat() / initialRot;
			var deltaPos = sourcePos - initialPos;

			UpdateChildren(bone, sourcePos, deltaRot, deltaPos);
		}


		private static unsafe void UpdateChildren(Bone bone, Vector3 sourcePos, Quaternion deltaRot, Vector3 deltaPos) {
			Matrix4x4 matrix;
			var descendants = bone!.GetDescendants();
			foreach (var child in descendants) {
				var access = child.AccessModelSpace(PropagateOrNot.DontPropagate);

				var offset = access->Translation.ToVector3() - sourcePos;
				offset = Vector3.Transform(offset, deltaRot);

				matrix = Interop.Alloc.GetMatrix(access);
				matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
				matrix.Translation = deltaPos + sourcePos + offset;
				Interop.Alloc.SetMatrix(access, matrix);
			}
		}

		public override string DebugPrint() {
			var str = "";
			if (this.Bone is null) str += $"Bone Global";
			else str += $"Bone {Locale.GetBoneName(Bone.HkaBone.Name.String)}";

			return str;
		}

		public override bool IsElemInHistory() {
			if (HistoryManager.History == null) return false;
			List<HistoryItem> history = HistoryManager.History;
			List<ActorBone>? allBones = history.OfType<ActorBone>().ToList();
			var found = false;

			foreach(ActorBone elem in allBones) {
				if (elem?.Bone?.UniqueName == Bone?.UniqueName) {
					found = true;
					break;
				}
			}
			return found;
		}
	}
}
