using System;
using System.Numerics;

using Hexa.NET.ImGuizmo;

namespace Ktisis.Data.Config.Sections;

public class GizmoConfig {
	public bool Visible = true;
	
	public ImGuizmoMode Mode = ImGuizmoMode.Local;
	public ImGuizmoOperation Operation = ImGuizmoOperation.Rotate;

	public bool MirrorRotation = false;
	public bool ParentBones = true;
	public bool RelativeBones = true;

	public bool AllowAxisFlip = true;
	public bool AllowRaySnap = true;

	public GizmoStyle Style = DefaultStyle;

	public static readonly GizmoStyle DefaultStyle = new() {
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
	
	public struct GizmoStyle {
		public float TranslationLineThickness;
		public float TranslationLineArrowSize;
		public float RotationLineThickness;
		public float RotationOuterLineThickness;
		public float ScaleLineThickness;
		public float ScaleLineCircleSize;
		public float HatchedAxisLineThickness;
		public float CenterCircleSize;

		public Vector4 ColorDirectionX;
		public Vector4 ColorDirectionY;
		public Vector4 ColorDirectionZ;
		public Vector4 ColorPlaneX;
		public Vector4 ColorPlaneY;
		public Vector4 ColorPlaneZ;
		public Vector4 ColorSelection;
		public Vector4 ColorInactive;
		public Vector4 ColorTranslationLine;
		public Vector4 ColorScaleLine;
		public Vector4 ColorRotationUsingBorder;
		public Vector4 ColorRotationUsingFill;
		public Vector4 ColorHatchedAxisLines;
		public Vector4 ColorText;
		public Vector4 ColorTextShadow;

		public unsafe void ApplyStyle()
		{
			var style = ImGuizmo.GetStyle().Handle;
			style->TranslationLineThickness = this.TranslationLineThickness;
			style->TranslationLineArrowSize = this.TranslationLineArrowSize;
			style->RotationLineThickness = this.RotationLineThickness;
			style->RotationOuterLineThickness = this.RotationOuterLineThickness;
			style->ScaleLineThickness = this.ScaleLineThickness;
			style->ScaleLineCircleSize = this.ScaleLineCircleSize;
			style->HatchedAxisLineThickness = this.HatchedAxisLineThickness;
			style->CenterCircleSize = this.CenterCircleSize;
			
			style->Colors_0 = this.ColorDirectionX;
			style->Colors_1 = this.ColorDirectionY;
			style->Colors_2 = this.ColorDirectionZ;
			style->Colors_3 = this.ColorPlaneX;
			style->Colors_4 = this.ColorPlaneY;
			style->Colors_5 = this.ColorPlaneZ;
			style->Colors_6 = this.ColorSelection;
			style->Colors_7 = this.ColorInactive;
			style->Colors_8 = this.ColorTranslationLine;
			style->Colors_9 = this.ColorScaleLine;
			style->Colors_10 = this.ColorRotationUsingBorder;
			style->Colors_11 = this.ColorRotationUsingFill;
			style->Colors_12 = this.ColorHatchedAxisLines;
			style->Colors_13 = this.ColorText;
			style->Colors_14 = this.ColorTextShadow;
		}
	}

}
