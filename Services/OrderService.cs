using Microsoft.AspNetCore.Http.HttpResults;
using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.ViewModels.Order;
using ProvidersDomain.Models.ApiModels;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Specification;
using Microsoft.AspNetCore.JsonPatch.Internal;
using System.Text;
using System.Runtime.CompilerServices;

namespace ProdmasterProvidersService.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;
        private readonly UserRepository _userRepository;
        private readonly StandartRepository _standartRepository;
        private readonly IUpdateProvidersService _updateProvidersService;
        public OrderService(OrderRepository orderRepository, ProductRepository productRepository, UserRepository userRepository, StandartRepository standartRepository, IUpdateProvidersService updateProvidersService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _standartRepository = standartRepository;
            _updateProvidersService = updateProvidersService;
        }
        public async Task<Order> CreateOrder(OrderApiModel order)
        {
            if (order != null)
            {
                if (await _orderRepository.First(o => o.JrId == order.JrId 
                    && o.OrderState != OrderState.DeclinedByRecipient
                    && o.OrderState != OrderState.ApprovedDeclineByProvider) == null)
                {
                    if (order.ProductPart != null)
                    {
                        var orderProductPart = new List<OrderProductPart>();
                        foreach (var opp in order.ProductPart)
                        {
                            var orderProduct = await _productRepository.First(p => p.DisanId == opp.DisanId);
                            if (orderProduct != null)
                            {
                                orderProductPart.Add(new OrderProductPart()
                                {
                                    Product = orderProduct,
                                    ProductId = orderProduct.Id,
                                    Price = opp.Price,
                                    Quantity = opp.Quantity,
                                });
                            }
                            else
                            {
                                var standart = await _standartRepository.First(s => s.StockId == opp.StockId);
                                if(standart != null)
                                {
                                    var product = new Product();
                                    product.DisanId = await GenerateFakeDisanId();
                                    product.Name = standart.Name;
                                    product.Quantity = opp.Quantity;
                                    orderProduct = await _productRepository.Add(product);
                                    orderProductPart.Add(new OrderProductPart()
                                    {
                                        Product = orderProduct,
                                        ProductId = orderProduct.Id,
                                        Price = opp.Price,
                                        Quantity = opp.Quantity
                                    });
                                }
                            }
                        }

                        var user = await _updateProvidersService.LoadProvider(new User() { DisanId = order.Object });

                        var newOrder = new Order()
                        {
                            JrId = order.JrId,
                            JournalId = order.JournalId,
                            Object = order.Object,
                            Token = await GenerateToken((user != null) ? user.Name.Trim() : string.Empty),
                            Date = order.Date,
                            User = user,  
                            OrderProductPart = orderProductPart,
                            OrderState = OrderState.New,
                            DeclineNote = ""
                        };
                        return await _orderRepository.Add(newOrder);
                    }
                }
            }
            return new Order();
        }

        private async Task<long> GenerateFakeDisanId()
        {
            var minDisanId = (await _productRepository.Select()).Min(d => d.DisanId) ?? 0;
            if (minDisanId >= 0)
            {
                return -101;
            }
            else
            {
                return minDisanId - 100;
            }
        }

        public async Task<Order> CreateOrRecreateOrder(OrderApiModel orderModel)
        {
            if (orderModel != null)
            {
                var order = await _orderRepository.First(o => o.JrId == orderModel.JrId
                    && o.OrderState != OrderState.DeclinedByRecipient
                    && o.OrderState != OrderState.ApprovedDeclineByProvider);
                if (order != null)
                {
                    if(order.OrderState == OrderState.DeclinedByProvider)
                    {
                        order.OrderState = OrderState.ApprovedDeclineByProvider;
                    }
                    else
                    {
                        order.OrderState = OrderState.DeclinedByRecipient;
                    }
                    if (orderModel.JournalId != null && orderModel.JournalId != 0)
                        order.JournalId = orderModel.JournalId;
                    await _orderRepository.Update(order);
                }
                return await CreateOrder(orderModel);
            }
            return new Order();
        }

        public async Task<List<Order>> GetOrdersForUser(long userId)
        {
            return (await _orderRepository.Where(o => o.User.Id == userId)).ToList();
        }

        public async Task<Order?> GetOrderByOrderModel(OrderModel model)
        {
            return await _orderRepository.First(o => o.Id == model.Id);
        }

        public async Task<OrderModel?> GetOrderModelByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var order = await _orderRepository.First(o => o.Token == token);
            if (order != null)
                if (order.User != null)
                    return await GetOrderModel(order.User, order.Id);
            return null;
        }

        public async Task<OrderModel?> GetOrderModel(User user, long id)
        {
            if (user.Orders != null)
            {
                var order = user.Orders.FirstOrDefault(c => c.Id == id);
                if (order == null) return null;
                var model = new OrderModel
                {
                    Id = id,
                    UserName = user.Name,
                    UserResponse = (order.OrderState == OrderState.New) ? OrderState.ConfirmedByProvider : order.OrderState,
                    OrderState = order.OrderState,
                    Date = order.Date.ToLocalTime(),
                    CreatedAt = order.CreatedAt.ToLocalTime(),
                    LastModified = order.LastModified.ToLocalTime(),
                    DeclineNote = order.DeclineNote ?? string.Empty,
                    Products = order.OrderProductPart.OrderBy(c => c.Product.Name)
                    .Select(c => new OrderProductModel
                    {
                        Id = c.ProductId,
                        Price = c.Price,
                        Name = c.Product.Name,
                        Quantity = c.Quantity,
                    }).ToList(),
                };
                if (!model.Products.Any()) model.Products.Add(new OrderProductModel() { Id = default, Price = default });
                return model;

            }
            return null;
        }
        public async Task DeclineOrderByRecipient(OrderApiModel orderModel)
        {
            var order = await _orderRepository.First(o => o.JrId == orderModel.JrId
                    && o.OrderState != OrderState.DeclinedByRecipient
                    && o.OrderState != OrderState.ApprovedDeclineByProvider);
            if (order != null)
            {
                if(order.OrderState == OrderState.DeclinedByProvider)
                {
                    order.OrderState = OrderState.ApprovedDeclineByProvider;
                }
                else
                {
                    order.OrderState = OrderState.DeclinedByRecipient;
                }
                await _orderRepository.Update(order);
            }
        }
        public async Task ConfirmConfirmedOrDeclinedOrders(List<OrderApiModel> orders)
        {
            foreach (var input in orders)
            {
                var order = await _orderRepository.First(o => o.JrId == input.JrId
                    && o.OrderState != OrderState.DeclinedByRecipient
                    && o.OrderState != OrderState.ApprovedDeclineByProvider);
                if (order != null)
                {
                    if(input.JournalId != 0 && input.JournalId != null)
                    {
                        if(order.OrderState == OrderState.ConfirmedByProvider
                            || order.OrderState == OrderState.EditedByProvider)
                        {
                            order.JournalId = input.JournalId;
                            order.OrderState = OrderState.ApprovedConfirmationByProvider;
                        }
                    }
                    else
                    {
                        if(order.OrderState == OrderState.DeclinedByProvider)
                        {
                            order.OrderState = OrderState.ApprovedDeclineByProvider;
                        }
                    }
                    await _orderRepository.Update(order);
                }
            }
        }
        public async Task<List<OrderApiModel>> GetConfirmedOrDeclinedOrders()
        {
            var list = new List<OrderApiModel>();
            var orders = await _orderRepository
                .Where(o => o.OrderState == OrderState.ConfirmedByProvider
                    || o.OrderState == OrderState.DeclinedByProvider
                    || o.OrderState == OrderState.EditedByProvider);
            foreach (var order in orders)
            {
                var productPart = new List<OrderProductApiModel>();
                foreach(var pp in order.OrderProductPart) 
                {
                    var apiProduct = new OrderProductApiModel() { DisanId = pp.Product.DisanId, Price = pp.Price, Quantity = pp.Quantity };
                    productPart.Add(apiProduct);
                }

                list.Add(new OrderApiModel()
                {
                    JrId = order.JrId,
                    JournalId = order.JournalId,
                    Date = order.Date,
                    OrderState = order.OrderState,
                    Object = order.Object,
                    DeclineNote = order.DeclineNote ?? string.Empty,
                    ProductPart = productPart,
                });
            }
            return list;
        }
        public async Task ConfirmOrder(OrderModel orderModel)
        {
            await ManageOrder(orderModel, OrderState.ConfirmedByProvider);
        }
        public async Task DeclineOrder(OrderModel orderModel)
        {
            await ManageOrder(orderModel, OrderState.DeclinedByProvider);
        }

        public async Task EditOrder(OrderModel orderModel)
        {
            await ManageOrder(orderModel, OrderState.EditedByProvider);
        }

        private async Task ManageOrder(OrderModel model, OrderState state)
        {
            if (state != OrderState.ConfirmedByProvider
                && state != OrderState.DeclinedByProvider
                && state != OrderState.EditedByProvider) return; 
            var order = await _orderRepository.First(o => o.Id == model.Id);
            if (order != null)
            {
                order.OrderState = state;
                if(state!=OrderState.ConfirmedByProvider)
                    order.DeclineNote = model.DeclineNote;

                if(state == OrderState.EditedByProvider)
                {
                    foreach(var modelProduct in model.Products)
                    {
                        var productPart = order.OrderProductPart;
                        var product = productPart.FirstOrDefault(p => p.ProductId == modelProduct.Id && p.OrderId == order.Id);
                        if(product != null && modelProduct.Quantity != null)
                        {
                            product.Quantity = (double)modelProduct.Quantity;
                        }
                    }
                }

                await _orderRepository.Update(order);
            }
        }

        public async Task<OrderProductModel?> GetOriginalProductInOrder(OrderProductModel model, OrderModel order)
        {
            var originalOrder = await _orderRepository.First(o => o.Id == order.Id);
            if(originalOrder != null)
            {
                var productPart = originalOrder.OrderProductPart;
                var product = productPart.FirstOrDefault(p => p.ProductId == model.Id);
                if (product != null)
                {
                    return new OrderProductModel() { Id = product.ProductId, Quantity = product.Quantity, Price = product.Price };
                }
            }
            return null;
        }

        public async Task<Order?> GetOrderById(long id)
        {
            return await _orderRepository.First(o => o.Id == id);
        }
        private async Task<string> GenerateToken(string salt)
        {
            var exprires = DateTime.Now + TimeSpan.FromDays(1);
            if (string.IsNullOrEmpty(salt)) salt = "salt";
            byte[] bytes = Encoding.UTF8.GetBytes(salt + exprires.ToString("s"));
            using (var sha = System.Security.Cryptography.SHA1.Create())
                return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2"))).Substring(8);
        }
    }
}
