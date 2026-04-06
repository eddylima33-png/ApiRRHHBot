using ApiRRHH.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiRRHH.Controllers
{
    [ApiController]
    [Route("api/portal/auth")]
    public class PortalAuthController : ControllerBase
    {
        private readonly PortalAuthService _portalAuthService;

        public PortalAuthController(PortalAuthService portalAuthService)
        {
            _portalAuthService = portalAuthService;
        }

        [HttpGet("estado/{dpi}")]
        public async Task<IActionResult> VerificarEstado(string dpi)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dpi))
                    return BadRequest(new { mensaje = "Debe ingresar un DPI." });

                var resultado = await _portalAuthService.VerificarEstadoDpiAsync(dpi);

                if (!resultado.ExisteEmpleado)
                {
                    return NotFound(new
                    {
                        existeEmpleado = false,
                        tienePerfil = false,
                        mensaje = "El DPI no existe en el sistema."
                    });
                }

                return Ok(new
                {
                    existeEmpleado = true,
                    tienePerfil = resultado.TienePerfil,
                    idEmpleado = resultado.IdEmpleado,
                    codigoEmpleado = resultado.CodigoEmpleado,
                    mensaje = resultado.TienePerfil
                        ? "El empleado ya tiene perfil creado."
                        : "El empleado aún no tiene perfil."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno al validar el DPI.",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("registrar-pin")]
        public async Task<IActionResult> RegistrarPin([FromBody] RegistrarPinRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { mensaje = "Debe enviar los datos del registro." });

                if (string.IsNullOrWhiteSpace(request.DPI))
                    return BadRequest(new { mensaje = "Debe ingresar un DPI." });

                if (string.IsNullOrWhiteSpace(request.Pin))
                    return BadRequest(new { mensaje = "Debe ingresar un PIN." });

                var resultado = await _portalAuthService.RegistrarPinAsync(request.DPI, request.Pin);

                if (!resultado.Ok)
                    return BadRequest(new { mensaje = resultado.Mensaje });

                return Ok(new
                {
                    ok = true,
                    mensaje = resultado.Mensaje
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno al registrar el PIN.",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginPortalRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { mensaje = "Debe enviar los datos de acceso." });

                if (string.IsNullOrWhiteSpace(request.DPI))
                    return BadRequest(new { mensaje = "Debe ingresar un DPI." });

                if (string.IsNullOrWhiteSpace(request.Pin))
                    return BadRequest(new { mensaje = "Debe ingresar un PIN." });

                var resultado = await _portalAuthService.LoginAsync(request.DPI, request.Pin);

                if (!resultado.Ok)
                {
                    return Unauthorized(new
                    {
                        ok = false,
                        mensaje = resultado.Mensaje
                    });
                }

                return Ok(new
                {
                    ok = true,
                    mensaje = resultado.Mensaje,
                    idEmpleado = resultado.IdEmpleado,
                    codigoEmpleado = resultado.CodigoEmpleado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno al iniciar sesión.",
                    detalle = ex.Message
                });
            }
        }
    }

    public class RegistrarPinRequest
    {
        public string DPI { get; set; } = "";
        public string Pin { get; set; } = "";
    }

    public class LoginPortalRequest
    {
        public string DPI { get; set; } = "";
        public string Pin { get; set; } = "";
    }
}