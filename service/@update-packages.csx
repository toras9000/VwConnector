#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly.General, 0.100.0"
#nullable enable
using System.Text.RegularExpressions;
using Kokuban;
using Lestaly;

var settings = new
{
    // Search directory for script files
    TargetDir = ThisSource.RelativeDirectory("./"),

    // Packages and versions to be unified and updated
    Packages = new PackageVersion[]
    {
        new("Lestaly.General",                       "0.100.0"),
        new("Kokuban",                               "0.2.0"),
        new("SmtpServer",                            "11.0.0"),
        new("MimeKit",                               "4.12.0"),
        new("VwConnector",                           "1.34.1-rev.6"),
    },
};

return await Paved.ProceedAsync(async () =>
{
    // Detection regular expression for package reference directives
    var detector = new Regex(@"^\s*#\s*r\s+""\s*nuget\s*:\s*(?<package>[a-zA-Z0-9_\-\.]+)(?:,| )\s*(?<version>.+)\s*""");

    // Dictionary of packages to be updated
    var versions = settings.Packages.ToDictionary(p => p.Name);

    // Search for scripts under the target directory
    foreach (var file in settings.TargetDir.EnumerateFiles("*.csx", SearchOption.AllDirectories))
    {
        WriteLine($"File: {file.RelativePathFrom(settings.TargetDir, ignoreCase: true)}");

        // Read file contents
        var lines = await file.ReadAllLinesAsync();

        // Attempt to update package references
        var detected = false;
        var updated = false;
        for (var i = 0; i < lines.Length; i++)
        {
            // Detecting Package Reference Directives
            var line = lines[i];
            var match = detector.Match(line);
            if (!match.Success) continue;
            detected = true;

            // Determine if the package is eligible for renewal
            var pkgName = match.Groups["package"].Value;
            if (!versions.TryGetValue(pkgName, out var package))
            {
                WriteLine(Chalk.BrightYellow[$"  Skip: {pkgName} - Not update target"]);
                continue;
            }

            // Parse the version number.
            if (!SemanticVersion.TryParse(match.Groups["version"].Value, out var pkgVer))
            {
                WriteLine(Chalk.BrightYellow[$"  Skip: Unable to recognize version number"]);
                continue;
            }

            // Determine if the package version needs to be updated.
            if (pkgVer == package.SemanticVersion)
            {
                WriteLine(Chalk.BrightYellow[$"  Skip: {pkgName} - Already in version"]);
                continue;
            }

            // Create a replacement line for the reference directive
            var newLine = @$"#r ""nuget: {pkgName}, {package.Version}""";
            lines[i] = newLine;

            // set a flag that there is an update
            updated = true;
            WriteLine(Chalk.Green[$"  Update: {pkgName} {pkgVer.Original} -> {package.Version}"]);
        }

        // Write back to file if updates are needed
        if (updated)
        {
            await file.WriteAllLinesAsync(lines);
        }
        else if (!detected)
        {
            WriteLine(Chalk.BrightYellow[$"  Directive not found"]);
        }
    }

});

// Package version information data type
record PackageVersion(string Name, string Version)
{
    public SemanticVersion SemanticVersion { get; } = SemanticVersion.Parse(Version);
}
