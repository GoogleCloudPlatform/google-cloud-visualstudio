using log4net;
using System.Web.Mvc;

namespace $safeprojectname$.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(HomeController));

        public ActionResult Index()
        {
            _log.Debug("Home page hit!");
            ViewBag.Title = "Home Page";
            return View();
        }
    }
}
