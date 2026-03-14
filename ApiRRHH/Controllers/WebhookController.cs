using ApiRRHH.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiRRHH.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly WhatsappService _whatsappService;
    private readonly PayrollService _payrollService;

    public WebhookController(
        IConfiguration configuration,
        WhatsappService whatsappService,
        PayrollService payrollService)
    {
        _configuration = configuration;
        _whatsappService = whatsappService;
        _payrollService = payrollService;
    }

    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string verifyToken,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        var expectedToken = _configuration["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && verifyToken == expectedToken)
        {
            return Ok(challenge);
        }

        return Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload)
    {
        try
        {
            var entry = payload.GetProperty("entry")[0];
            var changes = entry.GetProperty("changes")[0];
            var value = changes.GetProperty("value");

            if (!value.TryGetProperty("messages", out var messages))
                return Ok();

            var message = messages[0];
            var from = message.GetProperty("from").GetString() ?? "";
            var text = message.GetProperty("text").GetProperty("body").GetString()?.Trim() ?? "";

            if (text.StartsWith("codigo ", StringComparison.OrdinalIgnoreCase))
            {
                var codigo = text.Replace("codigo ", "", StringComparison.OrdinalIgnoreCase).Trim();

                var employee = await _payrollService.ValidateEmployeeAsync(codigo);

                if (!employee.exists)
                {
                    await _whatsappService.SendTextMessageAsync(from, $"No encontré un empleado activo con código {codigo}.");
                    return Ok();
                }

                var summary = await _payrollService.GetLatestPayrollSummaryAsync(employee.idEmpleado);

                if (summary is null)
                {
                    await _whatsappService.SendTextMessageAsync(from,$"Encontré a {employee.nombre}, pero aún no tiene planillas registradas.");
                }
                else
                {
                    await _whatsappService.SendTextMessageAsync(from, $"{employee.nombre}\n{summary}");
                }
            }
            else
            {
                await _whatsappService.SendTextMessageAsync(
                    from,
                    "Bienvenido al bot de planilla.\nEnvía: codigo E001"
                );
            }

            return Ok();
        }
        catch
        {
            return Ok();
        }
    }
}