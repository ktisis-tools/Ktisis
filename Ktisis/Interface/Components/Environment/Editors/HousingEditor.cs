using Dalamud.Bindings.ImGui;
using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Services.Data;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class HousingEditor(HousingDataService housingDataService) : EditorBase
{
    public override string Name { get; } = "Housing";

    public override bool IsActivated(EnvOverride flags)
        => flags.HasFlag(EnvOverride.Housing) && housingDataService.IsInHousing;

    public override void Draw(IEnvModule module, ref EnvState state)
    {
        if (this.DrawToggleCheckbox("Enable", EnvOverride.Housing, module))
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
            ImGui.Text("Housing light is not available.");
            return;
        }
			
        float currentLight = housingDataService.IndoorLight;
        if (ImGui.SliderFloat("Brightness", ref currentLight, 0.0f, 1.0f))
        {
            housingDataService.IndoorLight = currentLight;
        }

        bool ssaoValue = housingDataService.SSAOEnabled;
        if (ImGui.Checkbox("SSAO", ref ssaoValue))
        {
            housingDataService.SSAOEnabled = ssaoValue;
        }
    }
}
