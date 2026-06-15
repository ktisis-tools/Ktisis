using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

using Glamourer.Api.IpcSubscribers.Legacy;

using Ktisis.Common.Utility;
using Ktisis.Editor.Camera.Types;
using Ktisis.Interface.KTK;
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
	public List<OverlayInfo> Overlays { get; set; } = new List<OverlayInfo>();
	
	[Serializable]
	public struct ActorInfo {
		public ActorInfo(PoseFile pose, CharaFile chara, Transform location, string mcdf, float defaultRotation, ushort index, Guid penumbraCollection, Guid customizePlus, AttachInfo? attach = null) {
			Pose = pose;
			Chara = chara;
			Location = location;
			MCDF = mcdf;
			DefaultRotation = defaultRotation;
			Index = index;
			PenumbraCollection = penumbraCollection;
			CustomizePlus = customizePlus;
			Attach = attach;
		}
		public PoseFile Pose { get; set; }
		public CharaFile Chara { get; set; }
		public Transform Location { get; set; }
		public string MCDF { get; set; }
		public float DefaultRotation { get; set; }
		public ushort Index { get; set; }
		public Guid PenumbraCollection { get; set; } = Guid.Empty;
		public Guid CustomizePlus { get; set; } = Guid.Empty;
		public AttachInfo? Attach { get; set; }
	}
	
	[Serializable]
	public struct LightInfo {
		public LightInfo(LightFile light, Transform location, string name, bool state, AttachInfo? attach = null) {
			Light = light;
			Location = location;
			Name = name;
			Attach = attach;
			State = state;
		}
		public LightFile Light { get; set; }
		public Transform Location { get; set; }
		public string Name { get; set; }
		public bool State { get; set; } = true;
		
		public AttachInfo? Attach { get; set; }
	}

	[Serializable]
	public struct CameraInfo {
		public uint  Flags { get; set; }
		public ushort OrbitTarget { get; set; }
		public bool IsDelmited { get; set; }
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
	public struct OverlayInfo {
		public Type OverlayType { get; set; }
		
		//Status
		public StatusType StatusType { get; set; }
		public string StatusIcon { get; set; }
		
		//Balloon
		public BalloonBackground BalloonBackground { get; set; }
		public bool ShowArrow { get; set; }
		public float ArrowPosition { get; set; }
		
		//Talk
		public TalkBackground TalkBackground { get; set; }
		public TalkCursor TalkCursor { get; set; }
		public string Speaker { get; set; }

		
		public Vector2 Position { get; set; }
		public float Scale { get; set; }
		public float Opacity { get; set; }
		public string Dialog { get; set; }
		public bool Visible { get; set; }
		public string Name { get; set; }
		public int FontSize { get; set; }
		
		public enum Type {
			Balloon,
			Status,
			Talk,
			None
		}
	}
	
	[Serializable]
	public struct AttachInfo {
		public ushort ParentIndex { get; set; }
		public string NodeName { get; set; }
	}
}
