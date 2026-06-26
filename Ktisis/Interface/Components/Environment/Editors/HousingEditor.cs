using Dalamud.Bindings.ImGui;
using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Services.Data;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class HousingEditor(HousingDataService housingDataService) : EditorBase
{
    public override string Name => Ktisis.Locale.Translate("env_edit.housing.title");

    public override bool IsActivated(EnvOverride flags)
        => flags.HasFlag(EnvOverride.Housing) && housingDataService.IsInHousing;

    public override void Draw(IEnvModule module, ref EnvState state)
    {
        if (this.DrawToggleCheckbox(Ktisis.Locale.Translate("env_edit.enable"), EnvOverride.Housing, module))
        {
            
            
            if (!IsActivated(module.Override))
            {
                housingDataService.ResetLighting();
                housingDataService.ResetSSAO();
            }
        }
        
        using var _ = this.Disable(module);
        
		
        if (float.IsNaN(housingDataService.IndoorLight))
        {
            ImGui.Text(Ktisis.Locale.Translate("env_edit.housing.unavailable"));
            return;
        }
			
        float currentLight = housingDataService.IndoorLight;
        if (ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.housing.brightness"), ref currentLight, 0.0f, 1.0f))
        {
            housingDataService.IndoorLight = currentLight;
        }

        bool ssaoValue = housingDataService.SSAOEnabled;
        if (ImGui.Checkbox(Ktisis.Locale.Translate("env_edit.housing.ssao"), ref ssaoValue))
        {
            housingDataService.SSAOEnabled = ssaoValue;
        }
    }
}
