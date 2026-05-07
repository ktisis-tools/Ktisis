using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

using Ktisis.Interface.Components.Environment;
using Ktisis.Interface.Components.Environment.Editors;
using Ktisis.Interface.Windows.Editors;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Types;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Windows.ToolbarModules;

public class Env : EnvWindow {

	public Env(ISceneManager scene, IEnvModule module, WeatherSelect weatherSelect, SkyEditor sky, LightingEditor lighting, FogEditor fog, RainEditor rain, ParticlesEditor dust, StarsEditor stars, WindEditor wind, WaterEditor water, HousingEditor housingEditor) : base(scene, module, weatherSelect, sky, lighting, fog, rain, dust, stars, wind, water, housingEditor)
	{
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
	private  unsafe void DrawSideBar(EnvManagerEx* env) {
		var avail = new Vector2(400, 400);
		avail.X *= 0.35f;
		using var _frame = ImRaii.Child("##EnvWeather", avail);
		
		this.DrawWeatherTimeControls(env, avail.X);
		this.DrawAdvancedList();
	}
}
