using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;


namespace ApiGatewayService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IHttpClientFactory httpFactory, ILogger<ProductsController> logger)
        {
            _httpFactory = httpFactory;
            _logger = logger;
        }

        private HttpClient Client() => _httpFactory.CreateClient("ProductsService");
        private HttpClient InventoryClient() => _httpFactory.CreateClient("InventoryService");

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var client = Client();
            var resp = await client.GetAsync($"/products{Request.QueryString}");
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var client = Client();
            var resp = await client.GetAsync($"/products/{WebUtility.UrlEncode(id)}{Request.QueryString}");
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }

        [HttpPost]
        [Authorize]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Create()
        {
            // Forward multipart/form-data with files
            if (!Request.HasFormContentType)
            {
                return BadRequest(new { message = "Content-Type debe ser multipart/form-data" });
            }

            var form = await Request.ReadFormAsync();
            using var multipart = new MultipartFormDataContent();

            // Add fields
            foreach (var kv in form.Where(k => k.Value.Count > 0))
            {
                if (kv.Key == null) continue;
                // Skip files - they are in form.Files
                if (Request.Form.Files.Any(f => f.Name == kv.Key)) continue;
                multipart.Add(new StringContent(kv.Value), kv.Key);
            }

            // Add files
            foreach (var file in form.Files)
            {
                var stream = file.OpenReadStream();
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType ?? "application/octet-stream");
                multipart.Add(streamContent, file.Name, file.FileName);
            }

            var client = Client();
            var response = await client.PostAsync("/products", multipart);
            var body = await response.Content.ReadAsStringAsync();


            // Agregar producto a inventario con stock 0
            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(body);
                Guid id = Guid.Parse(json.RootElement.GetProperty("id").GetString());

                var inventoryClient = InventoryClient();
                var inventoryResp = await inventoryClient.PostAsync($"/api/Supabase/add",
                    new StringContent(JsonSerializer.Serialize(new { productid = id, stock = 0 }),
                    System.Text.Encoding.UTF8, "application/json"));

            }


            return StatusCode((int)response.StatusCode, body);
        }

        [HttpPatch("{id}")]
        [Authorize]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Patch(string id)
        {
            var client = Client();

            if (Request.HasFormContentType)
            {
                // multipart form-data (possibly with file)
                var form = await Request.ReadFormAsync();
                using var multipart = new MultipartFormDataContent();

                foreach (var kv in form.Where(k => k.Value.Count > 0))
                {
                    if (kv.Key == null) continue;
                    if (Request.Form.Files.Any(f => f.Name == kv.Key)) continue;
                    multipart.Add(new StringContent(kv.Value), kv.Key);
                }

                foreach (var file in form.Files)
                {
                    var stream = file.OpenReadStream();
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType ?? "application/octet-stream");
                    multipart.Add(streamContent, file.Name, file.FileName);
                }

                // PATCH via SendAsync with HttpMethod("PATCH")
                var requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), $"/products/{WebUtility.UrlEncode(id)}")
                {
                    Content = multipart
                };
                var response = await client.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, body);
            }
            else
            {
                // assume JSON body
                using var sr = new StreamReader(Request.Body);
                var json = await sr.ReadToEndAsync();
                var requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), $"/products/{WebUtility.UrlEncode(id)}")
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
                var response = await client.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, body);
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var client = Client();
            var resp = await client.DeleteAsync($"/products/{WebUtility.UrlEncode(id)}");
            var body = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, body);
        }
    }
}
