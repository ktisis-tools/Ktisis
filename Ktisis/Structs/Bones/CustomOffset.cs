using System.Numerics;

using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Bones {
	public class CustomOffset {
		public unsafe static Vector3 GetBoneOffset(Bone bone) {
			var target = Ktisis.Target; // TODO: Get bone's owner actor instead of target's

			if (!Ktisis.Configuration.CustomBoneOffset.TryGetValue(GetBodyTypeFromActor(target), out var bonesOffsets))
				return new();
			if (!bonesOffsets.TryGetValue(bone.HkaBone.Name.String, out Vector3 offset))
				return new();
			return offset;
		}
		public unsafe static BodyType GetBodyTypeFromActor(Actor.Actor* actor) {

			var gender = actor->Customize.Gender;
			var race = actor->Customize.Race;

			if (gender == Gender.Male)
				switch (race) {
					case Race.Lalafell: return BodyType.LalafellinMale;
					case Race.Roegadyn:
					case Race.Hrothgar: return BodyType.HrothgarRoegadyn;
					default: return BodyType.tallMale;
				} else switch (race) {
					case Race.Lalafell: return BodyType.LalafellinFemale;
					default: return BodyType.tallFemale;
				}
		}

		// This does the math to be inserted in Bone.GetWorldPos() equation
		public static unsafe Vector3 CalculateWorldOffset(ActorModel* model, Bone bone) =>
			Vector3.Transform(Vector3.Transform(GetBoneOffset(bone), bone.Transform.Rotation.ToQuat()), model->Rotation);

		public enum BodyType {
			tallMale,
			tallFemale,
			HrothgarRoegadyn,
			LalafellinMale,
			LalafellinFemale,
		}
	}
}
