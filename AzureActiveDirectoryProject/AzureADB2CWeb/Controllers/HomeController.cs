using AzureADB2CWeb.Helpers;
using AzureADB2CWeb.Models;
using AzureADB2CWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AzureADB2CWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;

        public HomeController(ILogger<HomeController> logger, 
            IHttpClientFactory httpClientFactory,
            IUserService userService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
        }

        public IActionResult Index()
        {
            if(User.Identity.IsAuthenticated)
            {
                var b2cObjectId = ((ClaimsIdentity)HttpContext.User.Identity)
                    .FindFirst(ClaimTypes.NameIdentifier).Value;
                var user = _userService.GetById(b2cObjectId);

                if(user == null || string.IsNullOrWhiteSpace(user.B2CObjectId))
                {
                    var role = ((ClaimsIdentity)HttpContext.User.Identity)
                        .FindFirst("extension_UserRole").Value;

                    var email = ((ClaimsIdentity)HttpContext.User.Identity)
                        .FindFirst("emails").Value;

                    user = new()
                    {
                        B2CObjectId = b2cObjectId,
                        Email = email,
                        UserRole = role
                    };

                    _userService.Create(user);
                }
            }

            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Permission("homeowner")]
        public IActionResult HomeOwner()
        {
            return View();
        }

        [Permission("contractor")]
        public IActionResult Contractor()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult SignIn()
        {
            var scheme = OpenIdConnectDefaults.AuthenticationScheme;
            var redirectUrl = Url.ActionContext.HttpContext.Request.Scheme
                + "://" + Url.ActionContext.HttpContext.Request.Host;

            return Challenge(new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            }, scheme);
        }

        public IActionResult EditProfile()
        {
            var scheme = OpenIdConnectDefaults.AuthenticationScheme;
            var redirectUrl = Url.ActionContext.HttpContext.Request.Scheme
                + "://" + Url.ActionContext.HttpContext.Request.Host;

            return Challenge(new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            }, scheme);
        }

        public IActionResult SignOut()
        {
            var scheme = OpenIdConnectDefaults.AuthenticationScheme;
            return SignOut(new AuthenticationProperties(), CookieAuthenticationDefaults.AuthenticationScheme, scheme);
        }

        public async Task<IActionResult> APICall()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://localhost:44370/weatherforecast");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue
                (JwtBearerDefaults.AuthenticationScheme, accessToken);

            var response = await client.SendAsync(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {

            }

            return Content(await response.Content.ReadAsStringAsync());

        }
    }
}
