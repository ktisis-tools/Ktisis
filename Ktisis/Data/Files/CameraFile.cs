using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Ktisis.Structs.Camera;
using Ktisis.Structs.Common;

namespace Ktisis.Data.Files;

public class CameraFile : JsonFile {
	public new string FileExtension { get; set; } = ".ktcamera";
	public new string TypeName { get; set; } = "Ktisis Camera";
	public const int CurrentVersion = 1;
	public new int FileVersion { get; set; } = CurrentVersion;

	public string? Nickname { get; set; } = null;
    public bool IsNoCollide { get; set; } = false;
    public bool IsOrthographic { get; set; } = false;
    public bool IsDelimited { get; set; } = false;
    public Vector3? FixedPosition { get; set; }
    public Vector3 RelativeOffset { get; set; } = Vector3.Zero;
    public Vector2 Angle { get; set; } = Vector2.Zero;
    public Vector2 Pan { get; set; } = Vector2.Zero;
    public float Rotation { get; set; } = 0.0f;
    public float Zoom { get; set; } = 0.0f;
    public float Distance { get; set; } = 0.0f;
    public float DistanceMin { get; set; } = 0.0f;
    public float DistanceMax { get; set; } = 0.0f;
    public float OrthographicZoom { get; set; } = 0.0f;
}
