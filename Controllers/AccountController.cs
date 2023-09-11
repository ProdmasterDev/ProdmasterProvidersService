using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using ProvidersDomain.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ProdmasterProvidersService.Controllers
{
    [Route("account")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        
        public AccountController(IUserService userService)
        {
            _userService = userService;
        }    
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetUser();
            var model = await _userService.GetModelFromUser(user);
            return View(model);
        }
        
        [AllowAnonymous]
        [HttpGet("login")]
        public async Task<IActionResult> Login([FromQuery(Name = "ReturnUrl")] string? returnUrl)
        {
            if (await Authorized()) return RedirectToAction(nameof(Index), "Account");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        
        [AllowAnonymous]
        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, [FromQuery(Name = "ReturnUrl")] string? returnUrl)
        {
            if (await Authorized()) return RedirectToAction(nameof(Index), "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    var user = long.TryParse(model.EmailOrINN, out var inn) ? 
                        await _userService.GetByINN(model.EmailOrINN) : 
                        await _userService.GetByEmail(model.EmailOrINN);

                    if (user != null)
                    {
                        if (user.Password == model.Password)
                        {
                            await Authenticate(user.Email);
                            //var returnUrl = HttpContext.Request.Query["ReturnUrl"].ToString();
                            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                                return Redirect(returnUrl);
                            return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", ""));
                        }
                        ModelState.AddModelError(nameof(LoginModel.Password), "Неправильный пароль");
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(LoginModel.EmailOrINN), "Такой пользователь не зарегистрирован!");
                    }
                }
                catch
                {
                    ModelState.AddModelError("", "Не удалось войти");
                }
            }
            return View(model);
        }
        
        [AllowAnonymous]
        [HttpGet("register")]
        public async Task<IActionResult> Register()
        {
            if (await Authorized()) return RedirectToAction(nameof(Index), "Account");

            return View();
        }
        
        [AllowAnonymous]
        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (await Authorized()) return RedirectToAction(nameof(Index), "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    if (!await _userService.UserExists(model))                    
                    {
                        var user = await _userService.Add(model);
                        await Authenticate(user.Email);
                        return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", ""));                        
                    }
                    ModelState.AddModelError("", "Пользователь с таким email или ИНН уже зарегистрирован!");
                    
                }
                catch
                {
                    ModelState.AddModelError("", "Не удалось войти");
                }
            }
            return View(model);
        }
        
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login), nameof(AccountController).Replace("Controller", ""));
        }

        [HttpGet("changePassword")]
        public async Task<IActionResult> ChangePassword()
        {            
            return View();
        }

        [HttpPost("changePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await GetUser();
                    if (user != null)
                    {
                        if (user.Password == model.OldPassword)
                        {
                            user.Password = model.NewPassword;
                            await _userService.UpdateUser(user);
                            return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", ""));                        
                        }
                        ModelState.AddModelError(nameof(model.OldPassword), "Старый пароль введен неверно");
                    }
                }
                catch
                {
                    ModelState.AddModelError("", "Не удалось сменить пароль");
                }
            }
            return View(model);
        }

        [NonAction]
        private async Task Authenticate(string userName)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
            };
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }
        
        [NonAction]
        private Task<User> GetUser()
        {
            var userName = User.Identity?.Name;
            return _userService.GetByEmail(userName);
        }
        
        [NonAction]
        private async Task<bool> Authorized()
        {
            var user = await GetUser();
            return user != null;
        }
    }
}
