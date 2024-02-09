using System.Numerics;

using Ktisis.ImGuizmo;

namespace Ktisis.Data.Config.Sections;

public class GizmoConfig {
	public bool Visible = true;
	
	public Mode Mode = Mode.Local;
	public Operation Operation = Operation.ROTATE;

	public bool MirrorRotation = false;
	public bool ParentBones = true;
	public bool RelativeBones = true;
	
	public bool AllowAxisFlip = true;

	public Style Style = DefaultStyle;

	public readonly static Style DefaultStyle = new() {
		TranslationLineThickness = 3.0f,
		TranslationLineArrowSize = 6.0f,
		RotationLineThickness = 2.0f,
		RotationOuterLineThickness = 3.0f,
		ScaleLineThickness = 3.0f,
		ScaleLineCircleSize = 6.0f,
		HatchedAxisLineThickness = 6.0f,
		CenterCircleSize = 6.0f,
		
		ColorDirectionX = new Vector4(0.666f, 0.000f, 0.000f, 1.000f),
		ColorDirectionY = new Vector4(0.000f, 0.666f, 0.000f, 1.000f),
		ColorDirectionZ = new Vector4(0.000f, 0.000f, 0.666f, 1.000f),
		ColorPlaneX = new Vector4(0.666f, 0.000f, 0.000f, 0.380f),
		ColorPlaneY = new Vector4(0.000f, 0.666f, 0.000f, 0.380f),
		ColorPlaneZ = new Vector4(0.000f, 0.000f, 0.666f, 0.380f),
		ColorSelection = new Vector4(1.000f, 0.500f, 0.062f, 0.541f),
		ColorInactive = new Vector4(0.600f, 0.600f, 0.600f, 0.600f),
		ColorTranslationLine = new Vector4(0.666f, 0.666f, 0.666f, 0.666f),
		ColorScaleLine = new Vector4(0.250f, 0.250f, 0.250f, 1.000f),
		ColorRotationUsingBorder = new Vector4(1.000f, 0.500f, 0.062f, 1.000f),
		ColorRotationUsingFill = new Vector4(1.000f, 0.500f, 0.062f, 0.500f),
		ColorHatchedAxisLines = new Vector4(0.000f, 0.000f, 0.000f, 0.500f),
		ColorText = new Vector4(1.000f, 1.000f, 1.000f, 1.000f),
		ColorTextShadow = new Vector4(0.000f, 0.000f, 0.000f, 1.000f)
	};
}
