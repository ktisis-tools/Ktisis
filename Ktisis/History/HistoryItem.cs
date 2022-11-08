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
        public TransformTableCopy Ttc { get; private set; }
        public Bone? Bone { get; private set; }
        public bool Global { get; private set; } = false;

        public HistoryItem(TransformTableCopy ttc, Bone? bone)
        {
            this.Ttc = ttc;
            this.Bone = bone;
            this.Global = (Bone == null);
        }
    }
}
