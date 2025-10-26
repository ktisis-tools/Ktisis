using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using GLib.Popups;
using GLib.Popups.Decorators;
using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Editor.Animation.Game;
using Ktisis.Editor.Animation.Types;
using Ktisis.Structs.Actors;

namespace Ktisis.Interface.Components.Chara;

[Transient]
public class AnimationEditorTab {
	private readonly static PoseModeEnum[] Modes = [
		PoseModeEnum.Idle, PoseModeEnum.SitGround, PoseModeEnum.SitChair, PoseModeEnum.Sleeping
	];

	// todo: fix additive and lips (pull from different partial skeleton? wrong timeline indexes?)
	private readonly static List<TimelineSlot> ScrubSlots = [
		TimelineSlot.FullBody, TimelineSlot.UpperBody
	];

	private readonly ConfigManager _cfg;
	private readonly ITextureProvider _tex;
	
	private readonly GameAnimationData _animData;

	private bool _openAnimList;
	private readonly AnimationFilter _animFilter = new();
	private readonly PopupList<GameAnimation> _animList;
	
	public IAnimationEditor Editor { set; private get; } = null!;

	public AnimationEditorTab(
		ConfigManager cfg,
		IDataManager data,
		ITextureProvider tex
	) {
		this._cfg = cfg;
		this._tex = tex;

		this._animData = new GameAnimationData(data);

		this._animList = new PopupList<GameAnimation>("##AnimEmoteList", this.DrawAnimationSelect)
			.WithSearch(AnimSearchPredicate)
			.WithFilter(this._animFilter);
	}
	
	// Config

	private Configuration Config => this._cfg.File;

	private ref bool PlayEmoteStart => ref this.Config.Editor.PlayEmoteStart;
	private ref bool ForceLoop => ref this.Config.Editor.ForceLoop;
	
	// Setup

	private bool _isSetup;
	
	public void Setup() {
		if (this._isSetup) return;
		this._isSetup = true;

		this._animData.Build().ContinueWith(task => {
			if (task.Exception != null)
				Ktisis.Log.Error($"Failed to fetch animations:\n{task.Exception}");
		});
	}
	
	// Draw
	
	public void Draw() {
		this.DrawAnimation();
	}
	
	// Animation selector
	
	private uint TimelineId;

	private static float CalcItemHeight() => (ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemInnerSpacing.Y) * 2;

	private void DrawAnimation() {
		ImGui.Spacing();
		
		var avail = ImGui.GetContentRegionAvail();
		using (var _ = ImRaii.Child("##animFrame", avail with { X = avail.X * 0.35f })) {
			ImGui.Text("Animation");
			this.DrawEmote();
			ImGui.Spacing();
			ImGui.Text("Idle Pose");
			this.DrawPose();
		}
		ImGui.SameLine(0, 0);
		using (var _ = ImRaii.Child("##tlFrame", avail with { X = avail.X * 0.65f })) {
			this.DrawTimelines();
		}

		if (this._openAnimList) {
			this._openAnimList = false;
			this._animList.Open();
		}

		if (this._animList.Draw(this._animData.GetAll(), this._animData.Count, out var anim, CalcItemHeight())) {
			if (!this._animFilter.SlotFilterActive)
				this.TimelineId = anim!.TimelineId;
			this.Editor.PlayAnimation(anim!, this.PlayEmoteStart);
		}
	}

	private void DrawEmote() {
		var space = ImGui.GetStyle().ItemInnerSpacing.X;

		if (Buttons.IconButton(FontAwesomeIcon.Search))
			this.OpenAnimationPopup();
		
		ImGui.SameLine(0, space);

		var intId = (int)this.TimelineId;
		if (ImGui.InputInt("##emote", ref intId))
			this.TimelineId = (uint)intId;

		if (ImGui.Button("Play"))
			this.PlayTimeline((uint)intId);
		ImGui.SameLine(0, space);
		if (ImGui.Button("Reset"))
			this.ResetTimeline();
		ImGui.SameLine(0, space);
		ImGui.Checkbox("Loop", ref this.ForceLoop);
		
		ImGui.Spacing();
		
		ImGui.Checkbox("Play emote start", ref this.PlayEmoteStart);
	}

	private void DrawPose() {
		if (!this.Editor.TryGetModeAndPose(out var mode, out var pose))
			return;
		
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2);
		
