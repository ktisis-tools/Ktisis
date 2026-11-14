using System;
using System.IO;

namespace Ktisis.Data.Config.Sections;

public class PoseViewConfig {
	// Pose View

    public string? BodyPath;
    public string? ArmorPath;
    public string? EarsPath;
    public string? FacePath;
    public string? HandsPath;
    public string? LipsPath;
    public string? MouthPath;
    public string? TailPath;
	public string? ToesPath;
	public string? CustomBodyPath;
	public string PenisPath;

    // helpers
    public string? CustomPathFor(string viewName) {
        // should switch against each 'View name=' in Views.xml (important if we expand in future!)
        return viewName switch {
            "Body" => this.BodyPath,
            "Face" => this.FacePath,
            "Lips" => this.LipsPath,
            "Mouth" => this.MouthPath,
            "Hands" => this.HandsPath,
            "Tail" => this.TailPath,
            "Armor" => this.ArmorPath,
            "Ears" => this.EarsPath,
			"Feet" => this.ToesPath,
			"HandsEx" => this.HandsPath,
			"CustomBody" => this.CustomBodyPath,
			"Penis" => this.PenisPath
        };
    }
}
