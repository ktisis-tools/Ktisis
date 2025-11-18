using Ktisis.Data.Config.Sections;

namespace Ktisis.Editor.Transforms;

public enum MirrorMode {
	Parallel = 0, // standard transform
	Inverse = 1, // v0.3 MirrorRotation
	Reflect = 2 // v0.2 RotationMirrorX
}

public record class TransformSetup {
	public MirrorMode MirrorRotation = MirrorMode.Parallel;
	public bool ParentBones = true;
	public bool RelativeBones = true;

	public void Configure(GizmoConfig cfg) {
		this.MirrorRotation = cfg.MirrorRotation;
		this.ParentBones = cfg.ParentBones;
		this.RelativeBones = cfg.RelativeBones;
	}
}
