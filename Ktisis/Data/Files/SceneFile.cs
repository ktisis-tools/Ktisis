using System.Collections.Generic;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Memory.Regular;
using FFXIVClientStructs.Havok.Animation.Rig;

using Ktisis.Editor.Camera.Types;

namespace Ktisis.Data.Files;

public class SceneFile : JsonFile {
	public new string FileExtension { get; set; } = ".ktscene";
	public new string TypeName { get; set; } = "Ktisis Scene";
	
	public const int CurrentVersion = 1;
	public new int FileVersion { get; set; } = CurrentVersion;

	public List<ActorInfo> Actors = new List<ActorInfo>();
	public List<LightFile> Lights = new List<LightFile>();
	public List<EditorCamera> EditorCameras = new List<EditorCamera>();
	
	
	public struct ActorInfo {
		internal string Pose;
		internal string Chara;
		internal string MCDF;
		internal LocationInfo WorldRelative;
		internal LocationInfo OriginRelative;
	}
	
	public struct LocationInfo {
		internal Vector3 Position;
		internal float Rotation;
	}

}
