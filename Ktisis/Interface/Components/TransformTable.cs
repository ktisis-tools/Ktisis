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

namespace Ktisis.Interface.Components {
	// Thanks to Emyka for the original code:
	// https://github.com/ktisis-tools/Ktisis/pull/5

	public class TransformTable {
		public bool IsEditing = false;

		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Scale;

		// Set stored values.

		public void Update(Vector3 pos, Quaternion rot, Vector3 scale) {
			Position = pos;
			Rotation = MathHelpers.ToEuler(rot);
			Scale = scale;
		}

		// Draw table.

		public bool DrawTable() {
			var result = false;

			var iconPos = FontAwesomeIcon.LocationArrow;
			var iconRot = FontAwesomeIcon.Sync;
			var iconSca = FontAwesomeIcon.ExpandAlt;

			// Attempt to find the exact size for any font and font size.
			float[] sizes = new float[3];
			sizes[0] = GuiHelpers.CalcIconSize(iconPos).X;
			sizes[1] = GuiHelpers.CalcIconSize(iconRot).X;
			sizes[2] = GuiHelpers.CalcIconSize(iconSca).X;
			var rightOffset = sizes.Max() + (ImGui.GetStyle().WindowPadding.X * .75f) + 0.1f;
			

			ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - rightOffset);
			result |= ImGui.DragFloat3("##Position", ref Position, 0.0005f);
			ImGui.SameLine();
			GuiHelpers.IconTooltip(iconPos, "Position", true);
			result |= ImGui.DragFloat3("##Rotation", ref Rotation, 0.1f);
			ImGui.SameLine();
			GuiHelpers.IconTooltip(iconRot, "Rotation", true);
			result |= ImGui.DragFloat3("##Scale", ref Scale, 0.01f);
			ImGui.SameLine();
			GuiHelpers.IconTooltip(iconSca, "Scale", true);
			ImGui.PopItemWidth();
			IsEditing = result;
			return result;
		}
		public bool Draw(Bone bone) {
			var result = false;
			var gizmo = OverlayWindow.GetGizmo(bone.UniqueName);
			if (gizmo != null) {
				(var savedPosition, var savedRotation, var savedScale) = gizmo.Decompose();
				(Position, Rotation, Scale) = (savedPosition, savedRotation, savedScale);
				if (DrawTable()) {
					(var deltaPosition, var deltaRotation, var deltaScale) = (savedPosition - Position, savedRotation - Rotation, savedScale - Scale);
					gizmo.InsertEulerDeltaMatrix(deltaPosition, deltaRotation, deltaScale);
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