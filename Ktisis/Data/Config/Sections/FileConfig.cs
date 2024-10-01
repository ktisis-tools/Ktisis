using System.Collections.Generic;

using Ktisis.Editor.Characters;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Posing.Data;

namespace Ktisis.Data.Config.Sections;

public class FileConfig {
	public Dictionary<string, string> LastOpenedPaths = new();
	
	public SaveModes ImportCharaModes = SaveModes.All;
	public bool ImportNpcApplyOnSelect = false;
	
	public bool ImportPoseSelectedBones = false;
	public bool AnchorPoseSelectedBones = false;
	public PoseTransforms ImportPoseTransforms = PoseTransforms.Rotation;
	public PoseMode ImportPoseModes = PoseMode.All;
}
