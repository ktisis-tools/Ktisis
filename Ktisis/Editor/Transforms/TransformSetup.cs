using Ktisis.Data.Config.Sections;

namespace Ktisis.Editor.Transforms;

public record class TransformSetup {
	public bool MirrorRotation = true;

	public void Configure(GizmoConfig cfg) {
		this.MirrorRotation = cfg.MirrorRotation;
	}
}
