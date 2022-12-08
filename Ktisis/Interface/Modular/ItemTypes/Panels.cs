using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interface.Windows.Workspace;

namespace Ktisis.Interface.Modular.ItemTypes.Panel {
	public class ActorList : BasePannel {
		public ActorList() : base() => LocaleHandle = "Actor List";
		public override void Draw() => ActorsList.Draw();
	}
	public class ActorListHorizontal : BasePannel {
		public ActorListHorizontal() : base() => LocaleHandle = "Actor List";
		public override void Draw() => ActorsList.Draw(true);
	}
	public class ControlButtonsExtra : BasePannel {
		public override void Draw() => ControlButtons.DrawExtra();
	}
	public class SettingsButton : BasePannel {
		public override void Draw() => ControlButtons.DrawSettings(0);
	}
	public class HandleEmpty : BasePannel {
		public override void Draw() => ImGui.Text("       ");
	}
	public class GizmoOperations : BasePannel {
		public override void Draw() => ControlButtons.DrawGizmoOperations();
	}
	public class GposeTextIndicator : BasePannel {
		public override void Draw() => Workspace.DrawGposeIndicator();
	}
	public class PoseSwitch : BasePannel {
		public override void Draw() => ControlButtons.DrawPoseSwitch();
	}
	public class SelectInfo : BasePannel {
		public override void Draw() => Workspace.SelectInfo();
	}
	public class EditActorButton : BasePannel {
		public override void Draw() => EditActor.DrawButton();
	}
	public class AnimationControls : BasePannel {
		public AnimationControls() : base() => LocaleHandle = "Animation Control";

		public override void Draw() => Components.AnimationControls.Draw();
	}
	public class GazeControl : BasePannel {
		public GazeControl() : base() => LocaleHandle = "Gaze Control";

		public override void Draw() => EditGaze.DrawWithHint();
	}
	public class ParentingCheckbox : BasePannel {
		public override void Draw() => ControlButtons.DrawParentingCheckbox();
	}
	public class TransformTable : BasePannel {
		public override void Draw() => Workspace.TransformTable();
	}
	public class CategoryVisibility : BasePannel {
		public CategoryVisibility() : base() => LocaleHandle = "Bone Categories";
		public override void Draw() => Categories.DrawToggleListWithHint();
	}
	public class BoneTree : BasePannel {
		public BoneTree() : base() => LocaleHandle = "Bone List";
		public override void Draw() => Components.BoneTree.Draw();
	}
	public class ImportExport : BasePannel {
		public ImportExport() : base() => LocaleHandle = "Import & Export";
		public override void Draw() => Workspace.DrawImportExport();
	}
	public class Advanced : BasePannel {
		public Advanced() : base() => LocaleHandle = "Advanced";
		public override void Draw() => Workspace.DrawAdvanced();
	}

}
