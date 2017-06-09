### About Class Section

To Display a section inside Team Explorer

1. Add TeamExplorerSectionAttribute. The attribute takes 3 input parameters.
* First one is a unique GUID. 
* Second one is the Team Explorer panel id. The id can be Connect, Home etc.  
* The thrid one is priority id. The smaller value section stays above the larger value section.

2. Implement ITeamExplorerSection interface
* The interface extends INotifyPropertyChanged and IDisposable interface. You can check detail at [MSDN](https://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.controls.iteamexplorersection%28v=vs.120%29.aspx). 
* Implement SectionContent that returns a WPF XAML UserControl. This is the content to be displayed as the "section" inside Team Explorer panel.
* The Title property is a string displayed as title of the section

MEF that instantiates the class

1. TeamExplorerSectionAttribute extends [ExportAttribute](https://msdn.microsoft.com/en-gb/library/system.componentmodel.composition.exportattribute(v=vs.110).aspx). It is to expose the class for [MEF](https://docs.microsoft.com/en-us/dotnet/framework/mef/index). 
2. Visual Studio creates instances of the class and Team Explorer takes  ITeamExplorerSection interface to display the SectionContent. 
3. ImportingConstructorAttribute is added to the constructor. 
> This is to tell the MEF system to use this constructor to instantiate the class. Otherwise, by default, MEF uses parameterless constuctor. 

4. The Section class takes ISectionView input parameter. 
> MEF is able to find the assembly that exposes ISectionView interface. In the case of this solution, it is class CsrSectionControl. Then MEF creates an object from the class, get the ISectionView interface from the object. MEF sends it to Section class construtor as input to instantiate the Section class.  
> If looking closer to ISectionView interface, it requires ISectionViewModel as input parameter. With the same logic described above, MEF is able to creates ISectionViewModel from class CsrSectionControlViewModel. This is because CsrSectionControlViewModel implements the interface and adds Export(typeof(ISectionViewModel). 

Multiple Visual Studio version support

1. ITeamExplorerSection interface is defined at Microsoft.TeamFoundation.Controls.dll.  
> It's not COM like interface that it is not cross version compatible. VS2015 and VS2017 has different version number. Visual Studio needs to find exactly same version ITeamExplorerSection as Microsoft.TeamFoundation.Controls.dll that is currently used by the in process Team Explorer. 

2. The Google Cloud Tools for Visual Studio extension is built under Visual Studio 2015. One exnteion binary can be installed to either Visual Studio 2015 or Visual Studio 2017.   

3. To solve the version problem, we build two versions of assemblies that exposes ITeamExplorerSection interface.  
> One is built for Visual Studio 2015, which references to VS2015 version of Microsoft.TeamFoundation.Controls.dll. It is project GoogleCloudExtension.TeamExplorerExtension.v14.  
> One is built for Visual Studio 2017, which references to VS2017 version of Microsoft.TeamFoundation.Controls.dll. It is project GoogleCloudExtension.TeamExplorerExtension.v15.  
> The two assemblies both present when extension is installed to either VS2015 or VS2017. It's not at installation time that Visual Studio chooses which aseembly to use.  
> Visual Studio or MEF has the magic to choose the right assembly to use ... 

4. The GoogleCloudExtension.TeamExplorerExtension project
> Both GoogleCloudExtension.TeamExplorerExtension.v15 and GoogleCloudExtension.TeamExplorerExtension.v14 project implement same class name Section, exposes same interface name ITeamExplorerSection. This is required as described above.  
> This creates a name conflicts when the project GoogleCloudExtension needs to reference to the classes.  
> This is why GoogleCloudExtension.TeamExplorerExtension is created. The project defines interfaces (contracts) that GoogleCloudExtension project references to. And both GoogleCloudExtension.TeamExplorerExtension.v14 project and GoogleCloudExtension.TeamExplorerExtension.v15 references to it too.  
> By using this projects structure together with MEF, we achieved dynamic binding.
