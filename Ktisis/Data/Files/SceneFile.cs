using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

using Ktisis.Common.Utility;

namespace Ktisis.Data.Files;
public class SceneFile : JsonFile {

	public new string FileExtension { get; set; } = ".ktscene";
	public new string TypeName { get; set; } = "Ktisis Scene";
	
	public const int CurrentVersion = 1;
	public new int FileVersion { get; set; } = CurrentVersion;
	public Vector3 SceneOrigin { get; set; }
	
	[JsonInclude]
	public List<ActorInfo> Actors { get; set; } = new List<ActorInfo>();
	[JsonInclude]
	public List<LightInfo> Lights { get; set; } = new List<LightInfo>();
	//public List<EditorCamera> EditorCameras = new List<EditorCamera>();
	
	[Serializable]
	public struct ActorInfo {
		public PoseFile Pose { get; set; }
		public CharaFile Chara { get; set; }
		public Transform Location { get; set; }
		public String MCDF { get; set; }
	}
	
	[Serializable]
	public struct LightInfo {
		public LightFile Light { get; set; }
		public Transform Location { get; set; }
		public String Name { get; set; }
	}
	
}
