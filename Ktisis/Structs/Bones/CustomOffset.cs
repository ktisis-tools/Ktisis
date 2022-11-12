using System.Numerics;

namespace Ktisis.Structs.Bones {
	internal class CustomOffset {


		public unsafe static Vector3 GetBoneOffset(Bone bone) {

			// TODO: use BodyType resolver
			var target = Ktisis.Target;
			string genderRaceIndex = $"{target->Customize.Gender}_{target->Customize.Race}";
			if (!Ktisis.Configuration.CustomBoneOffset.TryGetValue(genderRaceIndex, out var bonesOffsets))
				return new();
			if (!bonesOffsets.TryGetValue(bone.UniqueId, out Vector3 offset))
				return new();
			return offset;
		}

		private enum BodyType {
			tallMale,
			tallFemale,
			HrothgarRoegadyn,
			LalafellinMale,
			LalafellinFemale,
		}
	}
}
