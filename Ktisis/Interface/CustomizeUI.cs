using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Structs.Actor;

namespace Ktisis.Interface {
	internal unsafe class CustomizeUI {
		Ktisis Plugin;

		public bool Visible = false;

		public Actor* Target;

		// Constructor

		public CustomizeUI(Ktisis plugin) {
			Plugin = plugin;
		}

		// Toggle visibility

		public void Show() {
			Visible = true;
		}

		public void Hide() {
			Visible = false;
		}

		// Set target

		public void Show(GameObject? actor) {
			if (actor != null)
				SetTarget(actor);
			Show();
		}

		public unsafe void SetTarget(Actor* actor) {
			Target = actor;
		}

		public unsafe void SetTarget(GameObject actor) {
			SetTarget((Actor*)actor.Address);
		}

		// Draw window

		public void Draw() {
			if (!Visible)
				return;

			if (Target == null)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			// Create window
			if (ImGui.Begin($"{Target->Name}", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize)) {
				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				// Customize

				var custard = Target->Customize;

				// Race

				var curRace = Plugin.Locale.GetString($"{custard.Race}");
				if (ImGui.BeginCombo("Race", curRace)) {
					foreach (var race in Enum.GetValues(typeof(Race))) {
						var raceName = Plugin.Locale.GetString($"{race}");
						if (ImGui.Selectable(raceName, raceName == curRace)) {
							custard.Race = (Race)race;
						}
					}

					ImGui.SetItemDefaultFocus();
					ImGui.EndCombo();
				}

				// End

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
		}
	}
}