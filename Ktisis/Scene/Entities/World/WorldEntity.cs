using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Editor.Strategy;

namespace Ktisis.Scene.Entities.World;

public class WorldEntity : SceneEntity {
	public nint Address { get; set; }
	
	public override bool IsValid => base.IsValid && this.Address != nint.Zero;

	new public EditObject Edit() => (EditObject)this.Strategy;

	public WorldEntity(
		ISceneManager scene
	) : base(scene) {
		this.Strategy = new EditObject(this);
	}

	public unsafe Object* GetObject() => (Object*)this.Address;

	public virtual void Setup() {
		this.Clear();
	}

	public override void Update() {
		if (!this.IsValid) return;
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
}
