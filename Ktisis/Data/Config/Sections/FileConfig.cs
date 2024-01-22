using System.Collections.Generic;

using Ktisis.Editor.Characters;
using Ktisis.Editor.Posing;

namespace Ktisis.Data.Config.Sections;

public class FileConfig {
	public Dictionary<string, string> LastOpenedPaths = new();
	
	public SaveModes ImportCharaModes = SaveModes.All;
	
	public bool ImportPoseSelectedBones = false;
	public PoseTransforms ImportPoseTransforms = PoseTransforms.Rotation;
}
