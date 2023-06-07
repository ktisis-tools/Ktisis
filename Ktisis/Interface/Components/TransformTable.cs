using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;

using FFXIVClientStructs.Havok;

using Ktisis.Events;
using Ktisis.Helpers;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Bones;
using Ktisis.Util;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Components {
	// Thanks to Emyka for the original code:
	// https://github.com/ktisis-tools/Ktisis/pull/5

	public class TransformTable {
		public bool IsEditing = false;
		public bool IsActive = false;

		private float BaseSpeedPos;
		private float BaseSpeedRot;
		private float BaseSpeedSca;
		private float ModifierMultCtrl;
		private float ModifierMultShift;
		private string DigitPrecision = "%.3f";

		private static Vector4[] AxisColors = new[] {
			new Vector4(1, 0.328f, 0.211f, 1),
			new Vector4(0.33f, 0.82f, 0, 1),
			new Vector4(0, 0.33f, 1, 1)
		};

		internal static float InputsWidth => (ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin() - ImGui.GetStyle().ItemSpacing.X * 2.0f) / 3.0f;

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

		public TransformTable Clone() {
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

		public bool DrawTable() {
			var result = false;

			var iconPosition = FontAwesomeIcon.LocationArrow;
			var iconRotation = FontAwesomeIcon.Sync;
			var iconScale = FontAwesomeIcon.ExpandAlt;

			FetchConfigurations();
			
			var multiplier = 1f;
			if (ImGui.GetIO().KeyCtrl) multiplier *= ModifierMultCtrl;
			if (ImGui.GetIO().KeyShift) multiplier *= ModifierMultShift / 10; //divide by 10 cause of the native *10 when holding shift on DragFloat

			ImGui.PushItemWidth(InputsWidth - (ImGui.GetStyle().ItemSpacing.X + ControlButtons.ButtonSize.X) / 3);

			var anyActive = false;
			bool active;

			// Position
			result |= ColoredDragFloat3("##Position", ref Position, BaseSpeedPos * multiplier, AxisColors, out active);
			anyActive |= active;
			ImGui.SameLine();
			ControlButtons.ButtonChangeOperation(OPERATION.TRANSLATE, iconPosition);

			// Rotation
			result |= ColoredDragFloat3("##Rotation", ref Rotation, BaseSpeedRot * multiplier, AxisColors, out active);
			anyActive |= active;
			ImGui.SameLine();
			ControlButtons.ButtonChangeOperation(OPERATION.ROTATE, iconRotation);

			// Scale
			result |= ColoredDragFloat3("##Scale", ref Scale, BaseSpeedSca * multiplier, AxisColors, out active);
			anyActive |= active;
			ImGui.SameLine();
			ControlButtons.ButtonChangeOperation(OPERATION.SCALE, iconScale);

			IsActive = anyActive;
			/* FIXME: Checking `ImGui.IsAnyItemActive` seems overzealous? Should probably replace with new `anyActive`. */
			var newState = result || (IsEditing && ImGui.IsAnyItemActive());

			if (newState && !IsEditing)
				EventManager.FireOnTransformationMatrixChangeEvent(true);
			else if (IsEditing && !newState)
				EventManager.FireOnTransformationMatrixChangeEvent(false);

			IsEditing = newState;

			ImGui.PopItemWidth();

			if (!Ktisis.Configuration.TransformTableDisplayMultiplierInputs)
				return result;
			
			Vector2 mults = new(ModifierMultShift, ModifierMultCtrl);
			ImGui.PushItemWidth(InputsWidth);
			if (ImGui.DragFloat2("##SpeedMult##shiftCtrl", ref mults, 1f, 0.00001f, 10000f, null, ImGuiSliderFlags.Logarithmic)) {
				Ktisis.Configuration.TransformTableModifierMultShift = mults.X;
				Ktisis.Configuration.TransformTableModifierMultCtrl = mults.Y;
			}
			ImGui.PopItemWidth();
			ImGui.SameLine();
			GuiHelpers.IconTooltip(FontAwesomeIcon.Running, "Ctrl and Shift speed multipliers");

			return result;
		}

		public bool DrawFloat3(string label, ref Vector3 value, float speed, out bool anyActive, float borderSize = 1.0f)
			=> ColoredDragFloat3(label, ref value, speed, AxisColors, out anyActive, borderSize);

		private bool ColoredDragFloat3(string label, ref Vector3 value, float speed, IReadOnlyList<Vector4> colors, out bool anyActive, float borderSize = 1.0f) {
			if (colors.Count != 3) {
				anyActive = false;
				return false;
			}

			var result = false;
			anyActive = false;
			
			result |= ColoredDragFloat(label + "X", ref value.X, speed, colors[0], out var active, borderSize);
			anyActive |= active;
			ImGui.SameLine();
			result |= ColoredDragFloat(label + "Y", ref value.Y, speed, colors[1], out active, borderSize);
			anyActive |= active;
			ImGui.SameLine();
			result |= ColoredDragFloat(label + "Z", ref value.Z, speed, colors[2], out active, borderSize);
			anyActive |= active;

			return result;
		}

		private bool ColoredDragFloat(string label, ref float value, float speed, Vector4 color, out bool active, float borderSize = 1.0f) {
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, borderSize);
			ImGui.PushStyleColor(ImGuiCol.Border, color);
			var result = ImGui.DragFloat(label, ref value, speed, 0, 0, DigitPrecision);
			ImGui.PopStyleColor();
			ImGui.PopStyleVar();
			active = ImGui.IsItemActive();
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
