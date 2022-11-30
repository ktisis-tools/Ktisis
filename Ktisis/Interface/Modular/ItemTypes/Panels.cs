using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Interface.Modular.ItemTypes.BasePanel;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Localization;

namespace Ktisis.Interface.Modular.ItemTypes.BasePanel {
	public class BasePannel : IModularItem {
		public string LocaleHandle { get; set; }
		public BasePannel(string localeHandle = "modularPanel") {
			this.LocaleHandle = localeHandle;
		}

		virtual public string LocaleName() => $"{Locale.GetString(LocaleHandle)}##Modular##Pannel";

		virtual public void Draw() { }
	}

}
namespace Ktisis.Interface.Modular.ItemTypes.Panel {
	public class ActorList : BasePannel {
		public ActorList() => this.LocaleHandle = "Actor List";
		public override void Draw() => ActorsList.Draw();
	}
	public class ControlButtonsExtra : BasePannel {
		public override void Draw() => ControlButtons.DrawExtra();
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
		public AnimationControls() => this.LocaleHandle = "Animation Control";
		public override void Draw() => Components.AnimationControls.Draw();
	}
	public class GazeControl : BasePannel {
		public GazeControl() => this.LocaleHandle = "Gaze Control";
		public override void Draw() => EditGaze.DrawWithHint();
	}
	public class ParentingCheckbox : BasePannel {
		public override void Draw() => ControlButtons.DrawParentingCheckbox();
	}
	public class TransformTable : BasePannel {
		public override void Draw() => Workspace.TransformTable();
	}
	public class CategoryVisibility : BasePannel {
		public CategoryVisibility() => this.LocaleHandle = "Bone Categories";
		public override void Draw() => Categories.DrawToggleListWithHint();
	}
	public class BoneTree : BasePannel {
		public BoneTree() => this.LocaleHandle = "Bone List";
		public override void Draw() => Components.BoneTree.Draw();
	}
	public class ImportExport : BasePannel {
		public ImportExport() => this.LocaleHandle = "Import & Export";
		public override void Draw() => Workspace.DrawImportExport();
	}
	public class Advanced : BasePannel {
		public Advanced() => this.LocaleHandle = "Advanced";
		public override void Draw() => Workspace.DrawAdvanced();
	}

}
