namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// The type of projects supported.
    /// </summary>
    public enum KnownProjectTypes
    {
        /// <summary>
        /// The unknown project.
        /// </summary>
        None,

        /// <summary>
        /// An ASP.NET 4.x app.
        /// </summary>
        WebApplication,

        /// <summary>
        /// An ASP.NET Core 1.0 app
        /// </summary>
        NetCoreWebApplication1_0,

        /// <summary>
        /// An ASP.NET Core 1.1 app
        /// </summary>
        NetCoreWebApplication1_1,
    }
}