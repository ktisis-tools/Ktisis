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
	public bool SelectedBonesIncludeDescendants = false;
	public bool AnchorPoseSelectedBones = false;

	// patch fix pending better blanket exclusion/selection options
	public bool ExcludePoseEarBones = false;
	public PoseTransforms ImportPoseTransforms = PoseTransforms.Rotation;
	public PoseMode ImportPoseModes = PoseMode.All;
	public List<(string Path, string Name)> CustomLocations { get; set; } = new();
}
