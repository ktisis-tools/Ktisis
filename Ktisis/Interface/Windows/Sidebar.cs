using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;

using Ktisis.Services;
using Ktisis.Structs.Actor;
using Ktisis.Interop.Hooks;
using Ktisis.Interface.Dialog;
using Ktisis.Interface.Widgets;
using Ktisis.Interface.Components.Posing;

namespace Ktisis.Interface.Windows {
	public class Sidebar : KtisisWindow {
		public static string Name = $"Ktisis ({Ktisis.Version})##Sidebar";

		// Constants

		private static Vector2 ControlButtonSize = new Vector2(28, 28);

		// Constructor

		public Sidebar() : base(
			Name, ImGuiWindowFlags.None
		) {
			RespectCloseHotkey = false;
		}

		// Draw window

		public override void Draw() {
			// Set size constraints

			SizeConstraints = new WindowSizeConstraints {
				MinimumSize = new Vector2(270, 110),
				MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
			};

			// Interface

			var gposeOn = GPoseService.IsInGPose;

			PoseState.DrawPoseState(gposeOn);
			ImGui.SameLine();
			PoseState.DrawPoseSwitch(gposeOn);

			ImGui.Spacing();

			ControlButtons();

			DrawSelectState();

			ImGui.Spacing();

			DrawSceneTree();

			ImGui.End();
		}

		// Draw selection state

		private unsafe static void DrawSelectState() {
			var actor = GPoseService.TargetActor;
			if (actor == null) return;

			var frameSize = new Vector2(
				ImGui.GetContentRegionAvail().X - Align.WidthMargin,
				ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().FramePadding.Y
			);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(
				ImGui.GetStyle().FramePadding.X,
				ImGui.GetStyle().FramePadding.Y / 2
			));

			if (ImGui.BeginChildFrame(8, frameSize, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)) {
				////var bone = Skeleton.GetSelectedBone();
				////var select = Skeleton.BoneSelect;

				AnimStateIndicator(actor);

				ImGui.BeginGroup();

				var spacing = ImGui.GetStyle().ItemSpacing.Y / 2;
				var padding = ImGui.GetStyle().FramePadding.Y / 2;

				// display target name
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding);
				ImGui.Text(actor->GetNameOrId());

				// display selected bone name
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() - spacing - padding);
				/*if (select.Active && bone != null)
					ImGui.Text(bone.LocaleName);
				else*/
					ImGui.TextDisabled("No bone selected");

				ImGui.EndGroup();
				ImGui.EndChildFrame();
			}
			ImGui.PopStyleVar();
		}

		private unsafe static void AnimStateIndicator(Actor* actor) {
			var active = PoseHooks.IsGamePlaybackRunning(actor);

			var icon = active ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
			var size = Icons.CalcIconSize(icon).X;

			ImGui.SameLine(size / 1.5f);

			ImGui.BeginGroup();

			ImGui.Dummy(new Vector2(size, size) / 2);

			Icons.DrawIcon(icon);
			Text.Tooltip(active ? "Animation playback is active for this target." : "Animation playback is paused for this target.");

			ImGui.EndGroup();

			ImGui.SameLine(size * 2.5f);
		}

		// Control buttons

		private static void ControlButtons() {
			ImGui.BeginGroup();

			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(5, 5));

			OverlayVisibility();

			ImGui.SameLine();

			CameraSelect();

			ImGui.SameLine();

			ExtrasButton();

			ImGui.PopStyleVar();

			ImGui.EndGroup();
		}

		private static void AddItemButton() {
			if (Buttons.IconButton(FontAwesomeIcon.Plus, ControlButtonSize)) {
				var ctx = new ContextMenu();

				ctx.AddSection(new() {
					{ "Add existing actor...", null! }
				});

				ctx.AddSection(new() {
					{ "Create new actor", null! },
					{ "Create new camera", null! },
					{ "Create new light source", null! }
				});

				ctx.Show();
			}
		}

		private static void OpenEditor() {
			if (Buttons.IconButton(FontAwesomeIcon.PencilAlt, ControlButtonSize)) {
				if (EditorService.Selection == null) {
					var tar = EditorService.FindTarget(true);
					if (tar != null) tar.Select();
				}

				var window = new TransformEditor();
				window.ToggleOnOrRemove();
			}
		}

		private static void OverlayVisibility() {
			var visible = Ktisis.Configuration.ShowSkeleton;
			var tooltip = visible ? "Disable overlay" : "Enable overlay";
			if (Buttons.IconButtonTooltip(visible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, tooltip, ControlButtonSize))
				Ktisis.Configuration.ShowSkeleton = !visible;
		}

		private static void CameraSelect() {
			var tooltip = false ? "Disable work camera" : "Enable work camera";
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Camera, tooltip, ControlButtonSize)) {
				// Toggle work camera
			}

			ImGui.SameLine();

			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, (ControlButtonSize.Y - 17) / 2));
			ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ControlButtonSize.X - ImGui.GetStyle().FramePadding.X * 2);
			if (ImGui.BeginCombo("##Ktisis_Cam", "GPose Camera")) {
				ImGui.EndCombo();
			}
			ImGui.PopItemWidth();
			ImGui.PopStyleVar();
		}

		private static void ExtrasButton() {
			if (Buttons.IconButton(FontAwesomeIcon.EllipsisH, ControlButtonSize)) {
				var ctx = new ContextMenu();

				ctx.AddSection(new() {
					{ "Open Settings", null! }
				});

				ctx.Show();
			}
		}

		// Draw scene tree

		private static void DrawSceneTree() {
			var avail = ImGui.GetContentRegionAvail().Y - ControlButtonSize.Y - ImGui.GetStyle().FramePadding.Y * 3;

			if (ImGui.BeginChildFrame(471, new Vector2(-1, avail), ImGuiWindowFlags.HorizontalScrollbar)) {
				foreach (var item in EditorService.Items)
					item.DrawTreeNode();

				ImGui.EndChildFrame();

				if (ImGui.IsItemClicked() && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
					EditorService.Selection = null;
			}

			ImGui.BeginGroup();

			ImGui.Spacing();

			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(5, 5));
			AddItemButton();
			ImGui.SameLine();
			OpenEditor();
			ImGui.PopStyleVar();

			ImGui.EndGroup();
		}
	}
}