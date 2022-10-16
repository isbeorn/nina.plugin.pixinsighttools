using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("PixInsight Tools")]
[assembly: AssemblyDescription("A bundle of tools to interact with PixInsight from N.I.N.A. for the purpose of live stacking")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Stefan Berg")]
[assembly: AssemblyProduct("NINA.Plugins")]
[assembly: AssemblyCopyright("Copyright ©  2021-2022")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("6ac8f0ab-4455-4072-9fb0-734b9e97e3ce")]

[assembly: InternalsVisibleTo("NINA.Plugins.Test")]

//The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

//The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.0")]

//Your plugin homepage - omit if not applicaple
[assembly: AssemblyMetadata("Homepage", "https://www.patreon.com/stefanberg/")]
//The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
//The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
//The repository where your pluggin is hosted
[assembly: AssemblyMetadata("Repository", "https://bitbucket.org/Isbeorn/nina.plugin.pixinsighttools/src/master/")]

[assembly: AssemblyMetadata("ChangelogURL", "https://bitbucket.org/Isbeorn/nina.plugin.pixinsighttools/src/master/PixInsightTools/Changelog.md")]

//Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "PixInsight,Sequencer")]

//The featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://bitbucket.org/Isbeorn/nina.plugin.pixinsighttools/downloads/logo.png")]
//An example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "https://bitbucket.org/Isbeorn/nina.plugin.pixinsighttools/downloads/LiveStackTab.png")]
//An additional example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
[assembly: AssemblyMetadata("LongDescription", @"This plugin provides instructions and capabilities to interact with PixInsight and use the output to display a live stack inside N.I.N.A.

## **This plugin is provided as is. It is tested with various scenarios in mind, however for some use cases it might not work.**
**Use this plugin only on an imaging rig that has enough processing power to interact with PixInsight in parallel to imaging!**
**Furthermore you need long enough exposures for the live stack to process everything in between, otherwise the machine will always be busy catching up.**

## Prerequisites

* PixInsight needs to be installed and up-to-date
* Set up your PixInsight location
* Set a working directory - this folder will be used to store calibration files, temporary files as well as the live stack files
* Adjust the remaining settings on the plugin page to your needs
* Add BIAS, DARK or FLAT master files (optional)

## Sequencer Instructions

* Stack Flats
    + Place this inside a set of instructions that take FLAT frames below the last flat capture instruction
    + While flats are being taken, the instruction will gather these frames and calibrate them in the background. Filter meta data will automatically be considered to separate these.
    + Once the instruction is reached, it will take all flats that have been gathered up to that point and will stack them together
    + After the stack has been created it will be automatically put into the session library of the live stack panel

* Start Live Stack
    + This instruction will start the live stack command in the imaging panel

* Stop Live Stack
    + This instruction will stop the live stack command in the imaging panel

## Live Stacking Panel
* Expander options
    + Inside the expander you can manually add single session flat masters, or alternatively automatically let them be filled by the **Stack Flats** instruction
    + *Note* - Multi Session Flat Masters are not shown here, but will be used when you have set them in the options page
    + Quality Gates can be added to filter out images for the stacks that do not meet certain criteria
    + Furthermore a color combination can be specified when there are at least two different filters for one target available. The color stack will be created after the next frame is stacked.

* Start live stack
    + Once the live stack is started, the plugin will listen for any light frame that is being captured. 
    + Any frame that is gathered will be calibrated and put into a stack. Filters and Target Names are considered automatically. So you can create stacks for multiple filters and also multiple targets.
    + Inside a stack window you can also see the number of already stacked frames as well as specifying to apply an ABE to the image")]