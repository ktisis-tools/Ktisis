using Ktisis.Camera;
using Ktisis.Structs.FFXIV;

namespace Ktisis.History {
	public enum CameraEvent {
		None,
		CreateCamera,
		CameraValue,
		EditValue
	}
	
	public class CameraHistory : HistoryItem {
		public CameraEvent Event;

		public KtisisCamera Subject;
		public string? Property;
		public object? StartValue;
		public object? EndValue;
		
		// Factory

		public CameraHistory(CameraEvent @event) => Event = @event;

		public CameraHistory SetSubject(KtisisCamera subject) {
			Subject = subject;
			return this;
		}

		public CameraHistory SetProperty(string prop) {
			Property = prop;
			return this;
		}

		public CameraHistory SetStartValue(object? val) {
			StartValue = val;
			return this;
		}
		
		public CameraHistory SetEndValue(object? val) {
			EndValue = val;
			return this;
		}
		
		// HistoryItem

		public override HistoryItem Clone() {
			var edit = new CameraHistory(Event);
			edit.Property = Property;
			edit.StartValue = StartValue;
			edit.EndValue = EndValue;
			return edit;
		}
		
		public override void Update(bool undo) {
			switch (Event) {
				case CameraEvent.CreateCamera:
					HandleCreate(undo);
					break;
				default:
					break;
			}
		}
		
		// Handlers

		private unsafe void HandleCreate(bool undo) { // Camera creation
			if (undo) {
				if (!CameraService.GetCameraList().Contains(Subject))
					return;

				var ptr = Subject.AsGPoseCamera();
				if (ptr != null) SetEndValue(*ptr);
				
				var revertTo = CameraService.GetCameraByAddress(Subject.ClonedFrom);
				if (revertTo != null)
					CameraService.SetOverride(revertTo);
				else
					CameraService.Reset();

				CameraService.RemoveCamera(Subject);
			} else {
				var cam = CameraService.SpawnCamera();
				cam.CameraEdit = Subject.CameraEdit;

				var ptr = cam.AsGPoseCamera(); 
				if (ptr != null && EndValue is GPoseCamera values)
					*ptr = values;

				CameraService.SetOverride(cam);
				
				SetSubject(cam);
			}
		}
	}
}