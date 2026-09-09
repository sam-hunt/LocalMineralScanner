using System.Reflection;
using System.Runtime.InteropServices;

// Kept explicit (GenerateAssemblyInfo=false in the csproj) so the release flow can bump
// AssemblyVersion/AssemblyFileVersion in lockstep with About.xml's <modVersion> and
// CHANGELOG.md. Four-part X.Y.Z.0, where X.Y.Z is the mod's semantic version.
[assembly: AssemblyTitle("LocalMineralScanner")]
[assembly: AssemblyDescription("Local mineral scanner building for RimWorld")]
[assembly: AssemblyProduct("LocalMineralScanner")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: ComVisible(false)]
[assembly: Guid("49a46a0e-0d82-4ace-8bf4-cc06a52618c3")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
