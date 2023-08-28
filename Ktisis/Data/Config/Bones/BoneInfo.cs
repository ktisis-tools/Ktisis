namespace Ktisis.Data.Config.Bones; 

public class BoneInfo {
	public readonly string Name;

	public int? SortPriority;

	public BoneInfo(string name) => this.Name = name;
}