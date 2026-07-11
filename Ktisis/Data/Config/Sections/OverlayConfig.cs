namespace Ktisis.Data.Config.Sections;

public class OverlayConfig {
	public bool Visible = true;
	public bool BulkVisOverride = false;

	public bool DrawLines = true;
	public bool DrawLinesGizmo = true;
	public bool DrawDotsGizmo = true;

	public bool DimOverlayForInactiveActors = false;
	public bool PresetsOnActiveActor = false;
	public ActiveState ActiveStateType = ActiveState.Target;
	public float InactiveOpacity = 0.5f;

	public float DotRadius = 7.0f;
	public float LineThickness = 2.0f;
	public float LineOpacity = 0.95f;
	public float LineOpacityUsing = 0.15f;
	
	public bool DrawReferenceTitle = true;
}

public enum ActiveState {
	Target,
	Selection,
	Both
}
