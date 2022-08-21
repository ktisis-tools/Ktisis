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

		// Apply customize

		public void Apply(Customize custard) {
			Target->Customize = custard;
			Target->Redraw();
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
			var title = Plugin.Configuration.DisplayCharName ? $"{Target->Name}" : "Appearance";
			if (ImGui.Begin(title, ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize)) {
				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				// Customize

				var custom = Target->Customize;

				// Race

				var curRace = Plugin.Locale.GetString($"{custom.Race}");
				if (ImGui.BeginCombo("Race", curRace)) {
					foreach (Race race in Enum.GetValues(typeof(Race))) {
						var raceName = Plugin.Locale.GetString($"{race}");
						if (ImGui.Selectable(raceName, race == custom.Race)) {
							custom.Race = race;
							custom.Tribe = (Tribe)(
								Customize.GetRaceTribeIndex(race)
								+ 1 - (byte)custom.Tribe % 2
							);
							Apply(custom);
						}
					}

					ImGui.SetItemDefaultFocus();
					ImGui.EndCombo();
				}

				// Tribe

				var curTribe = Plugin.Locale.GetString($"{custom.Tribe}");
				if (ImGui.BeginCombo("Tribe", curTribe)) {
					var tribes = Enum.GetValues(typeof(Tribe));
					for (int i = 0; i < 2; i++) {
						var tribe = (Tribe)(Customize.GetRaceTribeIndex(custom.Race) + i);
						if (ImGui.Selectable(Plugin.Locale.GetString($"{tribe}"), tribe == custom.Tribe)) {
							custom.Tribe = tribe;
							Apply(custom);
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