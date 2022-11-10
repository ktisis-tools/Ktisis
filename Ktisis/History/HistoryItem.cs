using Ktisis.Interface.Components;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.History
{
    public class HistoryItem
    {
        public TransformTable Tt { get; private set; }
        public Bone? Bone { get; private set; }
        public bool IsGlobalTransform { get; private set; } = false;
        public unsafe Actor* Actor { get; private set; }

        public unsafe HistoryItem(TransformTable tt, Bone? bone, Actor* Actor)
        {
            this.Tt = tt;
            this.Bone = bone;
            this.IsGlobalTransform = (Bone == null);
            this.Actor = Actor;
        }
    }
}
