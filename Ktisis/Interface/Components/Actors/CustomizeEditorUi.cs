using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Characters.Types;
using Ktisis.Interface.Components.Actors.Data;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Actors;

[Transient]
public class CustomizeEditorUi {
	private readonly IDataManager _data;
	private readonly ITextureProvider _tex;

	private readonly MakeTypeData _makeTypeData = new();
	
	public ICustomizeEditor Editor { set; private get; } = null!;
	
	public CustomizeEditorUi(
		IDataManager data,
		ITextureProvider tex
	) {
		this._data = data;
		this._tex = tex;
	}
	
	// Setup

	private bool _isSetup;
	
	public void Setup() {
		if (this._isSetup) return;
		this._isSetup = true;
		this._makeTypeData.Build(this._data).ContinueWith(task => {
			if (task.Exception != null)
				Ktisis.Log.Error($"Failed to build customize data:\n{task.Exception}");
		});
	}
	
	// Draw
	
	public void Draw(ActorEntity actor) {
		var tribe = actor.GetTribe();
		var gender = actor.GetGender();
		
		ImGui.Text($"{tribe} {gender}");
		
		var data = this._makeTypeData.GetData(tribe, gender);
		if (data == null) return;

		var face = data.GetFeature(CustomizeIndex.FaceType)!;
		for (var i = 0; i < face.Params.Length; i++) {
			var icon = this._tex.GetIcon(face.Params[i].Graphic)!;
			if (i > 0) ImGui.SameLine();
			ImGui.Image(icon.ImGuiHandle, new Vector2(64, 64));
		}
		
		ImGui.Spacing();
		
		this.DrawSlider("Height", CustomizeIndex.Height, actor);
		this.DrawSliderFeat(CustomizeIndex.BustSize, data, actor);
		this.DrawSliderFeat(CustomizeIndex.RaceFeatureSize, data, actor);
	}

	private void DrawSlider(string label, CustomizeIndex index, ActorEntity actor) {
		var intValue = (int)this.Editor.GetCustomization(actor, index);
		if (ImGui.SliderInt(label, ref intValue, 0, 100))
			this.Editor.SetCustomization(actor, index, (byte)intValue);
	}

	private void DrawSliderFeat(string label, CustomizeIndex index, MakeTypeRace data, ActorEntity actor) {
		if (!data.HasFeature(index)) return;
		this.DrawSlider(label, index, actor);
	}

	private void DrawSliderFeat(CustomizeIndex index, MakeTypeRace data, ActorEntity actor) {
		var feat = data.GetFeature(index);
		if (feat == null) return;
		this.DrawSlider(feat.Name, index, actor);
	}
}
