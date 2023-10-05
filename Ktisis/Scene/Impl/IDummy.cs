using Ktisis.Common.Utility;

namespace Ktisis.Scene.Impl; 

public interface IDummy : ITransform {
	protected Transform Transform { get; set; }
	
	public void CalcTransform();
	
	void ITransform.SetTransform(Transform trans) => this.Transform = trans;
}
