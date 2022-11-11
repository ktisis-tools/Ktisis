using FFXIVClientStructs.Havok;

using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

using System.Numerics;

using static FFXIVClientStructs.Havok.hkaPose;

namespace Ktisis.Events
{
    public static class EventManager
    {
        public unsafe delegate void TransformationMatrixChange(Matrix4x4 transformationMatrix, Bone? bone, Actor* actor);
        public static TransformationMatrixChange? OnTransformationMatrixChange = null;

        public delegate void GizmoChange(GizmoState state);
        public static GizmoChange? OnGizmoChange = null;

        public static unsafe void FireOnTransformationMatrixChangeEvent()
        {
            var bone = Skeleton.GetSelectedBone(EditActor.Target->Model->Skeleton);
            var actor = (Actor*)Ktisis.GPoseTarget!.Address;
            hkQsTransformf* boneTransform = 
                bone is null ? &actor->Model->Transform : boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);
            OnTransformationMatrixChange!(
                Interop.Alloc.GetMatrix(boneTransform), 
                Skeleton.GetSelectedBone(EditActor.Target->Model->Skeleton), actor
            );
        }
    }
}
