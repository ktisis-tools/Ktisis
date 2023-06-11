using Dalamud.Logging;

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
		
		public string? Property;
		public object? StartValue;
		public object? EndValue;
		
		// Factory

		public CameraHistory(CameraEvent @event) {
			Event = @event;
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
			PluginLog.Information($"Attempting remove {Event}");
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
			if (Property == null) return;
			
			if (undo) {
				var cam = CameraService.GetCameraByName(Property);
				if (cam == null) return;
				
				var ptr = cam.AsGPoseCamera();
				if (ptr != null) SetEndValue(*ptr);
				
				if (StartValue is nint addr && CameraService.GetCameraByAddress(addr) is KtisisCamera revertTo)
					CameraService.SetOverride(revertTo.Address);
				else
					CameraService.Reset();
				
				CameraService.RemoveCamera(cam);
			} else {
				var cam = CameraService.SpawnCamera();
				var ptr = cam.AsGPoseCamera();
				if (ptr != null && EndValue != null)
					*ptr = (GPoseCamera)EndValue;
				var active = Services.Camera->GetActiveCamera();
				if (active != null) SetStartValue((nint)active);
				CameraService.SetOverride(cam.Address);
				SetEndValue(null);
			}
		}
	}
}