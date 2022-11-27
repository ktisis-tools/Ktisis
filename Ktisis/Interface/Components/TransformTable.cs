using System.Numerics;
using System.Linq;
using ImGuiNET;

using Dalamud.Interface;
using FFXIVClientStructs.Havok;

using Ktisis.Helpers;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Bones;
using Ktisis.Util;
using Ktisis.Structs.Actor;
using ImGuizmoNET;

namespace Ktisis.Interface.Components {
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

		public void FetchConfigurations() {
			BaseSpeedPos = Ktisis.Configuration.TransformTableBaseSpeedPos;
			BaseSpeedRot = Ktisis.Configuration.TransformTableBaseSpeedRot;
			BaseSpeedSca = Ktisis.Configuration.TransformTableBaseSpeedSca;
			ModifierMultCtrl = Ktisis.Configuration.TransformTableModifierMultCtrl;
			ModifierMultShift = Ktisis.Configuration.TransformTableModifierMultShift;
			DigitPrecision = $"%.{Ktisis.Configuration.TransformTableDigitPrecision}f";
		}


		// Set stored values.

		public void Update(Vector3 pos, Quaternion rot, Vector3 scale) {
			Position = pos;
			Rotation = MathHelpers.ToEuler(rot);
			Scale = scale;
		}

		// Draw table.

		public bool DrawTable() {
			var result = false;

			var iconPosition = FontAwesomeIcon.LocationArrow;
			var iconRotation = FontAwesomeIcon.Sync;
			var iconScale = FontAwesomeIcon.ExpandAlt;

			FetchConfigurations();

			var multiplier = 1f;
			if (ImGui.GetIO().KeyCtrl) multiplier *= ModifierMultCtrl;
			if (ImGui.GetIO().KeyShift) multiplier *= ModifierMultShift / 10; //divide by 10 cause of the native *10 when holding shift on DragFloat

			var inputsWidth = ImGui.GetContentRegionAvail().X - ControlButtons.ButtonSize.X - ImGui.GetStyle().ItemSpacing.X;
			ImGui.PushItemWidth(inputsWidth);

			// Position
			result |= ImGui.DragFloat3("##Position", ref Position, BaseSpeedPos * multiplier,0,0, DigitPrecision);
			ImGui.SameLine();
			ControlButtons.ButtonChangeOperation(OPERATION.TRANSLATE, iconPosition);
 
			// Rotation
			result |= ImGui.DragFloat3("##Rotation", ref Rotation, BaseSpeedRot * multiplier, 0, 0, DigitPrecision);
			ImGui.SameLine();
			ControlButtons.ButtonChangeOperation(OPERATION.ROTATE, iconRotation);
 
			// Scale
			result |= ImGui.DragFloat3("##Scale", ref Scale, BaseSpeedSca * multiplier, 0, 0, DigitPrecision);
			ImGui.SameLine();
			ControlButtons.ButtonChangeOperation(OPERATION.SCALE, iconScale);

			ImGui.PopItemWidth();
			IsEditing = result;

			if (Ktisis.Configuration.TransformTableDisplayMultiplierInputs) {
				var input2Width = (inputsWidth / 3 * 2) - (ImGui.GetStyle().ItemInnerSpacing.X /3);

				Vector2 mults = new(ModifierMultShift, ModifierMultCtrl);
				ImGui.PushItemWidth(input2Width);
				if (ImGui.DragFloat2("##SpeedMult##shiftCtrl", ref mults, 1f, 0.00001f, 10000f, null, ImGuiSliderFlags.Logarithmic)) {
					Ktisis.Configuration.TransformTableModifierMultShift = mults.X;
					Ktisis.Configuration.TransformTableModifierMultCtrl = mults.Y;
				}
				ImGui.PopItemWidth();
				ImGui.SameLine();
				GuiHelpers.IconTooltip(FontAwesomeIcon.Running, "Ctrl and Shift speed multipliers");
			}

			return result;
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
		public unsafe bool Draw(Actor* target) {
			if (!IsEditing)
				Update(target->Model->Position, target->Model->Rotation, target->Model->Scale);

			var result = DrawTable();
			target->Model->Position = Position;
			target->Model->Rotation = MathHelpers.ToQuaternion(Rotation);
			if (Scale.X > 0 && Scale.Y > 0 && Scale.Z > 0)
				target->Model->Scale = Scale;
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