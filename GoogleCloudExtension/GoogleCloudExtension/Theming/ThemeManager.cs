using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Theming
{
    public enum VsTheme
    {
        Unknown,
        Light,
        Blue,
        Dark,
    }

    /// <summary>
    /// This class implements a very simple theme manager.
    /// </summary>
    public class ThemeManager
    {
        // IDs of the various VS themes.
        private static readonly Guid s_lightThemeId = new Guid("de3dbbcd-f642-433c-8353-8f1df4370aba");
        private static readonly Guid s_blueThemeId = new Guid("a4d6a176-b948-4b29-8c66-53c97a1ed7d0");
        private static readonly Guid s_darkThemeId = new Guid("1ded0138-47ce-435e-84ef-9ec1f439b749");

        // The mapping of the theme ids to the VsTheme values.
        private static readonly Dictionary<Guid, VsTheme> s_themeIds = new Dictionary<Guid, VsTheme>
        {
            { s_lightThemeId, VsTheme.Light },
            { s_blueThemeId, VsTheme.Blue },
            { s_darkThemeId, VsTheme.Dark },
        };

        // This service is not available directly, this is a common technique to obtain the current theme
        // used in many projects.
        [Guid("0d915b59-2ed7-472a-9de8-9161737ea1c5")]
        private interface SVsColorThemeService
        { }

        /// <summary>
        /// Returns the current known theme to the caller. It will return <see cref="VsTheme.Unknown"/> for custom
        /// themes created by the user.
        /// </summary>
        /// <returns></returns>
        public static VsTheme GetCurrentTheme()
        {
            dynamic themeService = GoogleCloudExtensionPackage.GetGlobalService(typeof(SVsColorThemeService));
            Guid themeId = themeService.CurrentTheme.ThemeId;
            VsTheme result = VsTheme.Unknown;

            s_themeIds.TryGetValue(themeId, out result);
            return result;
        }
    }
}
