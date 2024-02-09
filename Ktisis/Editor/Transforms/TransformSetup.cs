using Ktisis.Data.Config.Sections;

namespace Ktisis.Editor.Transforms;

public record class TransformSetup {
	public bool MirrorRotation = false;
	public bool ParentBones = true;
	public bool RelativeBones = true;

	public void Configure(GizmoConfig cfg) {
		this.MirrorRotation = cfg.MirrorRotation;
		this.ParentBones = cfg.ParentBones;
		this.RelativeBones = cfg.RelativeBones;
	}
}
