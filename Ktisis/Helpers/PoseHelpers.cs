using System.IO;
using System.Collections.Generic;

using Ktisis.Data.Files;
using Ktisis.Data.Serialization;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Poses;
using Ktisis.Interop.Hooks;

namespace Ktisis.Helpers {
	internal class PoseHelpers {
		public unsafe static void ExportPose(Actor* actor, string path, PoseMode modes) {
			var model = actor->Model;
			if (model == null) return;

			var skeleton = model->Skeleton;
			if (skeleton == null) return;

			var pose = new PoseFile {
				Position = model->Position,
				Rotation = model->Rotation,
				Scale = model->Scale,
				Bones = new ()
			};

			pose.Bones.Store(skeleton);

			if (modes.HasFlag(PoseMode.Weapons)) {
				var main = actor->GetWeaponSkeleton(WeaponSlot.MainHand);
				if (main != null) {
					pose.MainHand = new ();
					pose.MainHand.Store(main);
				}

				var off = actor->GetWeaponSkeleton(WeaponSlot.OffHand);
				if (off != null) {
					pose.OffHand = new ();
					pose.OffHand.Store(off);
				}

				var prop = actor->GetWeaponSkeleton(WeaponSlot.Prop);
				if (prop != null) {
					pose.Prop = new ();
					pose.Prop.Store(prop);
				}
			}

			var json = JsonParser.Serialize(pose);

			using var file = new StreamWriter(path);
			file.Write(json);
		}

		public unsafe static void ImportPose(Actor* actor, List<string> path, PoseMode modes) {
			var content = File.ReadAllText(path[0]);
			if (Path.GetExtension(path[0]).Equals(".cmp")) content = LegacyPoseHelpers.ConvertLegacyPose(content);
			var pose = JsonParser.Deserialize<PoseFile>(content);
			if (pose == null) return;

			if (actor->Model == null) return;

			var skeleton = actor->Model->Skeleton;
			if (skeleton == null) return;

			pose.ConvertLegacyBones();

			// Ensure posing is enabled.
			if (!PoseHooks.PosingEnabled && !PoseHooks.AnamPosingEnabled)
				PoseHooks.EnablePosing();

			if (pose.Bones != null) {
				for (var p = 0; p < skeleton->PartialSkeletonCount; p++) {
					switch (p) {
						case 0:
							if (!modes.HasFlag(PoseMode.Body)) continue;
							break;
						case 1:
							if (!modes.HasFlag(PoseMode.Face)) continue;
							break;
					}

					pose.Bones.ApplyToPartial(skeleton, p, Ktisis.Configuration.PoseTransforms);
				}
			}

			if (modes.HasFlag(PoseMode.Weapons)) {
				var wepTrans = Ktisis.Configuration.PoseTransforms;
				if (Ktisis.Configuration.PositionWeapons)
					wepTrans |= PoseTransforms.Position;

				if (pose.MainHand != null) {
					var skele = actor->GetWeaponSkeleton(WeaponSlot.MainHand);
					if (skele != null) pose.MainHand.Apply(skele, wepTrans);
				}

				if (pose.OffHand != null) {
					var skele = actor->GetWeaponSkeleton(WeaponSlot.OffHand);
					if (skele != null) pose.OffHand.Apply(skele, wepTrans);
				}

				if (pose.Prop != null) {
					var skele = actor->GetWeaponSkeleton(WeaponSlot.Prop);
					if (skele != null) pose.Prop.Apply(skele, wepTrans);
				}
			}
		}
	}
}
