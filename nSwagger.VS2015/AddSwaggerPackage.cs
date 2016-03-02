namespace nSwagger.VS2015
{
    using EnvDTE;
    using EnvDTE80;
    using GUI;
    using Microsoft.VisualStudio.Shell;
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(AddSwaggerPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class AddSwaggerPackage : Package
    {
        /// <summary>
        /// AddSwaggerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "6090ec9f-332f-4f70-9694-a6e8e301ef12";

        public static readonly Guid CommandSet = new Guid("7e544b70-c042-43d4-a10d-28252f8b82fd");
        private DTE2 _dte;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            _dte = GetService(typeof(DTE)) as DTE2;
            var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
            {
                return;
            }

            {
                //add command
                var menuCommandID = new CommandID(CommandSet, 0x1020);
                var menuItem = new MenuCommand(AddSwaggerCommand, menuCommandID);
                commandService.AddCommand(menuItem);
            }

            {
                //update command
                var menuCommandID = new CommandID(CommandSet, 0x1030);
                var menuItem = new OleMenuCommand(UpdateSwaggerCommand, menuCommandID);
                menuItem.BeforeQueryStatus += UpdateItem_BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void AddSwaggerCommand(object sender, EventArgs e)
        {
            var project = VsHelpers.GetActiveProject(_dte);
            //todo: figure out if we doing CS or TS based on project type - if possible
            //todo: if we on a folder node, path should be folder
            var uiOptions = new UIOptions
            {
                Overwrite = true
            };

            uiOptions.Target = Path.Combine(Path.GetDirectoryName(project.FullName), "api.cs");
            var window = new MainWindow();
            window.SetUI(uiOptions);
            window.ShowDialog();
        }

        private void UpdateItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = sender as OleMenuCommand;
            button.Visible = false;

            var items = VsHelpers.GetSelectedItems(_dte);
            //todo: check if items > 0, check file extension is nSwagger - if it is, then show update button
            //todo: figure out why 
            System.Diagnostics.Debugger.Break();
        }

        private void UpdateSwaggerCommand(object sender, EventArgs e)
        {
            var project = VsHelpers.GetActiveProject(_dte);
        }
    }
}