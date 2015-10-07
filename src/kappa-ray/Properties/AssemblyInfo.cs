using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Information about this assembly is defined by the following attributes.
// Change them to the values specific to your project.

[assembly: AssemblyTitle ("kappa-ray")]
[assembly: AssemblyDescription ("Kappa Radiation")]
[assembly: AssemblyConfiguration ("")]
[assembly: AssemblyCompany ("")]
[assembly: AssemblyProduct ("kappa-ray")]
[assembly: AssemblyCopyright ("Copyright © 2015")]
[assembly: AssemblyTrademark ("")]
[assembly: AssemblyCulture ("")]
[assembly: Guid("162c55d7-bc4f-482e-b134-d45eab047fb1")]

// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.

// Don't include the patch revision in the AssemblyVersion - as this will break any dependent
// DLLs any time it changes.  Breaking on a minor revision is probably acceptable - it's
// unlikely that there wouldn't be other breaking changes on a minor version change.
[assembly: AssemblyVersion("0.3")]
[assembly: AssemblyFileVersion("0.3.0")]

// Use KSPAssembly to allow other DLLs to make this DLL a dependency in a
// non-hacky way in KSP.  Format is (AssemblyProduct, major, minor), and it
// does not appear to have a hard requirement to match the assembly version.
[assembly: KSPAssembly("kappa-ray", 0, 3)]
