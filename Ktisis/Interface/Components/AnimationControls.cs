using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;

using ImGuiNET;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Excel.GeneratedSheets;

using Ktisis.Interop.Hooks;
using Ktisis.Structs.Actor;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	public static class AnimationControls {
		private static int InputBaseAction;
		private static bool InputInterrupt = true;
		private static int InputBlendAction;
		private static string SearchTerm = string.Empty;

		private static List<ActionTimeline>? BaseSearchSelector;
		private static List<ActionTimeline>? BlendSearchSelector;
		public unsafe static void Draw(Actor* actor) {
			if (ImGui.CollapsingHeader("Animation Control")) {
				if (PoseHooks.PosingEnabled) {
					ImGui.Text("Animation Control is available when:");
					ImGui.BulletText("Posing is disabled");
					return;
				}

				Character* character = (Character*)actor;
				DrawBaseSelect(character);
				DrawBlendSelect(character);
				DrawReset(character);
				DrawSpeedControl(character);
			}
		}

		private unsafe static void DrawBaseSelect(Character* character) {
			ImGui.SetNextItemWidth(120f);
			ImGui.InputInt("###input_base", ref InputBaseAction);
			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Search, "Search", hiddenLabel: "base_search"))
				OpenBaseSearchSelector();

			ImGui.SameLine();
			ImGui.Checkbox("###interrupt", ref InputInterrupt);
			ImGui.SameLine();
			if (ImGui.Button("Base")) {
				character->SetMode(Character.CharacterModes.AnimLock, 0);
				SetBaseOverride(character, (ushort)InputBaseAction, InputInterrupt);
			}

			if (BaseSearchSelector != null)
				DrawBaseSearchSelector();
		}

		private unsafe static void DrawBlendSelect(Character* character) {
			// Blend Animation
			ImGui.SetNextItemWidth(120f);
			ImGui.InputInt("###input_blend", ref InputBlendAction);
			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Search, "Search", hiddenLabel: "blend_search"))
				OpenBlendSearchSelector();

			ImGui.SameLine();
			if (ImGui.Button("Blend"))
				character->ActionTimelineManager.Driver.PlayTimeline((ushort)InputBlendAction);

			if (BlendSearchSelector != null)
				DrawBlendSearchSelector();
		}

		private unsafe static void DrawReset(Character* character) {
			bool isOverridden = character->Mode == Character.CharacterModes.AnimLock;
			if (!isOverridden) ImGui.BeginDisabled();
			if (ImGui.Button(("Reset"))) {
				character->SetMode(Character.CharacterModes.Normal, 0);
				SetBaseOverride(character, 0, true);
			}
			if (!isOverridden) ImGui.EndDisabled();
		}

		private unsafe static void DrawSpeedControl(Character* character) {
			var originalMode = (int)ActorHooks.SpeedControlMode;
			var refMode = originalMode;


			ImGui.SetNextItemWidth(120f);
			ImGui.Combo("Speed Control Mode", ref refMode, Enum.GetNames(typeof(ActorHooks.SpeedControlModes)).ToArray(), Enum.GetValues(typeof(ActorHooks.SpeedControlModes)).Length);

			if (originalMode != refMode) {
				ActorHooks.SpeedControlMode = (ActorHooks.SpeedControlModes)refMode;
				SetSlotSpeed(character, ActionTimelineSlots.Base, 1f); // Flush the speed state
			}

			if (ActorHooks.SpeedControlMode == ActorHooks.SpeedControlModes.Global) {
				ImGui.Text("All Slots (Global)");
				ImGui.SetNextItemWidth(190f);
				ImGui.SliderFloat("Speed###global_speed", ref character->ActionTimelineManager.OverallSpeed, 0f, 5f);
				ImGui.Spacing();
			}

			var slots = Enum.GetValues<ActionTimelineSlots>();
			foreach (var slot in slots) {
				var slotName = Enum.GetName(slot);
				var actionid = character->ActionTimelineManager.Driver.TimelineIds[(uint)slot];
				if (actionid == 0)
					continue;

				ImGui.Text($"{(int)slot} ({slotName}): {actionid} - {Services.DataManager.GameData.GetExcelSheet<ActionTimeline>()!.GetRow(actionid)!.Key}");

				if (ActorHooks.SpeedControlMode == ActorHooks.SpeedControlModes.Slot) {
					var originalSpeed = character->ActionTimelineManager.Driver.TimelineSpeeds[(uint)slot];
					var refSpeed = originalSpeed;

					ImGui.SetNextItemWidth(190f);
					ImGui.SliderFloat($"Speed###slot_{(uint)slot}_speed", ref refSpeed, 0f, 5f);

					if (originalSpeed != refSpeed)
						SetSlotSpeed(character, slot, refSpeed);
				}
			}

			for (var skeletonIdx = 0; skeletonIdx < 2; ++skeletonIdx) {
				var skeleton = character->GameObject.DrawObject->Skeleton->PartialSkeletons->GetHavokAnimatedSkeleton(skeletonIdx);
				for (var animControlIdx = 0; animControlIdx < skeleton->AnimationControls.Length; ++animControlIdx) {
					var animControl = character->GameObject.DrawObject->Skeleton->PartialSkeletons->GetHavokAnimatedSkeleton(skeletonIdx)->AnimationControls[animControlIdx].Value;
					var duration = animControl->hkaAnimationControl.Binding.ptr->Animation.ptr->Duration;

					var speedEnabled = !((Actor*)character)->IsMotionEnabled;
					var scrubEnabled = animControl->PlaybackSpeed == 0f;

					ImGui.Text($"Skeleton {skeletonIdx} Control {animControlIdx}");

					if (!speedEnabled) ImGui.BeginDisabled();
					ImGui.SetNextItemWidth(190f);
					ImGui.SliderFloat($"Speed###slot_{skeletonIdx}_{animControlIdx}_pbspeed", ref animControl->PlaybackSpeed, 0f, 5f);
					if (!speedEnabled) ImGui.EndDisabled();


					if (!scrubEnabled) ImGui.BeginDisabled();
					ImGui.SetNextItemWidth(190f);
					ImGui.SliderFloat($"Scrub###slot_{skeletonIdx}_{animControlIdx}_scrub", ref animControl->hkaAnimationControl.LocalTime, 0f, duration);
					if (!scrubEnabled) ImGui.EndDisabled();

					if ((speedEnabled || scrubEnabled) && animControl->hkaAnimationControl.LocalTime >= duration - 0.05f)
						animControl->hkaAnimationControl.LocalTime = 0.0f;
				}
			}

			ImGui.Spacing();
		}

		private unsafe static void SetBaseOverride(Character* character, ushort actionId, bool interrupt) {
			character->ActionTimelineManager.BaseOverride = actionId;
			if (InputInterrupt)
				character->ActionTimelineManager.Driver.PlayTimeline(actionId);
		}

		private unsafe static void SetSlotSpeed(Character* character, ActionTimelineSlots slot, float speed) {
			ActionTimelineDriver* driver = &character->ActionTimelineManager.Driver;
			ActorHooks.SetSlotSpeedHook.Original(new IntPtr(driver), (uint)slot, speed);
		}

		private static void OpenBaseSearchSelector() {
			BaseSearchSelector = Services.DataManager.GameData.GetExcelSheet<ActionTimeline>()!
				.Where(i => !string.IsNullOrEmpty(i.Key))
				.Where(i => i.Slot == (int)ActionTimelineSlots.Base)
				.ToList();
		}
		private static void CloseBaseSearchSelector() {
			BaseSearchSelector = null;
		}

		private static void OpenBlendSearchSelector() {
			BlendSearchSelector = Services.DataManager.GameData.GetExcelSheet<ActionTimeline>()!
				.Where(i => !string.IsNullOrEmpty(i.Key))
				.ToList();
		}

		private static void CloseBlendSearchSelector() {
			BlendSearchSelector = null;
		}

		private static void DrawBaseSearchSelector() {
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				BaseSearchSelector!,
				(e, input) => e.Where(t => $"{t.RowId} - {t.Key}".Contains(input, StringComparison.OrdinalIgnoreCase)),
				(t, a) => {
					var selected = ImGui.Selectable($"{t.RowId} - {t.Key}###base_{t.RowId}", a);
					var focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				t => InputBaseAction = (ushort)t.RowId,
				CloseBaseSearchSelector,
				ref SearchTerm,
				"Action Select",
				"##base_action_select",
				"##base_action_search");
		}

		private static void DrawBlendSearchSelector() {
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				BlendSearchSelector!,
				(e, input) => e.Where(t => $"{t.RowId} - {t.Key}".Contains(input, StringComparison.OrdinalIgnoreCase)),
				(t, a) => {
					var selected = ImGui.Selectable($"{t.RowId} - {t.Key}###blend_{t.RowId}", a);
					var focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				t => InputBlendAction = (ushort)t.RowId,
				CloseBlendSearchSelector,
				ref SearchTerm,
				"Action Select",
				"##blend_action_select",
				"##blend_action_search");
		}
	}
}
