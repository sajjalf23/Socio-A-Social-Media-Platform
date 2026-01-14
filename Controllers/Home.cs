using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using SocioApp.Services;
using SocioApp.Models;
using Azure;
namespace socio.Controllers
{
    public class HomeController : Controller
    {

        private readonly IAdminService _adminService;
        private readonly IProfileService _profileService;

        private readonly ICommentService _commentService;
        public HomeController(IAdminService adminService, IProfileService profileService, ICommentService c)
        {
            _adminService = adminService;
            _profileService = profileService;
            _commentService = c;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

    }
}