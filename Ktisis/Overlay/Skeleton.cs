using System;

using ImGuiNET;

using Dalamud.Logging;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Havok;
using Ktisis.Structs.Ktisis;

namespace Ktisis.Overlay {
	public sealed class Skeleton {
		public GameGui Gui;

		public GameObject? Subject;

		public Skeleton(GameGui gui, GameObject? subject) {
			Gui = gui;
			Subject = subject;
		}

		public unsafe ActorModel* GetSubjectModel() {
			return ((Actor*)Subject?.Address)->Model;
		}

		public unsafe void Draw(ImDrawListPtr draw) {
			if (Subject == null)
				return;

			var model = GetSubjectModel();
			if (model == null)
				return;

			var cam = CameraManager.Instance()->Camera;
			//PluginLog.Information(string.Format("{0}", cam->Distance));

			foreach (HkaIndex index in *model->HkaIndex) {
				var pose = index.Pose;
				if (index.Pose == null)
					continue;

				var bones = new BoneList(pose);
				foreach (Bone bone in bones) {
					var worldPos = Subject.Position + bone.Rotate(model->Rotation) * model->Height;

					Gui.WorldToScreen(worldPos, out var pos);
					draw.AddCircleFilled(pos, Math.Max(3.0f, 10.0f - cam->Distance), 0x80ffffff, 100);

					if (bone.ParentId > 0) {
						var parent = bones.GetParentOf(bone);
						var parentPos = Subject.Position + parent.Rotate(model->Rotation) * model->Height;

						Gui.WorldToScreen(parentPos, out var pPos);
						draw.AddLine(pos, pPos, 0xffffffff);
					}
				}
			}
		}
	}
}
