using Dalamud.Interface;

using FFXIVClientStructs.Havok;

using ImGuiNET;

using Ktisis.Events;
using Ktisis.Helpers;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Bones;
using Ktisis.Util;

using System.Linq;
using System.Numerics;

namespace Ktisis.Interface.Components
{
	// Thanks to Emyka for the original code:
	// https://github.com/ktisis-tools/Ktisis/pull/5

	public class TransformTable {
		public bool IsEditing = false;

		private float BaseSpeedPos;
		private float BaseSpeedRot;
		private float BaseSpeedSca;
		private float ModifierMultCtrl;
		private float ModifierMultShift;
		private string DigitPrecision = "%.3f";

		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Scale;

		private TransformTableState _state = TransformTableState.IDLE;
		public void FetchConfigurations() {
			BaseSpeedPos = Ktisis.Configuration.TransformTableBaseSpeedPos;
			BaseSpeedRot = Ktisis.Configuration.TransformTableBaseSpeedRot;
			BaseSpeedSca = Ktisis.Configuration.TransformTableBaseSpeedSca;
			ModifierMultCtrl = Ktisis.Configuration.TransformTableModifierMultCtrl;
			ModifierMultShift = Ktisis.Configuration.TransformTableModifierMultShift;
			DigitPrecision = $"%.{Ktisis.Configuration.TransformTableDigitPrecision}f";
		}

		public TransformTable Clone()
		{
			TransformTable tt = new();
			tt.Position = Position;
			tt.Rotation = Rotation;
			tt.Scale = Scale;
			return tt;
		}


		// Set stored values.

		public void Update(Vector3 pos, Quaternion rot, Vector3 scale) {
			Position = pos;
			Rotation = MathHelpers.ToEuler(rot);
			Scale = scale;
		}

		// Draw table.

		public unsafe bool DrawTable()
		{
			var result = false;

			FetchConfigurations();

			var iconPos = FontAwesomeIcon.LocationArrow;
			var iconRot = FontAwesomeIcon.Sync;
			var iconSca = FontAwesomeIcon.ExpandAlt;

			var multiplier = 1f;
			if (ImGui.GetIO().KeyCtrl) multiplier *= ModifierMultCtrl;
			if (ImGui.GetIO().KeyShift) multiplier *= ModifierMultShift / 10; //divide by 10 cause of the native *10 when holding shift on DragFloat

			// Attempt to find the exact size for any font and font size.
			float[] sizes = new float[3];
			sizes[0] = GuiHelpers.CalcIconSize(iconPos).X;
			sizes[1] = GuiHelpers.CalcIconSize(iconRot).X;
			sizes[2] = GuiHelpers.CalcIconSize(iconSca).X;
			var rightOffset = GuiHelpers.GetRightOffset(sizes.Max());


            var inputsWidth = ImGui.GetContentRegionAvail().X - rightOffset;
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - rightOffset);
			result |= ImGui.DragFloat3("##Position", ref Position, 0.0005f);
			UpdateTransformTableState();
			ImGui.SameLine();
			GuiHelpers.IconTooltip(iconPos, "Position", true);
			result |= ImGui.DragFloat3("##Rotation", ref Rotation, 0.1f);
			UpdateTransformTableState();
			ImGui.SameLine();
			GuiHelpers.IconTooltip(iconRot, "Rotation", true);
			result |= ImGui.DragFloat3("##Scale", ref Scale, 0.01f);
			UpdateTransformTableState();
			ImGui.SameLine();
			GuiHelpers.IconTooltip(iconSca, "Scale", true);
			ImGui.PopItemWidth();
			IsEditing = result;
            if (Ktisis.Configuration.TransformTableDisplayMultiplierInputs)
            {
                var cellWidth = inputsWidth / 3 - (ImGui.GetStyle().ItemSpacing.X / 2);
                ImGui.PushItemWidth(cellWidth);
                if (ImGui.DragFloat("##SpeedMult##shift", ref ModifierMultShift, 1f, 0.00001f, 10000f, null, ImGuiSliderFlags.Logarithmic))
                    Ktisis.Configuration.TransformTableModifierMultShift = ModifierMultShift;
                ImGui.SameLine();
                if (ImGui.DragFloat("##SpeedMult##ctrl", ref ModifierMultCtrl, 1f, 0.00001f, 10000f, null, ImGuiSliderFlags.Logarithmic))
                    Ktisis.Configuration.TransformTableModifierMultCtrl = ModifierMultCtrl;
                ImGui.PopItemWidth();
                ImGui.SameLine();
                GuiHelpers.IconTooltip(FontAwesomeIcon.Running, "Ctrl and Shift speed multipliers");
            }
            return result;
		}

		private unsafe void UpdateTransformTableState()
		{
			if (ImGui.IsItemClicked() &&
				(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Left)))
			{
				_state = TransformTableState.EDITING;
			}
			
			if (ImGui.IsItemDeactivatedAfterEdit()) _state = TransformTableState.IDLE;

			EventManager.FireOnTransformationMatrixChangeEvent(_state);
		}

		public bool Draw(Bone bone) {
			var result = false;
			var gizmo = OverlayWindow.GetGizmo(bone.UniqueName);
			if (gizmo != null) {
				if (!IsEditing)
					(Position, Rotation, Scale) = gizmo.Decompose();

				(var savedPosition, var savedRotation, var savedScale) = (Position, Rotation, Scale);
				if (DrawTable()) {
					(var deltaPosition, var deltaRotation, var deltaScale) = (savedPosition - Position, savedRotation - Rotation, savedScale - Scale);
					gizmo.InsertEulerDeltaMatrix(-deltaPosition, -deltaRotation, -deltaScale);
					result = true;
				}
			}
			IsEditing = result;
			return result;
		}
		public bool Draw(ref Vector3 pos, ref Quaternion rot, ref Vector3 scale) {
			if (!IsEditing)
				Update(pos, rot, scale);

			var result = DrawTable();
			pos = Position;
			rot = MathHelpers.ToQuaternion(Rotation);
			if (Scale.X > 0 && Scale.Y > 0 && Scale.Z > 0)
				scale = Scale;
			return result;
		}

		public bool Draw(ref hkVector4f pos, ref hkQuaternionf rot, ref hkVector4f scale) {
			var result = DrawTable();

			pos = pos.SetFromVector3(Position);

			var rad = MathHelpers.ToRadians(Rotation);
			rot.setFromEulerAngles1(rad.X, rad.Y, rad.Z);

			scale = scale.SetFromVector3(Scale);

			return result;
		}

		public bool Draw(bool update, ref hkVector4f pos, ref hkQuaternionf rot, ref hkVector4f scale) {
			if (update) Update(pos.ToVector3(), rot.ToQuat(), scale.ToVector3());
			return Draw(ref pos, ref rot, ref scale);
		}
	}
}