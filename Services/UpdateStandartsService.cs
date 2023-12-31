﻿using Newtonsoft.Json;
using ProdmasterProvidersService.Database;
using ProdmasterProvidersService.Migrations;
using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace ProdmasterProvidersService.Services
{
    public class UpdateStandartsService : IUpdateStandartsService
    {
        private readonly HttpClient _httpClient;
        private readonly StandartRepository _standartRepository;
        private readonly ProductRepository _productRepository;
        private readonly OrderRepository _orderRepository;
        private const string url = "http://192.168.1.251:8444/api";

        public UpdateStandartsService(HttpClient httpClient, StandartRepository standartRepository, ProductRepository productRepository, OrderRepository orderRepository)
        {
            _httpClient = httpClient;
            _standartRepository = standartRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }
        public async Task Update()
        {
            var units = await GetUnits();
            if (units != null)
            {
                await UpdateUnits(units);
            }

            var standarts = await GetStandarts();
            if (standarts != null)
            {
                await UpdateStandarts(standarts);
            }

            var countries = await GetCountries();
            if (countries != null)
            {
                await UpdateCountries(countries);
            }

            var manufacturers = await GetManufacturers();
            if (manufacturers != null)
            {
                await UpdateManufacturers(manufacturers);
            }

            var products = await GetProducts();
            if (products != null)
            {
                await UpdateProducts(products);
            }

            var orders = await GetOrders();
            if (orders != null)
            {
                await UpdateOrders(orders);
            }
        }

        private T? GetObjectFromDataString<T>(string dataString)
        {
            return JsonConvert.DeserializeObject<T>(dataString);
        }

        private async Task<T?> GetObjectsFromQueryAsync<T>(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                var content = new StringContent(query, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                var result = await _httpClient.PostAsync(url, content);
                if (result != null && result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var dataString = await result.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(dataString))
                    {
                        return GetObjectFromDataString<T>(dataString);
                    }
                }
            }
            return default;
        }

        private Task UpdateStandarts(IEnumerable<Standart> standarts)
        {
            if (standarts.Any())
            {
                standarts = standarts.Where(c => c.UnitId != null && c.UnitId != 0);
                foreach (var standart in standarts)
                {
                    standart.Name = standart.Name.Trim();
                    standart.CategoryName = standart.CategoryName?.Trim() ?? "Остальное";
                    standart.Taste = standart.Taste?.Trim() ?? "Не указан";
                    standart.Smell = standart.Smell?.Trim() ?? "Не указан";
                    standart.Color = standart.Color?.Trim() ?? "Не указан";
                    standart.Structure = standart.Structure?.Trim() ?? "Не указана";
                    standart.Consistenc = standart.Consistenc?.Trim() ?? "Не указана";
                }
                return _standartRepository.AddOrUpdateRange(standarts);
            }
            return Task.CompletedTask;
        }

        private Task UpdateUnits(IEnumerable<Unit> units)
        {
            if (units.Any())
            {
                units = units.DistinctBy(c => c.Id).Where(c => c.Id != default);
                foreach (var unit in units)
                {
                    unit.Name = unit.Name.Trim();
                    unit.Short = unit.Short.Trim();
                }
                return _standartRepository.AddOrUpdateRange(units);
            }
            return Task.CompletedTask;
        }

        private Task UpdateCountries(IEnumerable<Country> countries)
        {
            if (countries.Any())
            {
                countries = countries.DistinctBy(c => c.Id);
                foreach (var unit in countries)
                {
                    unit.Name = unit.Name?.Trim();
                    unit.FullName = unit.FullName?.Trim();
                    unit.EngName = unit.EngName?.Trim();
                    unit.Code = unit.Code?.Trim();
                    unit.Code3 = unit.Code3?.Trim();
                }
                return _standartRepository.AddOrUpdateRange(countries);
            }
            return Task.CompletedTask;
        }

        private Task UpdateManufacturers(IEnumerable<Manufacturer> manufacturers)
        {
            if (manufacturers.Any())
            {
                manufacturers = manufacturers.DistinctBy(c => c.Id);
                foreach (var unit in manufacturers)
                {
                    unit.Name = unit.Name?.Trim();
                }
                return _standartRepository.AddOrUpdateRange(manufacturers);
            }
            return Task.CompletedTask;
        }

        private async Task UpdateProducts(IEnumerable<Product> products)
        {
            var productsFiltered= (await _productRepository.Where(p => p.DisanId != null)).ToList();
            productsFiltered = productsFiltered.Where(p => products.Any(x => x.DisanId == p.DisanId)).ToList();
            products = products.Where(p => productsFiltered.Any(pis => pis.DisanId == p.DisanId));
            if (products.Any())
            {
                var productsToUpdate = new List<Product>();
                foreach (var product in products)
                {
                    var item = productsFiltered.First(p => p.DisanId == product.DisanId);
                    if (item != null)
                    {
                        item.ManufacturerId = product.ManufacturerId;
                        //item.ManufacturerName = product.ManufacturerName;
                        item.Brand = product.Brand ?? item.Brand;
                        item.VendorCode = product.VendorCode ?? item.VendorCode;
                        item.Quantity = (product.Quantity != default) ? product.Quantity : item.Quantity;
                        item.CountryId = (product.CountryId == null || product.CountryId == 0) ? item.CountryId : product.CountryId;
                        if (productsToUpdate.FirstOrDefault(p => p.Id == item.Id) != null) { productsToUpdate.Add(item); }
                    }
                }
                await _productRepository.AddOrUpdateRange(productsToUpdate);
            }
        }
        private async Task UpdateOrders(IEnumerable<Order> orders)
        {
            if (orders.Any())
            {
                var ordersToUpdate = new List<Order>();
                foreach (var order in orders)
                {
                    var items = await _orderRepository.Where(p => p.JrId == order.JrId);
                    if (!items.Any() || order.JrId == 0)
                    {
                        items = await _orderRepository.Where(p => p.DocNumber == order.GetCleanDocNumber());
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                item.JrId = order.JrId;
                                foreach (var product in item.OrderProductPart)
                                {
                                    if (product.OriginalQuantity == null || product.OriginalQuantity == 0)
                                    {
                                        product.OriginalQuantity = product.Quantity;
                                    }
                                }
                                if (ordersToUpdate.FirstOrDefault(p => p.Id == item.Id) == null)
                                    ordersToUpdate.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            if (item != null)
                            {
                                var docNumberClean = order.GetCleanDocNumber();
                                if (docNumberClean != string.Empty && docNumberClean != item.DocNumber)
                                    item.DocNumber = docNumberClean;
                                foreach (var product in item.OrderProductPart)
                                {
                                    if (product.OriginalQuantity == null || product.OriginalQuantity == 0)
                                    {
                                        product.OriginalQuantity = product.Quantity;
                                    }
                                }
                                if (ordersToUpdate.FirstOrDefault(p => p.Id == item.Id) == null)
                                    ordersToUpdate.Add(item);
                            }
                        }
                    }
                }
                await _orderRepository.AddOrUpdateRange(ordersToUpdate);
            }
        }

        public string GetNumber(string docnumber)
        {
            var searchWord = "закупка";
            if (docnumber.Contains("закупка".ToUpper()))
            {
                var position = searchWord.Length + 2;
                return docnumber.Substring(position).Trim();
            }
            return string.Empty;
        }
        private Task<IEnumerable<Standart>?> GetStandarts()
        {
            var query = "\"select stands.number, stands.stock, stands.taste, stands.smell, stands.color, stands.structure, stands.consistenc, iif(not(isnull(saname.cvalue) or empty(saname.cvalue)), saname.cvalue, iif(not(empty(stands.nameprod)),stands.nameprod,stock.name)) as nameprod, stands.create, stands.modify, stock.unit as unitId, unit.short as unitShort, unit.name as unitName, sagroup.cvalue as categoryname " +
                "from standart.dbf as stands " +
                "left join stock.dbf as stock on stock.number == stands.stock " +
                "left join unitn.dbf as unit on unit.number == stock.unit " +
                "left join stockattribute.dbf as sagroup on stock.number == sagroup.r1 and sagroup.name == 'ГРУППА ПОРТАЛ' " +
                "left join stockattribute.dbf as saname on stock.number == saname.r1 and saname.name == 'НАИМЕНОВАНИЕ ПОРТАЛ' " +
                "where stands.custom == 751601 and not stands.arch\"";
            return GetObjectsFromQueryAsync<IEnumerable<Standart>>(query);
        }
        private Task<IEnumerable<Unit>?> GetUnits()
        {
            var query = "\"select unit.number as unitId, unit.short as unitShort, unit.name as unitName from unitn as unit\"";
            return GetObjectsFromQueryAsync<IEnumerable<Unit>>(query);
        }
        private Task<IEnumerable<Country>?> GetCountries()
        {
            var query = "\"select number, name, fullname, engname, code, code3 from country\"";
            return GetObjectsFromQueryAsync<IEnumerable<Country>>(query);
        }
        private Task<IEnumerable<Manufacturer>?> GetManufacturers()
        {
            var query = "\"select number, name, create, modify from manufacturer where not arch\"";
            return GetObjectsFromQueryAsync<IEnumerable<Manufacturer>>(query);
        }
        private Task<IEnumerable<Product>?> GetProducts()
        {
            var query = "\"select st1.number, iif(empty(st1.nameprod),stock.name,st1.nameprod) as nameprod, st1.price, TRANSFORM(st1.minkvant,'9999.999') as minkvant, st1.article, st1.create, st1.modify, st1.country, st1.brand, st1.manufact, st1.stock as stock, st2.number as parent " +
               "from standart as st1 " +
               "inner join standart as st2 on st1.stock == st2.stock " +
               "inner join stock as stock on st1.stock == stock.number " +
               $"where not st1.arch and st2.custom == 751601\"";
            return GetObjectsFromQueryAsync<IEnumerable<Product>>(query);
        }
        private Task<IEnumerable<Order>?> GetOrders()
        {
            var query = "\"select jr.number as jrid, jrf.jmain as journalid, jr.object, jr.doc_number as docnumber, '' as token, jr.date " +
               "from jr " +
               "left join jrf on jr.number == jrf.number " +
               $"where jr.opera == 2 and 'ЗАКУПКА'$jr.Doc_number and not arch\"";
            return GetObjectsFromQueryAsync<IEnumerable<Order>>(query);
        }
    }
}
