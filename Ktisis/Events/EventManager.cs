using Ktisis.Interface.Components;
using Ktisis.Overlay;
using Ktisis.Structs.Bones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.Events
{
    public static class EventManager
    {
        public delegate void TransformationMatrixChange(TransformTable tt, Bone? bone);
        public static TransformationMatrixChange? OnTransformationMatrixChange = null;

        public delegate void GizmoChange(GizmoState state);
        public static GizmoChange? OnGizmoChange = null;
    }
}
