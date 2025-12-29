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
            "Ears" => this.EarsPath
        };
    }
}
