using System;

using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;

using Ktisis.Data.Config.Gobos;
using Ktisis.Scene.Entities.World;
using Ktisis.Data.Files;
using Ktisis.Data.Serialization;
using CsLight = FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer.LightType;
using LightType = Ktisis.Data.Files.LightType;

namespace Ktisis.Editor.Lights;


public class EntityLightConverter {
	private readonly GoboSchema _goboSchema;
    private LightEntity _light;



	public EntityLightConverter(LightEntity light) {
		this._light = light;
		this._goboSchema = SchemaReader.ReadGobos();
	}

    public unsafe void Apply(LightFile file) {
		var sceneLight = this._light.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;

		light->LightType = (CsLight)file.LightType;
        this._light.Flags |= LightEntityFlags.Update;
        this._light.Name = file.Nickname;

        light->Flags = file.Flags;
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

		if (file.Gobo != null) {
			var gobo = this._goboSchema.Gobos.Find(gob => gob.Path == file.Gobo);
			if  (gobo != null) this._light.SetGobo(gobo);
		}
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
        file.LightType = (LightType)light->LightType;
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
		file.Gobo = this._light.Gobo?.Path;
	}
}
