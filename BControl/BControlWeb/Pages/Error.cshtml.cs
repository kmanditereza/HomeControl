﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Error.cshtml.cs" company="DTV-Online">
//   Copyright(c) 2019 Dr. Peter Trimmel. All rights reserved.
// </copyright>
// <license>
// Licensed under the MIT license. See the LICENSE file in the project root for more information.
// </license>
// --------------------------------------------------------------------------------------------------------------------
namespace BControlWeb.Pages
{
    #region Using Directives

    using System.Diagnostics;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    #endregion

    /// <summary>
    /// Default ASP.NET core error handling page.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorModel : PageModel
    {
        #region Public Properties

        /// <summary>
        /// The current request ID.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Flag indicating to show the current request ID.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the state needed for the page.
        /// </summary>
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }

        #endregion
    }
}
