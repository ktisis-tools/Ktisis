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
        private TransformTable tt;
        private Bone bone;

        public HistoryItem(TransformTable tt, Bone bone)
        {
            this.tt = tt;
            this.bone = bone;
        }
    }
}
