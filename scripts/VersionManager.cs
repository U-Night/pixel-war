using Godot;
using System;

public partial class VersionManager : Node {
    public string GameVersion { get; private set; } = "dev";
    public string BuildNumber { get; private set; } = "preview";
    public string FullVersion { get; private set; } = "";

    public string YearOfLicenseValidity { get; private set; } = "2026";

    public override void _EnterTree() {
        if (ProjectSettings.HasSetting("application/config/version")) {
            GameVersion = ProjectSettings.GetSetting("application/config/version").ToString();
        }

        if (ProjectSettings.HasSetting("application/config/build_number")) {
            BuildNumber = ProjectSettings.GetSetting("application/config/build_number").ToString();
        }

        FullVersion = $"v{GameVersion} (build {BuildNumber})";
    }
}