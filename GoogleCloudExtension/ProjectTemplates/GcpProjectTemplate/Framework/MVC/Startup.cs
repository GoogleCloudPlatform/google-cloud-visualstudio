using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(_safe_project_name_.Startup))]
namespace _safe_project_name_
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
