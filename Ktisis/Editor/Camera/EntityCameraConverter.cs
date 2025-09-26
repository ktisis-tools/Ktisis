using System;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Editor.Camera.Types;
using Ktisis.Data.Files;

namespace Ktitis.Editor.Camera;

public class EntityCameraConverter {
    private readonly EditorCamera _camera;

    public EntityCameraConverter(EditorCamera camera) => this._camera = camera;

    public void Apply(CameraFile file) {
        
    }

    public CameraFile Save() {
		var file = new CameraFile { Nickname = this._camera.Name };
        return file;
    }
}
