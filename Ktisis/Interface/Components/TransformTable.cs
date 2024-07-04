using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;

using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

using Ktisis.Util;
using Ktisis.Events;
using Ktisis.Helpers;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

namespace Ktisis.Interface.Components {
	// Thanks to Emyka for the original code:
	// https://github.com/ktisis-tools/Ktisis/pull/5

	public class TransformTable {
		public bool IsEditing = false;
		public bool IsActive = false;
		public bool IsEdited = false; // auhfghjjfduuhghuughhhj

		private float BaseSpeedPos;
		private float BaseSpeedRot;
		private float BaseSpeedSca;
		private float ModifierMultCtrl;
		private float ModifierMultShift;
		private string DigitPrecision = "%.3f";

		private static Vector4[] AxisColors = {
			new(1, 0.328f, 0.211f, 1),
			new (0.33f, 0.82f, 0, 1),
			new(0, 0.33f, 1, 1)
		};
		private static Vector4[] DefaultColors = { // :(
			new(0.75f, 0.75f, 0.75f, 1f),
			new(0.75f, 0.75f, 0.75f, 1f),
			new(0.75f, 0.75f, 0.75f, 1f)
		};

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

		private float CalcItemWidth(int count = 3, float offset = 0)
			=> (float)Math.Ceiling((ImGui.GetContentRegionAvail().X - offset - ImGui.GetStyle().ItemSpacing.X * (count - 1)) / count);

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
			
			ImGui.PushItemWidth(CalcItemWidth(3, ControlButtons.ButtonSize.X + ImGui.GetStyle().ItemSpacing.X));

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
			ImGui.PushItemWidth(CalcItemWidth(2));
			if (ImGui.DragFloat2("##SpeedMult##shiftCtrl", ref mults, 1f, 0.00001f, 10000f, null, ImGuiSliderFlags.Logarithmic)) {
				Ktisis.Configuration.TransformTableModifierMultShift = mults.X;
				Ktisis.Configuration.TransformTableModifierMultCtrl = mults.Y;
			}
			ImGui.PopItemWidth();
			ImGui.SameLine();
			GuiHelpers.IconTooltip(FontAwesomeIcon.Running, "Ctrl and Shift speed multipliers");

			return result;
		}

		public bool ColoredDragFloat3(string label, ref Vector3 value, float speed, float borderSize = 1.0f) {
			ImGui.PushItemWidth(CalcItemWidth(3));
			var result = ColoredDragFloat3(label, ref value, speed, AxisColors, out _, borderSize);
			ImGui.PopItemWidth();
			return result;
		}

		public bool ColoredDragFloat2(string label, ref Vector2 value, float speed, float borderSize = 1.0f) {
			ImGui.PushItemWidth(CalcItemWidth(2));
			var result = ColoredDragFloat2(label, ref value, speed, AxisColors, borderSize);
			ImGui.PopItemWidth();
			return result;
		}
		
		public bool DragFloat2(string label, ref Vector2 value, float speed, float borderSize = 1.0f) {
			ImGui.PushItemWidth(CalcItemWidth(2));
			var result = ColoredDragFloat2(label, ref value, speed, DefaultColors, borderSize, true);
			ImGui.PopItemWidth();
			return result;
		}

		private bool ColoredDragFloat3(string label, ref Vector3 value, float speed, IReadOnlyList<Vector4> colors, out bool anyActive, float borderSize = 1.0f) {
			if (colors.Count < 3) {
				anyActive = false;
				return false;
			}

			var result = false;
			anyActive = false;

			IsEdited = false;
			
			result |= ColoredDragFloat(label + "X", ref value.X, speed, colors[0], out var active, borderSize);
			anyActive |= active;
			IsEdited |= ImGui.IsItemDeactivatedAfterEdit();
			ImGui.SameLine();
			result |= ColoredDragFloat(label + "Y", ref value.Y, speed, colors[1], out active, borderSize);
			anyActive |= active;
			IsEdited |= ImGui.IsItemDeactivatedAfterEdit();
			ImGui.SameLine();
			result |= ColoredDragFloat(label + "Z", ref value.Z, speed, colors[2], out active, borderSize);
			anyActive |= active;
			IsEdited |= ImGui.IsItemDeactivatedAfterEdit();

			IsActive = anyActive;

			return result;
		}
		
		private bool ColoredDragFloat2(string id, ref Vector2 value, float speed, IReadOnlyList<Vector4> colors, float borderSize = 1.0f, bool showXY = false) {
			if (colors.Count < 2) {
				return false;
			}

			IsActive = false;
			IsEdited = false;

			var result = false;
			result |= ColoredDragFloat($"##{id}_X", ref value.X, speed, colors[0], out var active, borderSize, showXY ? "X:  " : "");
			IsActive |= active;
			ImGui.SameLine();
			result |= ColoredDragFloat($"##{id}_Y", ref value.Y, speed, colors[1], out active, borderSize, showXY ? "Y:  " : "");
			IsActive |= active;
			return result;
		}

		private bool ColoredDragFloat(string label, ref float value, float speed, Vector4 color, out bool active, float borderSize = 1.0f, string prefix = "") {
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, borderSize);
			ImGui.PushStyleColor(ImGuiCol.Border, color);
			var result = ImGui.DragFloat(label, ref value, speed, 0, 0, $"{prefix}{DigitPrecision}");
			IsEdited |= ImGui.IsItemDeactivatedAfterEdit();
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
