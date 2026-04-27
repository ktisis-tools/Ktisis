using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

using Ktisis.Common.Utility;
using Ktisis.Editor.Camera.Types;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;
using Ktisis.Structs.Env.Weather;

namespace Ktisis.Data.Files;
public class SceneFile : JsonFile {

	public new string FileExtension { get; set; } = ".ktscene";
	public new string TypeName { get; set; } = "Ktisis Scene";
	
	public const int CurrentVersion = 1;
	public new int FileVersion { get; set; } = CurrentVersion;
	public Vector3 SceneOrigin { get; set; }
	public uint MapID { get; set; }
	
	[JsonInclude]
	public List<ActorInfo> Actors { get; set; } = new List<ActorInfo>();
	[JsonInclude]
	public List<LightInfo> Lights { get; set; } = new List<LightInfo>();
	public List<CameraInfo> Cameras  { get; set; } = new List<CameraInfo>();
	public EnvironmentInfo Environment { get; set; } = new EnvironmentInfo();
	
	[Serializable]
	public struct ActorInfo {
		public PoseFile Pose { get; set; }
		public CharaFile Chara { get; set; }
		public Transform Location { get; set; }
		public String MCDF { get; set; }
		public float DefaultRotation { get; set; }
		public ushort Index { get; set; }
		
		public AttachInfo? Attach { get; set; }
	}
	
	[Serializable]
	public struct LightInfo {
		public LightFile Light { get; set; }
		public Transform Location { get; set; }
		public String Name { get; set; }
		
		public AttachInfo? Attach { get; set; }
	}

	[Serializable]
	public struct CameraInfo {
		public uint  Flags { get; set; }
		public ushort OrbitTarget { get; set; }
		public bool isDelmited { get; set; }
		public Vector3? FixedPosition { get; set; }
		public Vector3? Angle { get; set; }
		
		public float OrthographicZoom { get; set; }
		public string Name { get; set; }
		public bool IsActive { get; set; }
		
		public AttachInfo? Attach { get; set; }
	}


	public struct EnvironmentInfo {
		public uint Override { get; set; }
		public EnvState State { get; set; }
		public float Time { get; set; }
		public int Day { get; set; }
		public byte Weather { get; set; }
	}

	[Serializable]
	public struct AttachInfo {
		public ushort ParentIndex { get; set; }
		public string NodeName { get; set; }
	}
}
