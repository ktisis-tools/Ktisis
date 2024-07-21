using System;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using GLib.Popups;
using GLib.Popups.Decorators;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;
using Ktisis.Editor.Animation.Game;
using Ktisis.Editor.Animation.Types;
using Ktisis.Structs.Actors;

namespace Ktisis.Interface.Components.Chara;

[Transient]
public class AnimationEditorTab {
	private readonly static PoseModeEnum[] Modes = [
		PoseModeEnum.Idle, PoseModeEnum.SitGround, PoseModeEnum.SitChair, PoseModeEnum.Sleeping
	];

	private readonly IDataManager _data;
	private readonly ITextureProvider _tex;
	
	private readonly GameAnimationData _animations = new();

	private readonly AnimationFilter _animFilter = new();
	private readonly PopupList<GameAnimation> _animList;
	
	public IAnimationEditor Editor { set; private get; } = null!;

	public AnimationEditorTab(
		IDataManager data,
		ITextureProvider tex
	) {
		this._data = data;
		this._tex = tex;

		this._animList = new PopupList<GameAnimation>("##AnimEmoteList", this.DrawEmote)
			.WithSearch(EmoteSearchPredicate)
			.WithFilter(this._animFilter);
	}
	
	// Setup

	private bool _isSetup;
	
	public void Setup() {
		if (this._isSetup) return;
		this._isSetup = true;

		this._animations.Build(this._data).ContinueWith(task => {
			if (task.Exception != null)
				Ktisis.Log.Error($"Failed to fetch animations:\n{task.Exception}");
		});
	}
	
	// Draw

	private static float CalcItemHeight() => (ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemInnerSpacing.Y) * 2;

	private ushort Id;
	
	public void Draw() {
		var id = (int)this.Id;
		ImGui.SetNextItemWidth(100);
		if (ImGui.InputInt("##id", ref id))
			this.Id = (ushort)id;
		ImGui.SameLine();
		if (ImGui.Button("Play"))
			this.Editor.SetTimelineId(this.Id);

		if (ImGui.Button("Emote"))
			this._animList.Open();
		
		if (this._animList.Draw(this._animations.GetAll(), this._animations.Count, out var anim, CalcItemHeight()))
			anim!.Apply(this.Editor);

		ImGui.Spacing();

		if (!this.Editor.TryGetModeAndPose(out var mode, out var pose))
			return;

		if (ImGui.BeginCombo("Mode", mode.ToString())) {
			foreach (var modeType in Modes) {
				if (ImGui.Selectable(modeType.ToString(), modeType == mode))
					this.Editor.SetPose(modeType, 0);
			}
			ImGui.EndCombo();
		}
		
		if (ImGui.InputInt("Pose", ref pose)) {
			var count = this.Editor.GetPoseCount(mode);
			pose = pose < 0 ? count - 1 : pose % count;
			this.Editor.SetPose(mode, (byte)pose);
		}

		var isWepDrawn = this.Editor.IsWeaponDrawn;
		if (ImGui.Button(isWepDrawn ? "Sheathe Weapon" : "Draw Weapon"))
			this.Editor.ToggleWeapon();
	}
	
	// Emote popup

	private bool DrawEmote(GameAnimation emote, bool isFocus) {
		var height = CalcItemHeight();
		var space = ImGui.GetStyle().ItemInnerSpacing.X;
		
		var cursor = ImGui.GetCursorPosX();
		ImGui.SetCursorPosX(cursor + ImGui.GetFrameHeight());
		
		var result = ImGui.Button(string.Empty, new Vector2(ImGui.GetContentRegionAvail().X, height));
		
		ImGui.SameLine(cursor, height + space);
		ImGui.Text(emote.Name);
		
		ImGui.SameLine(cursor, height + space);
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight());
		using (var _ = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text).SetAlpha(0xAF)))
			ImGui.Text($"{emote.Slot}");
		
		ImGui.SameLine(cursor);

		var size = new Vector2(height, height);
		if (emote.Icon != 0 && this._tex.TryGetFromGameIcon((uint)emote.Icon, out var icon)) {
			ImGui.Image(icon.GetWrapOrEmpty().ImGuiHandle, size);
		} else {
			ImGui.Dummy(size);
		}
		
		return result;
	}

	private static bool EmoteSearchPredicate(GameAnimation emote, string query)
		=> emote.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase);

	private class AnimationFilter : IFilterProvider<GameAnimation> {
		private enum AnimType {
			Action,
			Emote,
			Expression
		}

		private AnimType Type = AnimType.Action;
		
		public bool DrawOptions() {
			var update = false;
			var isFirst = true;
			foreach (var value in Enum.GetValues<AnimType>()) {
				if (isFirst)
					isFirst = false;
				else
					ImGui.SameLine();
				
				if (ImGui.RadioButton($"{value}", this.Type == value)) {
					this.Type = value;
					update = true;
				}
			}
			ImGui.Spacing();
			return update;
		}
		
		public bool Filter(GameAnimation item) {
			return item switch {
				ActionAnimation => this.Type == AnimType.Action,
				EmoteAnimation emote => this.Type == (emote.IsExpression ? AnimType.Expression: AnimType.Emote),
				_ => false
			};
		}
	}
}
