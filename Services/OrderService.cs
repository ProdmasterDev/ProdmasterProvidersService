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
        public OrderService(OrderRepository orderRepository, ProductRepository productRepository, UserRepository userRepository, StandartRepository standartRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _standartRepository = standartRepository;
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

                        var user = await _userRepository.First(u => u.DisanId == order.Object);

                        if (user == null)
                        {
                            order.Object = default;
                        }

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
                        if(order.OrderState == OrderState.ConfirmedByProvider)
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
                    || o.OrderState == OrderState.DeclinedByProvider);
            foreach (var order in orders)
            {
                list.Add(new OrderApiModel()
                {
                    JrId = order.JrId,
                    JournalId = order.JournalId,
                    Date = order.Date,
                    Object = order.Object,
                    DeclineNote = order.DeclineNote ?? string.Empty,
                });
            }
            return list;
        }
        public async Task ConfirmOrder(OrderModel orderModel)
        {
            var order = await _orderRepository.First(o => o.Id == orderModel.Id);
            if (order != null)
            {
                order.OrderState = OrderState.ConfirmedByProvider;
                await _orderRepository.Update(order);
            }
        }
        public async Task DeclineOrder(OrderModel orderModel)
        {
            var order = await _orderRepository.First(o => o.Id == orderModel.Id);
            if(order != null)
            {
                order.OrderState = OrderState.DeclinedByProvider;
                order.DeclineNote = orderModel.DeclineNote;
                await _orderRepository.Update(order);
            }
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
