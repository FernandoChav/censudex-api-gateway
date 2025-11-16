using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayService.Services.Interfaces
{
    public interface IAuthProxyService
    {
        Task<IActionResult> ProxyLoginAsync(JsonElement body);


        Task<IActionResult> ProxyLogoutAsync(string? authorizationHeader);

        Task<IActionResult> ProxyValidateTokenAsync(string? authorizationHeader);
    }
}