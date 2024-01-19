using System;
using System.Linq;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Characters.Make;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Services;
using Ktisis.Structs.Characters;

namespace Ktisis.Interface.Components.Actors;

[Transient]
public class CustomizeEditorUi {
	private readonly IDataManager _data;
	private readonly ITextureProvider _tex;
	private readonly CustomizeDiscoveryService _discovery;

	private readonly MakeTypeData _makeTypeData = new();
	
	public ICustomizeEditor Editor { set; private get; } = null!;
	
	public CustomizeEditorUi(
		IDataManager data,
		ITextureProvider tex,
		CustomizeDiscoveryService discovery
	) {
		this._data = data;
		this._tex = tex;
		this._discovery = discovery;
	}
	
	// Setup

	private bool _isSetup;
	
	public void Setup() {
		if (this._isSetup) return;
		this._isSetup = true;
		this._makeTypeData.Build(this._data, this._discovery).ContinueWith(task => {
			if (task.Exception != null)
				Ktisis.Log.Error($"Failed to build customize data:\n{task.Exception}");
		});
	}
	
	// Draw
	
	public void Draw(ActorEntity actor) {
		var tribe = (Tribe)this.Editor.GetCustomization(actor, CustomizeIndex.Tribe);
		var gender = (Gender)this.Editor.GetCustomization(actor, CustomizeIndex.Gender);
		
		var data = this._makeTypeData.GetData(tribe, gender);
		if (data == null) return;

		this.Draw(actor, data);
	}

	private void Draw(ActorEntity actor, MakeTypeRace data) {
		this.DrawSideFrame(actor, data);
		ImGui.SameLine();
		this.DrawMainFrame(actor, data);
	}
	
	// Side frame

