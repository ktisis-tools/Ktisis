using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Structs.Actor;

namespace Ktisis.Interface {
	internal unsafe class CustomizeGui {
		Ktisis Plugin;

		public bool Visible = false;

		public Actor* Target;

		public static readonly float SliderRate = 0.25f;

		// Constructor

		public CustomizeGui(Ktisis plugin) {
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

				// Gender

				var isM = custom.Gender == Gender.Male;
				if (ImGuiComponents.IconButton(isM ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus)) {
					custom.Gender = isM ? Gender.Female : Gender.Male;
					Apply(custom);
				}

				ImGui.SameLine();
				ImGui.Text(isM ? "Male" : "Female");

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

				// Sliders

				ImGui.Separator();

				// Height

				var height = (int)custom.Height;
				if (ImGui.DragInt("Height", ref height, SliderRate, 0, 100)) {
					custom.Height = (byte)height;
					Apply(custom);
				}

				// Feature

				var feat = (int)custom.RaceFeatureSize;
				var featName = "Feature";

				// TODO: Streamline this; pull from CharaMakeCustomize.
				if (custom.Race == Race.Miqote
				|| custom.Race == Race.AuRa
				|| custom.Race == Race.Hrothgar) {
					featName = "Tail Length";
				} else if (custom.Race == Race.Viera
				|| custom.Race == Race.Elezen
				|| custom.Race == Race.Lalafell) {
					featName = "Ear Length";
				} else if (custom.Race == Race.Hyur
				|| custom.Race == Race.Roegadyn) {
					featName = "Muscle Tone";
				}

				if (ImGui.DragInt(featName, ref feat, SliderRate, 0, 100)) {
					custom.RaceFeatureSize = (byte)feat;
					Apply(custom);
				}

				// Boobas

				var bust = (int)custom.BustSize;
				if (ImGui.DragInt("Bust Size", ref bust, SliderRate, 0, 100)) {
					custom.BustSize = (byte)bust;
					Apply(custom);
				}

				// Numbers

				ImGui.Separator();

				// End

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
		}
	}
}