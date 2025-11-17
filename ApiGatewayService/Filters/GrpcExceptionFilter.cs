using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiGatewayService.Filters
{
    public class GrpcExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is RpcException rpcException)
            {
                // Mapea el código de gRPC a un código HTTP (ej. NotFound -> 404)
                var statusCode = MapGrpcStatusCode(rpcException.StatusCode);

                // Crea una respuesta de error clara
                var result = new ObjectResult(new { message = rpcException.Status.Detail })
                {
                    StatusCode = statusCode
                };

                // Asigna el resultado y marca la excepción como manejada
                context.Result = result;
                context.ExceptionHandled = true;
            }
        }
        private int MapGrpcStatusCode(StatusCode grpcStatusCode)
        {
            return grpcStatusCode switch
            {
                StatusCode.NotFound => 404,
                StatusCode.InvalidArgument => 400,
                StatusCode.AlreadyExists => 409,
                StatusCode.Unauthenticated => 401,
                StatusCode.PermissionDenied => 403,
                _ => 500 // Error interno por defecto
            };
        }
    }
}