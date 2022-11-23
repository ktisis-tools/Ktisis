using FFXIVClientStructs.Havok;

using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
﻿using Ktisis.Structs.Actor.State;
using Ktisis.Structs.Bones;

using System.Numerics;

using static FFXIVClientStructs.Havok.hkaPose;

namespace Ktisis.Events {
	public static class EventManager {
		public delegate void GPoseChange(ActorGposeState state);
		public static GPoseChange? OnGPoseChange = null;

		public static void FireOnGposeChangeEvent(ActorGposeState state) {
			OnGPoseChange?.Invoke(state);
		}

		public unsafe delegate void TransformationMatrixChange(TransformTableState state, Matrix4x4 transformationMatrix, Bone? bone, Actor* actor);
		public static TransformationMatrixChange? OnTransformationMatrixChange = null;

		public delegate void GizmoChange(GizmoState state);
		public static GizmoChange? OnGizmoChange = null;

		public static unsafe void FireOnTransformationMatrixChangeEvent(TransformTableState state) {
			if (OnTransformationMatrixChange == null) return;
			var bone = Skeleton.GetSelectedBone();
			var actor = (Actor*)Ktisis.GPoseTarget!.Address;
			hkQsTransformf* boneTransform =
				bone is null ? &actor->Model->Transform : bone!.AccessModelSpace(PropagateOrNot.DontPropagate);
			OnTransformationMatrixChange(
				state,
				Interop.Alloc.GetMatrix(boneTransform),
				Skeleton.GetSelectedBone(),
				actor
			);
		}

		public static unsafe void FireOnGizmoChangeEvent(GizmoState state) {
			OnGizmoChange?.Invoke(state);
		}
	}
}
