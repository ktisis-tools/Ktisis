using System;
using System.Linq;
using System.Reflection;

using Ktisis.Camera;
using Ktisis.Structs.FFXIV;

namespace Ktisis.History {
	public enum CameraEvent {
		None,
		CreateCamera,
		CameraValue,
		EditValue,
		FreecamValue
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

		internal CameraHistory ResolveStartValue(object? setTo = null) {
			StartValue = setTo ?? GetValue();
			return this;
		}

		internal CameraHistory ResolveEndValue(object? setTo = null) {
			EndValue = setTo ?? GetValue();
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
		
		// Values
		
		private object? GetValue() => GetField()?.GetValue(GetTargetObject());
		private unsafe void SetValue(object val) {
			if (!Subject.IsValid()) return;
			
			var target = GetTargetObject();
			GetField()?.SetValue(target, val);
			if (Event == CameraEvent.CameraValue)
				*Subject.AsGPoseCamera() = (GPoseCamera)target;
		}

		private unsafe object GetTargetObject() => Event switch {
			CameraEvent.CameraValue => *Subject.AsGPoseCamera(),
			CameraEvent.EditValue => Subject.CameraEdit,
			CameraEvent.FreecamValue => Subject.WorkCamera!,
			_ => throw new Exception("Bad CameraEvent?")
		};
		
		private unsafe FieldInfo? GetField() {
			if (!Subject.IsValid()) return null;
			return GetTargetObject()
				.GetType()
				.GetFields()
				.FirstOrDefault(f => f?.Name == Property, null);
		}

		// Handlers
		
		public override void Update(bool undo) {
			switch (Event) {
				case CameraEvent.FreecamValue:
					if (!Subject.IsValid() || Subject.WorkCamera == null) {
						// No op - trigger next undo
						IsNoop = true;
						break;
					}
					goto vals;
				case CameraEvent.CameraValue or CameraEvent.EditValue:
					vals: HandleValues(undo);
					break;
				case CameraEvent.CreateCamera:
					HandleCreate(undo);
					break;
			}
		}

		private unsafe void HandleValues(bool undo) {
			if (undo)
				SetValue(StartValue!);
			else
				SetValue(EndValue!);
		}

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