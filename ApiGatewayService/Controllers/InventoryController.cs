using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace ApiGatewayService.Controllers
{
    [Route("[controller]")]
    public class InventoryController : Controller
    {
        private readonly ILogger<InventoryController> _logger;
        private readonly IHttpClientFactory _httpFactory;

        public InventoryController(ILogger<InventoryController> logger, IHttpClientFactory httpFactory)
        {
            _logger = logger;
            _httpFactory = httpFactory;
        }

        private HttpClient InventoryClient() => _httpFactory.CreateClient("InventoryService");
        private HttpClient ProductsClient() => _httpFactory.CreateClient("ProductsService");

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            // --- INVENTORY SERVICE ---
            var invClient = InventoryClient();
            var invResp = await invClient.GetAsync($"/api/Supabase/getAll{Request.QueryString}");
            var invBody = await invResp.Content.ReadAsStringAsync();

            if (!invResp.IsSuccessStatusCode)
                return StatusCode((int)invResp.StatusCode, invBody);

            if (invResp.Content.Headers.ContentType?.MediaType != "application/json")
                return StatusCode(500, new { error = "Inventory no devolvió JSON", invBody });

            var inventory = JsonSerializer.Deserialize<List<InventoryDTO>>(invBody);


            // --- PRODUCTS SERVICE ---
            var prodClient = ProductsClient();
            var prodResp = await prodClient.GetAsync($"/products{Request.QueryString}");
            var prodBody = await prodResp.Content.ReadAsStringAsync();

            if (!prodResp.IsSuccessStatusCode)
                return StatusCode((int)prodResp.StatusCode, prodBody);

            if (prodResp.Content.Headers.ContentType?.MediaType != "application/json")
                return StatusCode(500, new { error = "Products no devolvió JSON", prodBody });

            var products = JsonSerializer.Deserialize<List<ProductDTO>>(prodBody);

            // --- JOIN ---
            var inventoryDict = inventory.ToDictionary(i => i.productid.ToString(), i => i.stock);

            var result = products
                .Select(p => new ProductWithStockDTO
                {
                    id = p.id,
                    name = p.name,
                    category = p.category,
                    isActive = p.isActive,
                    stock = inventoryDict.TryGetValue(p.id, out var stock) ? stock : 0
                })
                .ToList();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            // --- INVENTORY SERVICE ---
            var invClient = InventoryClient();
            var invResp = await invClient.GetAsync(
                $"/api/Supabase/get/{WebUtility.UrlEncode(id.ToString())}{Request.QueryString}"
            );
            var invBody = await invResp.Content.ReadAsStringAsync();

            if (!invResp.IsSuccessStatusCode)
                return StatusCode((int)invResp.StatusCode, invBody);

            if (invResp.Content.Headers.ContentType?.MediaType != "application/json")
                return StatusCode(500, new { error = "Inventory no devolvió JSON", invBody });

            var inventoryItem = JsonSerializer.Deserialize<InventoryDTO>(invBody);

            // --- PRODUCTS SERVICE ---
            var prodClient = ProductsClient();
            var prodResp = await prodClient.GetAsync(
                $"/products/{WebUtility.UrlEncode(id.ToString())}{Request.QueryString}"
            );
            var prodBody = await prodResp.Content.ReadAsStringAsync();

            if (!prodResp.IsSuccessStatusCode)
                return StatusCode((int)prodResp.StatusCode, prodBody);

            if (prodResp.Content.Headers.ContentType?.MediaType != "application/json")
                return StatusCode(500, new { error = "Products no devolvió JSON", prodBody });

            var product = JsonSerializer.Deserialize<ProductDTO>(prodBody);

            // --- JOIN ---
            var result = new ProductWithStockDTO
            {
                id = product.id,
                name = product.name,
                category = product.category,
                isActive = product.isActive,
                stock = inventoryItem?.stock ?? 0
            };

            return Ok(result);
        }



        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateStock(Guid id, [FromBody] long stock)
        {
            try
            {
                var inventoryClient = InventoryClient();
                var inventoryResp = await inventoryClient.PatchAsync(
                    $"/api/Supabase/update/set/{WebUtility.UrlEncode(id.ToString())}", 
                    new StringContent(JsonSerializer.Serialize(stock), Encoding.UTF8, "application/json")
                );
                var invBody = await inventoryResp.Content.ReadAsStringAsync();

                return StatusCode((int)inventoryResp.StatusCode, invBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
            
        }

        private class InventoryDTO
        {
            public  Guid productid { get; set; }
            public  long stock { get; set; }
        }
        private class ProductDTO
        {
            public string id { get; set; }
            public string name { get; set; }
            public string category { get; set; }
            public bool isActive { get; set; }
        }
        private class ProductWithStockDTO
        {
            public string id { get; set; }
            public string name { get; set; }
            public string category { get; set; }
            public bool isActive { get; set; }
            public long stock { get; set; }
        }




    }
}