		if (ImGui.BeginCombo("##Mode", mode.ToString())) {
			foreach (var modeType in Modes) {
				if (ImGui.Selectable(modeType.ToString(), modeType == mode))
					this.Editor.SetPose(modeType, 0);
			}
			ImGui.EndCombo();
		}
		
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2);
		
		if (ImGui.InputInt("##Pose", ref pose, 1)) {
			var count = this.Editor.GetPoseCount(mode);
			pose = pose < 0 ? count - 1 : pose % count;
			this.Editor.SetPose(mode, (byte)pose);
		}
		
		ImGui.Spacing();

		var isWeaponDrawn = this.Editor.IsWeaponDrawn;
		if (ImGui.Checkbox("Weapon drawn", ref isWeaponDrawn))
			this.Editor.ToggleWeapon();

		var posLock = this.Editor.PositionLockEnabled;
		if (ImGui.Checkbox("Freeze positions", ref posLock))
			this.Editor.PositionLockEnabled = posLock;
	}

	private unsafe void DrawTimelines() {
		var speedCtrl = this.Editor.SpeedControlEnabled;
		if (ImGui.Checkbox("Enable speed control", ref speedCtrl)) {
			if (!speedCtrl)
				this.Editor.ResetTimelineSpeeds();
			this.Editor.SpeedControlEnabled = speedCtrl;
		}
		
		ImGui.Spacing();

		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		
		var animTimeline = this.Editor.GetTimeline();
		
		foreach (var slot in Enum.GetValues<TimelineSlot>()) {
			using var _id = ImRaii.PushId($"timeline_{slot}");

			var index = (int)slot;

			if (Buttons.IconButton(FontAwesomeIcon.EllipsisH, new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
				this.OpenAnimationPopup(slot);
			
			ImGui.SameLine(0, spacing);

			var id = animTimeline.TimelineIds[index];
			var timeline = this._animData.GetTimelineById(id);

			ImGui.SetNextItemWidth(40);
			
			var intId = (int)id;
			ImGui.InputInt($"##id{index}", ref intId, 0, 0, flags: ImGuiInputTextFlags.ReadOnly);

			ImGui.SameLine(0, spacing);
			var widthR = ImGui.CalcItemWidth() - ImGui.GetFrameHeight() - 40;
			ImGui.SetNextItemWidth(widthR);
			
			var key = timeline?.Key.ExtractText() ?? string.Empty;
			using (var _disable = ImRaii.Disabled(key.IsNullOrEmpty()))
				ImGui.InputText($"##s{index}", ref key, 256, ImGuiInputTextFlags.ReadOnly);
			
			ImGui.SameLine(0, 0);
			ImGui.LabelText("{0}", $"{slot}");

			var speed = animTimeline.TimelineSpeeds[index];
			using (var _disable = ImRaii.Disabled(!speedCtrl)) {
				ImGui.SetNextItemWidth(ImGui.GetFrameHeight() + spacing + 40);
				var changed = ImGui.InputFloat($"##speed_l{index}", ref speed);
				ImGui.SameLine(0, spacing);
				ImGui.SetNextItemWidth(widthR);
				changed |= ImGui.SliderFloat($"##speed_r{index}", ref speed, 0.0f, 2.0f, "");
				if (changed) this.Editor.SetTimelineSpeed((uint)index, speed);
			}
			ImGui.SameLine(0, 0);
			using (var _disable = ImRaii.Disabled(!speedCtrl))
				ImGui.LabelText("{0}", "Playback Speed");

			if (ScrubSlots.Contains(slot) && !key.IsNullOrEmpty()) {
				// draw active sliders for fullbody/upperbody if they have animations
				var control = this.Editor.GetHkaControl(index); // fetch hkaDefaultControl once per draw and reuse
				var duration = this.Editor.GetHkaDuration(control) ?? 0.0f;
				var localTime = this.Editor.GetHkaLocalTime(control) ?? 0.0f;

				ImGui.SetNextItemWidth(ImGui.GetFrameHeight() + spacing + 40);
				var changed = ImGui.InputFloat($"##scrub_l{index}", ref localTime, flags: ImGuiInputTextFlags.EnterReturnsTrue);
				ImGui.SameLine(0, spacing);
				ImGui.SetNextItemWidth(widthR);
				changed |= ImGui.SliderFloat($"##scrub_r{index}", ref localTime, 0.0f, duration, flags: ImGuiSliderFlags.NoInput);
				if (changed) this.Editor.SetHkaLocalTime(control, Math.Clamp(localTime, 0.0f, duration));
			} else if (ScrubSlots.Contains(slot)) {
				// draw disabled dummy sliders for fullbody/upperbody if not animating
				using var _disable = ImRaii.Disabled();
				var dummy = 0.0f;
				ImGui.SetNextItemWidth(ImGui.GetFrameHeight() + spacing + 40);
				ImGui.InputFloat($"##scrub_l{index}", ref dummy);
				ImGui.SameLine(0, spacing);
				ImGui.SetNextItemWidth(widthR);
				ImGui.SliderFloat($"##scrub_r{index}", ref dummy, 0f, 1f);
			}
			ImGui.SameLine(0, 0);
			using (var _disable = ImRaii.Disabled(ScrubSlots.Contains(slot) && key.IsNullOrEmpty()))
				ImGui.LabelText("{0}", "Scrub");

			ImGui.Spacing();
			if (slot == TimelineSlot.Lips) continue;

			ImGui.Separator();
			ImGui.Spacing();
		}
	}
	
	private bool DrawAnimationSelect(GameAnimation anim, bool isFocus) {
		var height = CalcItemHeight();
		var space = ImGui.GetStyle().ItemInnerSpacing.X;
		
		var cursor = ImGui.GetCursorPosX();
		
		var result = ImGui.Button(string.Empty, new Vector2(ImGui.GetContentRegionAvail().X, height));
		
		ImGui.SameLine(cursor, height + space);
		ImGui.Text(anim.Name);
		
		ImGui.SameLine(cursor, height + space);
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight());
		using (var _ = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text).SetAlpha(0xAF)))
			ImGui.Text($"{anim.Slot}");
		
		ImGui.SameLine(cursor);

		var size = new Vector2(height, height);
		if (anim.Icon != 0 && this._tex.TryGetFromGameIcon((uint)anim.Icon, out var icon)) {
			ImGui.Image(icon.GetWrapOrEmpty().Handle, size);
		} else {
			ImGui.Dummy(size);
		}
		
		return result;
	}

	private void OpenAnimationPopup(TimelineSlot? slot = null) {
		var isFilterSlot = slot != null;
		this._animFilter.SlotFilterActive = isFilterSlot;
		if (isFilterSlot) this._animFilter.Slot = slot!.Value;
		this._openAnimList = true;
	}

	private static bool AnimSearchPredicate(GameAnimation anim, string query)
		=> anim.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase);

	private class AnimationFilter : IFilterProvider<GameAnimation> {
		private enum AnimType {
			Action,
			Emote,
			Expression,
			RawTimeline
		}

		private AnimType Type = AnimType.Action;

		public bool SlotFilterActive;
		public TimelineSlot Slot = TimelineSlot.FullBody;
		
		public bool DrawOptions() {
			var update = false;
			
			var values = Enum.GetValues<AnimType>();
			var excludes = this.GetExcludes();
			for (var i = 0; i < values.Length; i++) {
				// if we got an excludes list, skip any radio buttons irrelevant to the picked timeline slot
				if (excludes.Contains(i)) {
					if (this.Type == values[i])
						this.Type = this.BestDefaultTypeForExcludes(excludes);
					continue;
				}
				if (excludes.Count > 0 || (i % 3) != 0) ImGui.SameLine();

				var value = values[i];
				if (ImGui.RadioButton($"{value}", this.Type == value)) {
					this.Type = value;
					update = true;
				}
			}
			
			ImGui.Spacing();
			return update;
		}
		
		public bool Filter(GameAnimation item) {
			return (!this.SlotFilterActive || this.Slot == item.Slot) && item switch {
				ActionAnimation => this.Type == AnimType.Action,
				EmoteAnimation emote => this.Type == (emote.IsExpression ? AnimType.Expression: AnimType.Emote),
				TimelineAnimation => this.Type == AnimType.RawTimeline,
				_ => false
			};
		}

		private List<int> GetExcludes() {
			// conditionally exclude radio buttons based on availability for selected slotfilter
			if (!this.SlotFilterActive) return new List<int>();

			if (this.Slot == TimelineSlot.FullBody || this.Slot == TimelineSlot.UpperBody)
				return new List<int> {(int)AnimType.Expression};
			else if (this.Slot == TimelineSlot.Expression)
				return new List<int> {(int)AnimType.Action, (int)AnimType.Emote};
			else if (this.Slot == TimelineSlot.Additive)
				return new List<int> {(int)AnimType.Action, (int)AnimType.Expression};
			else if (this.Slot == TimelineSlot.Lips)
				return new List<int> {(int)AnimType.Action, (int)AnimType.Emote, (int)AnimType.Expression};

			return new List<int>();
		}

		private AnimType BestDefaultTypeForExcludes(List<int> excludes) {
			// if we have to exclude buttons, return a new default in order of usefulness
			if (!excludes.Contains((int)AnimType.Emote))
				return AnimType.Emote;
			else if (!excludes.Contains((int)AnimType.Action))
				return AnimType.Action;
			else if (!excludes.Contains((int)AnimType.Expression))
				return AnimType.Expression;
			return AnimType.RawTimeline;
		}
	}
	
	// Wrappers

	private void PlayTimeline(uint id) {
		this.Editor.PlayTimeline(id);
		if (this.ForceLoop) this.Editor.SetForceTimeline((ushort)id);
	}

	private void ResetTimeline() {
		this.Editor.PlayTimeline(3);
		this.Editor.SetForceTimeline(0);
	}
}
