using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Interface.Components.Environment;
using Ktisis.Interface.Components.Environment.Editors;
using Ktisis.Interface.Types;
using Ktisis.Interface.Widgets.Environment;
using Ktisis.Scene;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Windows.Editors;

public class EnvWindow : KtisisWindow {
	private enum EnvEditorTab {
		None,
		Sky,
		Light,
		Fog,
		Rain,
		Particles,
		Stars,
		Wind
	}
	
	private readonly ISceneManager _scene;
	private readonly IEnvModule _module;

	private readonly WeatherSelect _weatherSelect;
	
	private EnvEditorTab Current = EnvEditorTab.None;
	private readonly Dictionary<EnvEditorTab, EditorBase> _editors = new();
	
	public EnvWindow(
		ISceneManager scene,
		IEnvModule module,
		WeatherSelect weatherSelect,
		SkyEditor sky,
		LightingEditor lighting,
		FogEditor fog,
		RainEditor rain,
		ParticlesEditor dust,
		StarsEditor stars,
		WindEditor wind
	) : base(
		"Environment Editor"
	) {
		this._scene = scene;
		this._module = module;
		this._weatherSelect = weatherSelect;
		this.Setup(EnvEditorTab.Sky, sky)
			.Setup(EnvEditorTab.Light, lighting)
			.Setup(EnvEditorTab.Fog, fog)
			.Setup(EnvEditorTab.Rain, rain)
			.Setup(EnvEditorTab.Particles, dust)
			.Setup(EnvEditorTab.Stars, stars)
			.Setup(EnvEditorTab.Wind, wind);
	}

	private EnvWindow Setup(EnvEditorTab id, EditorBase editor) {
		this._editors.Add(id, editor);
		return this;
	}
	
	// Draw UI

	public override void PreOpenCheck() {
		if (this._scene.IsValid && this._module.IsInit) return;
		Ktisis.Log.Verbose("State for env editor is stale, closing...");
		this.Close();
	}
	
	public override void PreDraw() {
		base.PreDraw();
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(400, 300),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
	}

	public unsafe override void Draw() {
		var env = EnvManagerEx.Instance();
		if (env == null) return;
		
		this.DrawSideBar(env);
		if (this.Current != 0) {
			var style = ImGui.GetStyle();
			ImGui.SameLine(0, (style.ItemSpacing + style.FramePadding / 2).X);
			this.DrawAdvancedEditor(env);
		}
	}
	
	// Sidebar

	private unsafe void DrawSideBar(EnvManagerEx* env) {
		var avail = ImGui.GetContentRegionAvail();
		avail.X *= 0.35f;
		using var _frame = ImRaii.Child("##EnvWeather", avail);
		
		this.DrawWeatherTimeControls(env, avail.X);
		this.DrawAdvancedList();
	}

	private unsafe void DrawWeatherTimeControls(EnvManagerEx* env, float width) {
		//var spacing = ImGui.GetStyle().ItemSpacing.X;
		
		//Icons.DrawIcon(FontAwesomeIcon.Sun);
		//ImGui.SameLine();
		ImGui.Text("Weather");
		
		if (this._weatherSelect.Draw(env, out var newWeather) && newWeather != null) {
			var id = (byte)newWeather.RowId;
			this._module.Weather = id;
			env->_base.ActiveWeather = id;
		}
		
		ImGui.Spacing();

		var isLocked = this._module.Override.HasFlag(EnvOverride.TimeWeather);
		if (Buttons.IconButton(isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock)) {
			this._module.Weather = env->_base.ActiveWeather;
			this._module.Time = env->_base.DayTimeSeconds;
			this._module.Day = DayTimeControls.CalculateDay(env);
			this._module.Override ^= EnvOverride.TimeWeather;
		}
		ImGui.SameLine();
		ImGui.Text("Time and Day");

		using var _disable = ImRaii.Disabled(!isLocked);
		
		if (DayTimeControls.DrawTime(env, out var time))
			this._module.Time = time;

		ImGui.SetNextItemWidth(width);
		if (DayTimeControls.DrawDay(env, out var day))
			this._module.Day = day;
	}

	private void DrawAdvancedList() {
		//Icons.DrawIcon(FontAwesomeIcon.Cog);
		//ImGui.SameLine();
		ImGui.Text("Advanced Editing");
		
		var size = ImGui.GetContentRegionAvail();
		size.Y -= ImGui.GetStyle().WindowPadding.Y / 2;
		using var _box = ImRaii.ListBox("##AdvancedOptions", size);
		if (!_box.Success) return;

		foreach (var (id, editor) in this._editors) {
			var isActive = editor.IsActivated(this._module.Override);
			using var _color = ImRaii.PushColor(ImGuiCol.Text, 0x7FFFFFFF, !isActive);
			
			var isCurrent = id == this.Current;
			if (ImGui.Selectable(editor.Name, isCurrent))
				this.Current = !isCurrent ? id : 0;
		}
	}
	
	// Advanced Editor

	private unsafe void DrawAdvancedEditor(EnvManagerEx* env) {
		using var _frame = ImRaii.Child("##AdvancedFrame", ImGui.GetContentRegionAvail());
		if (!_frame.Success) return;

		if (!this._editors.TryGetValue(this.Current, out var editor))
			return;

		ImGui.Text(editor.Name);
		ImGui.Separator();
		ImGui.Spacing();
		editor.Draw(this._module, ref env->EnvState);
	}
}
