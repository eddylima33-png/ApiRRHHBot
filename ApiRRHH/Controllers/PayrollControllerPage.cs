using ApiRRHH.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiRRHH.Controllers
{
    [ApiController]
    [Route("api/page/payroll")]
    public class PayrollControllerPage : ControllerBase
    {
        private readonly PayrollServicePage _payrollService;

        public PayrollControllerPage(PayrollServicePage payrollService)
        {
            _payrollService = payrollService;
        }

        [HttpGet("ultima-planilla/{dpi}")]
        public async Task<IActionResult> ObtenerUltimaPlanilla(string dpi)
        {
            if (string.IsNullOrWhiteSpace(dpi))
                return BadRequest(new { mensaje = "Debe ingresar un DPI." });

            var resultado = await _payrollService.ObtenerUltimaPlanillaPorDpiAsync(dpi);

            if (resultado == null)
                return NotFound(new { mensaje = "No se encontró planilla para ese DPI." });

            return Ok(resultado);
        }
    }
}