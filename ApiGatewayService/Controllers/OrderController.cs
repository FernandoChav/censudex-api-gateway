using Microsoft.AspNetCore.Mvc;
using Grpc.Core;

// IMPORTANTE: Usar alias para evitar conflictos de nombres
using OrderServiceProtos = global::Censudex_orders.Protos;
using GrpcStatusCode = Grpc.Core.StatusCode;
using Microsoft.AspNetCore.Authorization;

namespace ApiGatewayService.Controllers
{
    /// <summary>
    /// Controlador para gestionar pedidos
    /// Se comunica con Order Service mediante gRPC
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderServiceProtos.OrderService.OrderServiceClient _orderClient;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            OrderServiceProtos.OrderService.OrderServiceClient orderClient,
            ILogger<OrdersController> logger)
        {
            _orderClient = orderClient;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/orders - Crear un nuevo pedido
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderServiceProtos.CreateOrderRequest request)
        {
            try
            {
                _logger.LogInformation("Creando pedido para cliente: {ClientId}", request.ClientId);

                var response = await _orderClient.CreateOrderAsync(request);

                _logger.LogInformation("Pedido creado exitosamente: {OrderId}", response.Id);

                var result = new
                {
                    id = response.Id,
                    client_id = response.ClientId,
                    status = response.Status,
                    total_amount = response.TotalAmount,
                    shipping_address = response.ShippingAddress,
                    created_at = response.CreatedAt,
                    updated_at = response.UpdatedAt,
                    items = response.Items.Select(i => new
                    {
                        id = i.Id,
                        product_id = i.ProductId,
                        product_name = i.ProductName,
                        quantity = i.Quantity,
                        unit_price = i.UnitPrice,
                        subtotal = i.Subtotal
                    })
                };

                return CreatedAtAction(nameof(GetOrderById), new { id = response.Id }, result);
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.InvalidArgument)
            {
                _logger.LogWarning(ex, "Datos inválidos al crear pedido");
                return BadRequest(new { error = "Datos inválidos", detail = ex.Status.Detail });
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.Unavailable)
            {
                _logger.LogError(ex, "Order Service no disponible");
                return StatusCode(503, new { error = "Order Service no disponible" });
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error gRPC al crear pedido");
                return StatusCode(500, new { error = "Error interno", detail = ex.Status.Detail });
            }
        }

        /// <summary>
        /// GET /api/orders - Obtener todos los pedidos con filtros opcionales
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string? orderId,
            [FromQuery] string? clientId,
            [FromQuery] string? clientName,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            try
            {
                _logger.LogInformation("Consultando pedidos con filtros");

                var request = new OrderServiceProtos.GetOrdersRequest
                {
                    OrderId = orderId ?? "",
                    ClientId = clientId ?? "",
                    ClientName = clientName ?? "",
                    StartDate = startDate ?? "",
                    EndDate = endDate ?? ""
                };

                var response = await _orderClient.GetOrdersAsync(request);

                var result = new
                {
                    total_count = response.TotalCount,
                    orders = response.Orders.Select(o => new
                    {
                        id = o.Id,
                        client_id = o.ClientId,
                        status = o.Status,
                        total_amount = o.TotalAmount,
                        shipping_address = o.ShippingAddress,
                        tracking_number = o.TrackingNumber,
                        created_at = o.CreatedAt,
                        updated_at = o.UpdatedAt,
                        items = o.Items.Select(i => new
                        {
                            id = i.Id,
                            product_id = i.ProductId,
                            product_name = i.ProductName,
                            quantity = i.Quantity,
                            unit_price = i.UnitPrice,
                            subtotal = i.Subtotal
                        })
                    })
                };

                return Ok(result);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error gRPC al obtener pedidos");
                return StatusCode(500, new { error = "Error interno", detail = ex.Status.Detail });
            }
        }

        /// <summary>
        /// GET /api/orders/{id} - Obtener un pedido específico por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrderById(string id)
        {
            try
            {
                _logger.LogInformation("Consultando pedido: {OrderId}", id);

                var request = new OrderServiceProtos.GetOrderByIdRequest { OrderId = id };
                var response = await _orderClient.GetOrderByIdAsync(request);

                var result = new
                {
                    id = response.Id,
                    client_id = response.ClientId,
                    status = response.Status,
                    total_amount = response.TotalAmount,
                    shipping_address = response.ShippingAddress,
                    tracking_number = response.TrackingNumber,
                    cancellation_reason = response.CancellationReason,
                    created_at = response.CreatedAt,
                    updated_at = response.UpdatedAt,
                    items = response.Items.Select(i => new
                    {
                        id = i.Id,
                        product_id = i.ProductId,
                        product_name = i.ProductName,
                        quantity = i.Quantity,
                        unit_price = i.UnitPrice,
                        subtotal = i.Subtotal
                    })
                };

                return Ok(result);
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Pedido no encontrado: {OrderId}", id);
                return NotFound(new { error = "Pedido no encontrado", orderId = id });
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error gRPC al obtener pedido {OrderId}", id);
                return StatusCode(500, new { error = "Error interno", detail = ex.Status.Detail });
            }
        }

        /// <summary>
        /// PUT /api/orders/{id}/status - Actualizar el estado de un pedido
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrderStatus(
            string id,
            [FromBody] OrderServiceProtos.UpdateOrderStatusRequest request)
        {
            try
            {
                _logger.LogInformation("Actualizando estado del pedido {OrderId} a {Status}", 
                    id, request.Status);

                request.OrderId = id;
                var response = await _orderClient.UpdateOrderStatusAsync(request);

                var result = new
                {
                    id = response.Id,
                    client_id = response.ClientId,
                    status = response.Status,
                    total_amount = response.TotalAmount,
                    tracking_number = response.TrackingNumber,
                    updated_at = response.UpdatedAt
                };

                return Ok(result);
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Pedido no encontrado: {OrderId}", id);
                return NotFound(new { error = "Pedido no encontrado", orderId = id });
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.FailedPrecondition)
            {
                _logger.LogWarning(ex, "No se puede actualizar el pedido {OrderId}", id);
                return BadRequest(new { error = "No se puede actualizar", detail = ex.Status.Detail });
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error gRPC al actualizar pedido {OrderId}", id);
                return StatusCode(500, new { error = "Error interno", detail = ex.Status.Detail });
            }
        }

        /// <summary>
        /// PATCH /api/orders/{id} - Cancelar un pedido
        /// </summary>
        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelOrder(
            string id,
            [FromBody] OrderServiceProtos.CancelOrderRequest request)
        {
            try
            {
                _logger.LogInformation("Cancelando pedido {OrderId}", id);

                request.OrderId = id;
                var response = await _orderClient.CancelOrderAsync(request);

                var result = new
                {
                    id = response.Id,
                    client_id = response.ClientId,
                    status = response.Status,
                    cancellation_reason = response.CancellationReason,
                    updated_at = response.UpdatedAt
                };

                return Ok(result);
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Pedido no encontrado: {OrderId}", id);
                return NotFound(new { error = "Pedido no encontrado", orderId = id });
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.FailedPrecondition)
            {
                _logger.LogWarning(ex, "No se puede cancelar el pedido {OrderId}", id);
                return BadRequest(new { error = "No se puede cancelar", detail = ex.Status.Detail });
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error gRPC al cancelar pedido {OrderId}", id);
                return StatusCode(500, new { error = "Error interno", detail = ex.Status.Detail });
            }
        }
    }
}