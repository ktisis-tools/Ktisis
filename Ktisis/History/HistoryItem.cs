using Ktisis.Interface.Components;
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
        public bool Global { get; private set; } = false;

        public HistoryItem(TransformTable tt, Bone? bone)
        {
            this.Tt = tt;
            this.Bone = bone;
            this.Global = (Bone == null);
        }
    }
}
