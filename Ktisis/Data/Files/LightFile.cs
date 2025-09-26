using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Ktisis.Structs.Lights;
using Ktisis.Structs.Common;

using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace Ktisis.Data.Files;

public class LightFile : JsonFile {
	public new string FileExtension { get; set; } = ".ktlight";
	public new string TypeName { get; set; } = "Ktisis Light";
	public const int CurrentVersion = 1;
	public new int FileVersion { get; set; } = CurrentVersion;

	public string? Nickname { get; set; } = null;
	public LightFlags Flags { get; set; }
	public LightType LightType { get; set; }
	public unsafe Transform? Transform { get; set; } = null;
	public ColorHDR Color { get; set; } = new ColorHDR();
	public float ShadowNear { get; set; } = 0.0f;
	public float ShadowFar { get; set; } = 0.0f;
	public FalloffType FalloffType { get; set; }
	public Vector2 AreaAngle { get; set; } = Vector2.Zero;
	public float Falloff { get; set; } = 0.0f;
	public float LightAngle { get; set; } = 0.0f;
	public float FalloffAngle { get; set; } = 0.0f;
	public float Range { get; set; } = 0.0f;
	public float CharaShadowRange { get; set; } = 0.0f;
}
