﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using Microsoft.AspNet.Identity.Owin;
using MySql.Data.MySqlClient;
using $safeprojectname$.Models;

namespace $safeprojectname$.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Index()
        {
            VerifyDatabaseConfiguration();

            return View();
        }

        /// <summary>
        /// Default value of the database connection string generated by this ASP.NET application template.
        /// </summary>
        const string ConnectionStringPlaceholder = "Server=[IPv4 IP Address];Database=[Database name];Uid=[User name];Pwd=[User password]";

        /// <summary>
        /// Check the value of the DefaultConnection database connection string.
        /// If the connection string has not been configured, redirect to a page that provides instructions
        /// for configuring this application to use a Cloud SQL database, as well as additional resources. 
        /// </summary>
        void VerifyDatabaseConfiguration()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

            if (connectionString == ConnectionStringPlaceholder)
            {
                Response.Redirect("/Content/GoogleCloudSQLConfiguration.html");
            }
        }
    }
}
