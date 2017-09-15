using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SafeProjectName.Startup))]
namespace SafeProjectName
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
