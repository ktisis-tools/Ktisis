using System.Numerics;
using ImGuiNET;

using FFXIVClientStructs.Havok;

using Ktisis.Helpers;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Structs.Bones;

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
			result |= ImGui.DragFloat3("Position", ref Position, 0.0005f);
			result |= ImGui.DragFloat3("Rotation", ref Rotation, 0.1f);
			result |= ImGui.DragFloat3("Scale", ref Scale, 0.01f);
			IsEditing = result;
			return result;
		}

		public bool Draw(Bone bone)
		{
			var result = false;
			var gizmo = OverlayWindow.GetGizmo(bone.UniqueName);
			if (gizmo != null)
			{
				(var savedPosition, var savedRotation, var savedScale) = gizmo.Decompose();
				(var position, var rotation, var scale) = (savedPosition, savedRotation, savedScale);
				if (Draw(ref position, ref rotation, ref scale))
				{
					(var deltaPosition, var deltaRotation, var deltaScale) = (savedPosition - position, savedRotation - rotation, savedScale - scale);
					gizmo.InsertEulerDeltaMatrix(deltaPosition, deltaRotation, deltaScale);
					result = true;
				}
			}
			IsEditing = result;
			return result;
		}
		public bool Draw(ref Vector3 position, ref Vector3 rotation, ref Vector3 scale)
		{
			var result = false;
			result |= ImGui.DragFloat3("Position", ref position, 0.0005f, -10000f, 10000f, "%.5f");
			result |= ImGui.DragFloat3("Rotation", ref rotation, 0.1f);
			result |= ImGui.DragFloat3("Scale", ref scale, 0.01f);
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