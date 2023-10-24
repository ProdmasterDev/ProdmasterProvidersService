using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProdmasterProvidersService.Services;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Catalog;
using ProvidersDomain.ViewModels.Order;
using ProvidersDomain.ViewModels.Specification;
using System;
using System.Data;

namespace ProdmasterProvidersService.Controllers
{
    [Authorize]
    [Route("order")]
    public class OrderController : Controller
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;

        public OrderController(ILogger<OrderController> logger, IOrderService orderService, IUserService userService)
        {
            _logger = logger;
            _orderService = orderService;
            _userService = userService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index([FromQuery] long? id)
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogWarning($"Failed to get user, userName: {User.Identity?.Name};");
                return BadRequest("Failed to get user");
            }

            if(user.Orders != null && user.Orders.Any())
            {
                if (id != null)
                {
                    return RedirectToAction("ViewOrder", "Order", new { id = id.Value });
                }
                return View(new OrderIndexModel { Orders = user.Orders.OrderByDescending(c => c.Date).ToList() });
            }
            return View(new OrderIndexModel { Orders = new List<Order>() });
        }

        

        [HttpGet]
        [Route("ViewOrder")]
        public async Task<IActionResult> ViewOrder(long id)
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogWarning($"Failed to get user, userName: {User.Identity?.Name};");
                return BadRequest("Failed to get user");
            }
            var orderModel = await _orderService.GetOrderModel(user, id);
            

            if(orderModel == null)
            {
                return RedirectToAction("Index", "Order");
            }
            else
            {
                orderModel.Action = nameof(ViewOrder);
                return View(orderModel);
            }
                
        }

        [HttpPost]
        [Route("ViewOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ViewOrder(OrderModel model)
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogWarning($"Failed to get user, userName: {User.Identity?.Name};");
                return BadRequest("Failed to get user");
            }
            
            return await ProcessUserResponse(model, user);
        }

        [AllowAnonymous]
        [HttpGet("ViewOrderAnonymous")]
        public async Task<IActionResult> ViewOrderAnonymous(string token)
        {
            var orderModel = await _orderService.GetOrderModelByToken(token);
            if (orderModel == null)
            {
                return View("OrderError", new OrderError("Заказ не найден!"));
            }
            else
            {
                orderModel.Action = nameof(ViewOrderAnonymous);
                if (orderModel.CreatedAt + TimeSpan.FromDays(1) < DateTime.Now)
                {
                    return View("OrderError", new OrderError("Срок ссылки истек (1 сутки)!"));
                }
                else
                {
                    return View(nameof(ViewOrder), orderModel);
                }
            }
        }

        [AllowAnonymous]
        [HttpPost("ViewOrderAnonymous")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ViewOrderAnonymous(OrderModel model)
        {
            var order = await _orderService.GetOrderByOrderModel(model);
            if (order == null)
                return View("OrderError", new OrderError("Заказ не найден!"));
            var user = order.User;
            if (user == null)
                return View("OrderError", new OrderError("Поставщик не найден!"));

            return await ProcessUserResponse(model, user);
        }

        [NonAction]
        private async Task<IActionResult> ProcessUserResponse(OrderModel model, User user)
        {
            model.DeclineNote = (model.DeclineNote == null || model.UserResponse == OrderState.ConfirmedByProvider) ? string.Empty : model.DeclineNote;

            if (model.OrderState == OrderState.New)
            {
                if (model.UserResponse == OrderState.ConfirmedByProvider)
                {
                    await _orderService.ConfirmOrder(model);
                    var orderModel = await _orderService.GetOrderModel(user, model.Id);
                    return View(nameof(ViewOrder), orderModel);
                }

                if (model.UserResponse == OrderState.DeclinedByProvider)
                {
                    if (model.DeclineNote.Length > 0)
                    {
                        await _orderService.DeclineOrder(model);
                        var orderModel = await _orderService.GetOrderModel(user, model.Id);
                        return View(nameof(ViewOrder), orderModel);
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(model.DeclineNote), "Не заполнена причина отказа!");
                        return View(nameof(ViewOrder), model);
                    }
                }

                ModelState.AddModelError("", "Неизвестная ошибка! Заказ не подтвержден и не отклонен!");
            }
            return View(nameof(ViewOrder), model);
        }

        [NonAction]
        private Task<User> GetUser()
        {
            var userName = User.Identity?.Name;
            return _userService.GetByEmail(userName);
        }
    }
}
