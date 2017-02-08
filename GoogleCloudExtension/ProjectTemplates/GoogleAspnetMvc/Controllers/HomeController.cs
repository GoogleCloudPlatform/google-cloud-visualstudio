using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using MySql.Data.MySqlClient;
using $safeprojectname$.Models;

namespace $safeprojectname$.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var dbContext = HttpContext.GetOwinContext().Get<ApplicationDbContext>();

            try
            {
                dbContext.Database.Connection.Open();
                ViewBag.DatabaseError = false;
            }
            catch (MySqlException ex)
            {
                ViewBag.DatabaseError = true;
                ViewBag.DatabaseErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}