using System;

using Ktisis.Scene.Entities.World;
using Ktisis.Data.Files;

namespace Ktisis.Editor.Lights;

public class EntityLightConverter {
    private LightEntity _light;

    public EntityLightConverter(LightEntity light) => this._light = light;

    public unsafe void Apply(LightFile file) {
		var sceneLight = this._light.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;

        this._light.Flags |= LightEntityFlags.Update;
        this._light.Name = file.Nickname;

        light->Flags = file.Flags;
        light->LightType = file.LightType;
        // light->Transform = file.Transform; TODO
        light->Color.RGB = file.RGB;
        light->Color.Intensity = file.Intensity;
        light->ShadowNear = file.ShadowNear;
        light->ShadowFar = file.ShadowFar;
        light->FalloffType = file.FalloffType;
        light->AreaAngle = file.AreaAngle;
        light->Falloff = file.Falloff;
        light->LightAngle = file.LightAngle;
        light->FalloffAngle = file.FalloffAngle;
        light->Range = file.Range;
        light->CharaShadowRange = file.CharaShadowRange;
    }

    public LightFile Save() {
		var file = new LightFile { Nickname = this._light.Name };
        this.Write(file);
        return file;
    }

    private unsafe void Write(LightFile file) {
		var sceneLight = this._light.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;

        file.Flags = light->Flags;
        file.LightType = light->LightType;
        // file.Transform = light->Transform; TODO: local and world transform saving - scenelight vs renderlight transform?
        file.RGB = light->Color.RGB;
        file.Intensity = light->Color.Intensity;
        file.ShadowNear = light->ShadowNear;
        file.ShadowFar = light->ShadowFar;
        file.FalloffType = light->FalloffType;
        file.AreaAngle = light->AreaAngle;
        file.Falloff = light->Falloff;
        file.LightAngle = light->LightAngle;
        file.FalloffAngle = light->FalloffAngle;
        file.Range = light->Range;
        file.CharaShadowRange = light->CharaShadowRange;
    }
}
