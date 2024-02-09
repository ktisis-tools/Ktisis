using System;
using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Scene.Decor;

namespace Ktisis.Editor.Posing.Attachment;

public interface IAttachManager : IDisposable {
	public void Attach(IAttachable child, IAttachTarget target);
	public void Detach(IAttachable child);
	public unsafe void Invalidate(Skeleton* parent);
}

public class AttachManager : IAttachManager {
	// Attach handling
	
	public void Attach(IAttachable child, IAttachTarget target) {
		Ktisis.Log.Info($"Attaching {child} {child.GetHashCode():X}");
		if (child.IsValid && target.TryAcceptAttach(child))
			this.Attachments.Add(child);
	}

	public void Detach(IAttachable child) {
		if (!child.IsValid) return;
		
		Ktisis.Log.Info($"Detaching {child} {child.GetHashCode():X}");

		try {
			child.Detach();
		} finally {
			this.Attachments.RemoveWhere(item => item.Equals(child));
		}
	}

	public unsafe void Invalidate(Skeleton* parent) {
		foreach (var item in this.Attachments.Where(x => x.IsValid).ToList()) {
			var attach = item.GetAttach();
			if (attach != null && attach->Parent == parent)
				this.Detach(item);
		}
	}
	
	// State
	
	private readonly HashSet<IAttachable> Attachments = new();
	
	private void Clear() {
		foreach (var item in this.Attachments)
			item.Detach();
		this.Attachments.Clear();
	}
	
	// Disposal
	
	public void Dispose() {
		try {
			this.Clear();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to clear attachments:\n{err}");
		}
		GC.SuppressFinalize(this);
	}
}
