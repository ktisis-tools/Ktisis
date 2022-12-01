using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interface.Windows.Workspace;

namespace Ktisis.Interface.Modular.ItemTypes.Panel {
	public class ActorList : BasePannel {
		public ActorList(ParamsExtra extra) : base(extra, "Actor List") { }
		public override void Draw() => ActorsList.Draw();
	}
	public class ActorListHorizontal : BasePannel {
		public ActorListHorizontal(ParamsExtra extra) : base(extra, "Actor List") { }
		public override void Draw() => ActorsList.Draw(true);
	}
	public class ControlButtonsExtra : BasePannel {
		public ControlButtonsExtra(ParamsExtra extra) : base(extra) { }
		public override void Draw() => ControlButtons.DrawExtra();
	}
	public class SettingsButton : BasePannel {
		public SettingsButton(ParamsExtra extra) : base(extra) { }
		public override void Draw() => ControlButtons.DrawSettings(0);
	}
	public class HandleEmpty : BasePannel {
		public HandleEmpty(ParamsExtra extra) : base(extra) { }
		public override void Draw() => ImGui.Text("       ");
	}
	public class GizmoOperations : BasePannel {
		public GizmoOperations(ParamsExtra extra) : base(extra) { }
		public override void Draw() => ControlButtons.DrawGizmoOperations();
	}
	public class GposeTextIndicator : BasePannel {
		public GposeTextIndicator(ParamsExtra extra) : base(extra) { }
		public override void Draw() => Workspace.DrawGposeIndicator();
	}
	public class PoseSwitch : BasePannel {
		public PoseSwitch(ParamsExtra extra) : base(extra) { }
		public override void Draw() => ControlButtons.DrawPoseSwitch();
	}
	public class SelectInfo : BasePannel {
		public SelectInfo(ParamsExtra extra) : base(extra) { }
		public override void Draw() => Workspace.SelectInfo();
	}
	public class EditActorButton : BasePannel {
		public EditActorButton(ParamsExtra extra) : base(extra) { }
		public override void Draw() => EditActor.DrawButton();
	}
	public class AnimationControls : BasePannel {
		public AnimationControls(ParamsExtra extra) : base(extra, "Animation Control") { }
		public override void Draw() => Components.AnimationControls.Draw();
	}
	public class GazeControl : BasePannel {
		public GazeControl(ParamsExtra extra) : base(extra, "Gaze Control") { }
		public override void Draw() => EditGaze.DrawWithHint();
	}
	public class ParentingCheckbox : BasePannel {
		public ParentingCheckbox(ParamsExtra extra) : base(extra) { }
		public override void Draw() => ControlButtons.DrawParentingCheckbox();
	}
	public class TransformTable : BasePannel {
		public TransformTable(ParamsExtra extra) : base(extra) { }
		public override void Draw() => Workspace.TransformTable();
	}
	public class CategoryVisibility : BasePannel {
		public CategoryVisibility(ParamsExtra extra) : base(extra, "Bone Categories") { }
		public override void Draw() => Categories.DrawToggleListWithHint();
	}
	public class BoneTree : BasePannel {
		public BoneTree(ParamsExtra extra) : base(extra, "Bone List") { }
		public override void Draw() => Components.BoneTree.Draw();
	}
	public class ImportExport : BasePannel {
		public ImportExport(ParamsExtra extra) : base(extra, "Import & Export") { }
		public override void Draw() => Workspace.DrawImportExport();
	}
	public class Advanced : BasePannel {
		public Advanced(ParamsExtra extra) : base(extra, "Advanced") { }
		public override void Draw() => Workspace.DrawAdvanced();
	}

}
