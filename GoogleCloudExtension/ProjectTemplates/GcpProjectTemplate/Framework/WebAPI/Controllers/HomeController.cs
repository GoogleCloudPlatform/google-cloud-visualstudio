using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _safe_project_name_.Controllers
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