	private void DrawSideFrame(ActorEntity actor, MakeTypeRace data) {
		var size = ImGui.GetContentRegionAvail();
		size.X = MathF.Max(size.X * 0.35f, 235.0f);
		using var _frame = ImRaii.Child("##CustomizeSideFrame", size, true);

		var cX = ImGui.GetCursorPosX();
		this.DrawBodySelect(actor, data.Gender);
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - cX));
		this.DrawTribeSelect(actor, data.Tribe);
		
		ImGui.Spacing();

		this.DrawFeatSlider(CustomizeIndex.Height, actor, data);
		this.DrawFeatSlider(CustomizeIndex.BustSize, actor, data);
		this.DrawFeatSlider(CustomizeIndex.RaceFeatureSize, actor, data);

		ImGui.Spacing();
		
		this.DrawFeatParams(CustomizeIndex.Eyebrows, actor, data);
		this.DrawFeatParams(CustomizeIndex.EyeShape, actor, data);
		this.DrawFeatParams(CustomizeIndex.NoseShape, actor, data);
		this.DrawFeatParams(CustomizeIndex.JawShape, actor, data);
		this.DrawFeatParams(CustomizeIndex.LipStyle, actor, data);

	}
	
	// Body + Tribe selectors

	private void DrawBodySelect(ActorEntity actor, Gender current) {
		var icon = current == Gender.Masculine ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus;
		if (Buttons.IconButton(icon))
			this.Editor.SetCustomization(actor, CustomizeIndex.Gender, (byte)(current == Gender.Feminine ? 0 : 1));
	}

	private void DrawTribeSelect(ActorEntity actor, Tribe current) {
		using var _combo = ImRaii.Combo("Body", current.ToString());
		if (!_combo.Success) return;
		
		foreach (var tribe in Enum.GetValues<Tribe>()) {
			if (ImGui.Selectable(tribe.ToString(), tribe == current)) {
				this.Editor.Prepare()
					.SetCustomization(CustomizeIndex.Tribe, (byte)tribe)
					.SetCustomization(CustomizeIndex.Race, (byte)Math.Floor(((decimal)tribe + 1) / 2))
					.Dispatch(actor);
			}
		}
	}
	
	// Sliders

	private void DrawSlider(string label, CustomizeIndex index, ActorEntity actor) {
		var intValue = (int)this.Editor.GetCustomization(actor, index);
		if (ImGui.SliderInt(label, ref intValue, 0, 100))
			this.Editor.SetCustomization(actor, index, (byte)intValue);
	}

	private void DrawFeatSlider(CustomizeIndex index, ActorEntity actor, MakeTypeRace data) {
		var feat = data.GetFeature(index);
		if (feat == null) return;
		this.DrawSlider(feat.Name, index, actor);
	}
	
	// Params

	private void DrawFeatParams(CustomizeIndex index, ActorEntity actor, MakeTypeRace data) {
		var feat = data.GetFeature(index);
		if (feat == null) return;

		var baseValue = this.Editor.GetCustomization(actor, index);
		var current = (byte)(baseValue & ~0x80);

		var isZeroIndex = feat.Params.FirstOrDefault()?.Value == 0;

		/*var space = ImGui.GetStyle().ItemInnerSpacing.X;
		var width = ImGui.CalcItemWidth() - space;

		ImGui.SetNextItemWidth(width * 0.70f);
		var result = DrawFeatCombo(feat, current, out var newValue);
		
		ImGui.SameLine(0, space);

		ImGui.SetNextItemWidth(width * 0.30f);*/
		var intValue = (int)current;
		if (isZeroIndex) intValue++;
		if (ImGui.InputInt(feat.Name, ref intValue) && intValue >= (isZeroIndex ? 1 : 0)) {
			var newValue = (byte)(isZeroIndex ? --intValue : intValue);
			this.Editor.SetCustomization(actor, index, (byte)(newValue | (baseValue & 0x80)));
		}
	}

	private static bool DrawFeatCombo(MakeTypeFeature feat, byte current, out byte selected) {
		selected = 0xFF;
		
		using var _combo = ImRaii.Combo($"##Combo_{feat.Name}", FormatFeatParam(feat, current));
		if (!_combo.Success) return false;

		var result = false;
		foreach (var param in feat.Params) {
			var select = ImGui.Selectable(FormatFeatParam(feat, param.Value), current == param.Value);
			selected = param.Value;
			result |= select;
		}
		return result;
	}

	private static string FormatFeatParam(MakeTypeFeature feat, byte value) {
		if (feat.Params.FirstOrDefault()?.Value == 0)
			value++;
		return $"{feat.Name} #{value}";
	}
	
	// Main frame

	private void DrawMainFrame(ActorEntity actor, MakeTypeRace data) {
		using var _frame = ImRaii.Child("##CustomizeMainFrame", ImGui.GetContentRegionAvail());
		if (!_frame.Success) return;

		this.DrawFeatIconParams(CustomizeIndex.FaceType, actor, data);
		this.DrawFeatIconParams(CustomizeIndex.HairStyle, actor, data);
		this.DrawFeatIconParams(CustomizeIndex.Facepaint, actor, data);
		this.DrawFeatIconParams(CustomizeIndex.RaceFeatureType, actor, data);
	}
	
	// Icons

	private readonly static Vector2 ButtonSize = new(52, 52);

	private void DrawFeatIconParams(CustomizeIndex index, ActorEntity actor, MakeTypeRace data) {
		var feat = data.GetFeature(index);
		if (feat == null) return;
		
		var baseValue = this.Editor.GetCustomization(actor, index);

		var active = feat.Params.FirstOrDefault(param => param.Value == baseValue);
		this.DrawFeatIconButton($"{baseValue}", active);
		
		var btnHeight = ImGui.GetItemRectSize().Y;

		ImGui.SameLine();
		using var _group = ImRaii.Group();
		
		var padHeight = btnHeight / 2 - (ImGui.GetFrameHeightWithSpacing() + UiBuilder.IconFont.FontSize);
		ImGui.Dummy(Vector2.Zero with { Y = padHeight });
		
		ImGui.Text(feat.Name);

		var intValue = (int)baseValue;
		if (ImGui.InputInt($"##Input_{feat.Index}", ref intValue))
			this.Editor.SetCustomization(actor, index, (byte)intValue);
	}

	private bool DrawFeatIconButton(string fallback, MakeTypeParam? param) {
		using var _col = ImRaii.PushColor(ImGuiCol.Button, 0);
		
		var icon = param != null ? this._tex.GetIcon(param.Graphic) : null;

		bool clicked;
		if (icon != null)
			clicked = ImGui.ImageButton(icon.ImGuiHandle, ButtonSize);
		else
			clicked = ImGui.Button(fallback, ButtonSize + ImGui.GetStyle().FramePadding * 2);
		return clicked;
	}
}
