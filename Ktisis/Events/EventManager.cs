using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Overlay;
using Ktisis.Structs;
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
            if (bone is null)
            {
                bone = actor->Model->Skeleton->GetBone(0, 1);
            }
            var boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);
            EventManager.OnTransformationMatrixChange!(Interop.Alloc.GetMatrix(boneTransform), Skeleton.GetSelectedBone(EditActor.Target->Model->Skeleton), actor);
        }
    }
}
