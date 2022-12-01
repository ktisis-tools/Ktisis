using System.Numerics;

using Ktisis.Structs.Poses;

namespace Ktisis.Data.Files {
    public class PoseFile : JsonFile {
		public string FileExtension { get; set; } = ".pose";
		public string TypeName { get; set; } = "Ktisis Pose";

		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; }
		public Vector3 Scale { get; set; }

		public PoseContainer? Bones { get; set; }
	}
}