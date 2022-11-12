using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

using System.Numerics;

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

        public unsafe HistoryItem Clone()
        {
            return new HistoryItem(TransformationMatrix, Bone, Actor);
        }
    }
}
