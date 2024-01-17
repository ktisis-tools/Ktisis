using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Scene.Decor;

namespace Ktisis.Scene.Entities.World;

public class WorldEntity(ISceneManager scene) : SceneEntity(scene), ITransform, IVisibility {
	public nint Address { get; set; }
	
	public bool Visible { get; set; }
	
	public unsafe virtual Object* GetObject() => (Object*)this.Address;
	
	public unsafe virtual bool IsObjectValid => this.GetObject() != null;

	public virtual void Setup() {
		this.Clear();
	}
	
	// Update handler

	public override void Update() {
		if (!this.IsObjectValid) return;
		this.UpdateChildren();
		base.Update();
	}
	
	private unsafe void UpdateChildren() {
		var ptr = this.GetObject();
		if (ptr == null) return;
		
		var objects = new List<nint>();

		var child = ptr->ChildObject;
		var cursor = child;
		while (cursor != null) {
			objects.Add((nint)cursor);
			cursor = cursor->NextSiblingObject;
			if (cursor == child) break;
		}
		
		foreach (var entity in this.Children.Where(x => x is WorldEntity).Cast<WorldEntity>().ToList()) {
			if (objects.Contains(entity.Address))
				objects.Remove(entity.Address);
			else
				entity.Remove();
		}

		foreach (var address in objects)
			this.CreateObjectEntity((Object*)address);
	}

	private unsafe void CreateObjectEntity(Object* ptr) {
		Ktisis.Log.Verbose($"Creating object entity for {(nint)ptr:X}");
		this.Scene.Factory.CreateObject()
			.SetAddress(ptr)
			.Add(this);
	}
	
	// Transform

	public unsafe Transform? GetTransform() {
		var ptr = this.GetObject();
		if (ptr == null) return null;
		return new Transform(
			ptr->Position,
			ptr->Rotation,
			ptr->Scale
		);
	}
	
	public unsafe virtual void SetTransform(Transform trans) {
		var ptr = this.GetObject();
		if (ptr == null) return;
		ptr->Position = trans.Position;
		ptr->Rotation = trans.Rotation;
		ptr->Scale = trans.Scale;
	}
}
