using System;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Editor.Camera.Types;
using Ktisis.Data.Files;

namespace Ktitis.Editor.Camera;

public class EntityCameraConverter {
    private EditorCamera _camera;

    public EntityCameraConverter(EditorCamera camera) => this._camera = camera;

    public unsafe void Apply(CameraFile file) {
		var ptr = this._camera.Camera;

        if (file.IsNoCollide)
            this._camera.Flags |= CameraFlags.NoCollide;
        else
            this._camera.Flags &= ~CameraFlags.NoCollide;

        this._camera.SetOrthographic(file.IsOrthographic);
        this._camera.SetDelimited(file.IsDelimited);
        // this._camera.FixedPosition = file.FixedPosition;
        this._camera.RelativeOffset = file.RelativeOffset;
        this._camera.OrthographicZoom = file.OrthographicZoom;

		if (ptr == null) return;
        ptr->Angle = file.Angle;
        ptr->Pan = file.Pan;
        ptr->Rotation = file.Rotation;
        ptr->Zoom = file.Zoom;
        ptr->Distance = file.Distance;
        ptr->DistanceMin = file.DistanceMin;
        ptr->DistanceMax = file.DistanceMax;
    }

    public CameraFile Save() {
		var file = new CameraFile { Nickname = this._camera.Name };
        this.Write(file);
        return file;
    }

    private unsafe void Write(CameraFile file) {
		var ptr = this._camera.Camera;

        file.IsNoCollide = this._camera.IsNoCollide;
        file.IsOrthographic = this._camera.IsOrthographic;
        file.IsDelimited = this._camera.IsDelimited;
        // file.FixedPosition = this._camera.FixedPosition; TODO: local and world transform saving
        file.RelativeOffset = this._camera.RelativeOffset;
        file.Angle = ptr->Angle; // rads
        file.Pan = ptr->Pan; // rads
        file.Rotation = ptr->Rotation;
        file.Zoom = ptr->Zoom;
        file.Distance = ptr->Distance;
        file.DistanceMin = ptr->DistanceMin;
        file.DistanceMax = ptr->DistanceMax;
        file.OrthographicZoom = this._camera.OrthographicZoom;
    }
}
