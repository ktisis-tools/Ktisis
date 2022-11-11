using Ktisis.Interface.Components;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.History
{
    public class HistoryItem
    {
        public Matrix4x4 TransformationMatrix { get; private set; }
        public Bone? Bone { get; private set; }
        public unsafe Actor* Actor { get; private set; }

        public unsafe HistoryItem(Matrix4x4 transformationMatrix, Bone? bone, Actor* Actor)
        {
            this.TransformationMatrix = transformationMatrix;
            this.Bone = bone;
            this.Actor = Actor;
        }
    }
}
