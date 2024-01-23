using System.Numerics;

using Ktisis.Editor.Posing;
using Ktisis.Editor.Posing.Data;

namespace Ktisis.Data.Files;

public class PoseFile : JsonFile {
	public new string FileExtension { get; set; } = ".pose";
	public new string TypeName { get; set; } = "Ktisis Pose";

	public const int CurrentVersion = 2;
	
	public Vector3 Position { get; set; }
	public Quaternion Rotation { get; set; }

	public PoseContainer? Bones { get; set; }

	public PoseContainer? MainHand { get; set; }
	public PoseContainer? OffHand { get; set; }
	public PoseContainer? Prop { get; set; }
	
	// TODO LEGACY CONVERSION
}
