using ImGuiNET;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Posing;
using Ktisis.Services;
using Ktisis.Scene.Skeletons;
using Ktisis.Scene.Interfaces;
using Ktisis.Interface.Dialog;
using Ktisis.Structs.Actor;

namespace Ktisis.Scene.Actors {
	public class ActorObject : SkeletonObject, ITransformable, IHasSkeleton {
		// ActorObject

		private int Index;

		private string? Nickname;

		public ActorObject(int x) {
			Index = x;
			//AddChild(new SkeletonObject());
		}

		// Manipulable

		public override uint Color => 0xFF6EE266;

		public unsafe override string Name {
			get {
				var actor = GetActor();
				return actor != null ? actor->GetNameOrId() : "INVALID";
			}
			set => Nickname = value;
		}

		public override void Select() {
			if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
				PluginLog.Information($"Target {Index}");
			} else {
				var ctrl = ImGui.IsKeyDown(ImGuiKey.LeftCtrl);
				EditorService.Select(this, ctrl);
			}
		}

		public override void Context() {
			var ctx = new ContextMenu();

			ctx.AddSection(new() {
				{ "Select", Select },
				{ "Set nickname...", null! }
			});

			ctx.AddSection(new() {
				{ "Open appearance editor", null! },
				{ "Open animation control", null! },
				{ "Open gaze control", null! }
			});

			ctx.Show();
		}

		// Actor

		internal unsafe Actor* GetActor()
			=> (Actor*)DalamudServices.ObjectTable.GetObjectAddress(Index);

		// SkeletonObject

		public unsafe override Skeleton* GetSkeleton() {
			var actor = GetActor();
			return actor != null ? actor->GetSkeleton() : null;
		}

		// Transformable

		public unsafe override ActorModel* GetObject() {
			var actor = GetActor();
			if (actor == null) return null;

			return actor->Model;
		}

		public unsafe override Transform? GetTransform() {
			var actor = GetActor();
			if (actor == null || actor->Model == null) return null;
			return Transform.FromHavok(actor->Model->Transform);
		}

		public unsafe override void SetTransform(Transform trans) {
			var actor = GetActor();
			if (actor == null || actor->Model == null) return;
			actor->Model->Transform = trans.ToHavok();
		}
	}
}