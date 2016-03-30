namespace nSwagger
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using EnvDTE;
    using EnvDTE80;

    internal static class VsHelpers
    {
        public static Project GetActiveProject(DTE2 dte)
        {
            try
            {
                var activeSolutionProjects = dte.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                    return activeSolutionProjects.GetValue(0) as Project;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

            return null;
        }

        public static IEnumerable<ProjectItem> GetSelectedItems(DTE2 dte)
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {
                var item = selItem.Object as ProjectItem;

                if (item != null)
                    yield return item;
            }
        }
    }
}