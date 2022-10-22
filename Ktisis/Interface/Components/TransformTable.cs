using System.Numerics;
using ImGuiNET;

using FFXIVClientStructs.Havok;

using Ktisis.Helpers;
using Ktisis.Structs;

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
			rot = MathHelpers.ToQuaternion(Rotation).ToHavok();
			scale = scale.SetFromVector3(Scale);
			return result;
		}

		public bool Draw(bool update, ref hkVector4f pos, ref hkQuaternionf rot, ref hkVector4f scale) {
			if (update) Update(pos.ToVector3(), rot.ToQuat(), scale.ToVector3());
			return Draw(ref pos, ref rot, ref scale);
		}
	}
}