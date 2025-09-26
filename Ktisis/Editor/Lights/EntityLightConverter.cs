using System;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Data.Files;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Editor.Lights;

public class EntityLightConverter {
    private readonly LightEntity _light;

    public EntityLightConverter(LightEntity light) => this._light = light;

    public void Apply(LightFile file) {
        
    }

    public LightFile Save() {
		var file = new LightFile { Nickname = this._light.Name };
        return file;
    }
}
