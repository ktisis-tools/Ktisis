using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.History
{
    public class TransformTableCopy
    {
        public Vector3 pos { get; set; }
        public Quaternion rot { get; set; }

        public Vector3 scale { get; set; }
        public TransformTableCopy(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            this.pos = pos;
            this.rot = rot;
            this.scale = scale;
        }

        public override string ToString()
        {
            return pos.ToString() + rot.ToString() + scale.ToString();
        }
    }
}
