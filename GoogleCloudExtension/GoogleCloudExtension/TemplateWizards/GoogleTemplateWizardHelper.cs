using System.Collections.Generic;
using System.IO;

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// Helper methods for all template wizards.
    /// </summary>
    public static class GoogleTemplateWizardHelper
    {
        /// <summary>
        /// Deletes the project and, if is exclusive, the solution folders.
        /// </summary>
        /// <param name="replacements">The Wizard replacements <see cref="Dictionary{TKey,TValue}"/>.</param>
        public static void CleanupDirectories(Dictionary<string, string> replacements)
        {
            if (Directory.Exists(replacements[ReplacementsKeys.DestinationDirectoryKey]))
            {
                Directory.Delete(replacements[ReplacementsKeys.DestinationDirectoryKey], true);
            }
            bool isExclusive;
            if (Directory.Exists(replacements[ReplacementsKeys.SolutionDirectoryKey]) &&
                bool.TryParse(replacements[ReplacementsKeys.ExclusiveProjectKey], out isExclusive) &&
                isExclusive)
            {
                Directory.Delete(replacements[ReplacementsKeys.SolutionDirectoryKey], true);
            }
        }
    }
}