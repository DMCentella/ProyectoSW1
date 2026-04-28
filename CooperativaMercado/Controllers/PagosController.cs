using CooperativaMercado.Repository.Dao;
using CooperativaMercado.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CooperativaMercado.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PagosController : ControllerBase
    {
        private readonly PagoDao _pagoDao;

        public PagosController(PagoDao pagoDao)
        {
            _pagoDao = pagoDao;
        }


        [HttpPost("registrarPago")]
        public ActionResult RegistrarPago([FromBody] RegistrarPagoRequest request)
        {
            if (request == null)
                return BadRequest(new { mensaje = "Los datos del pago son requeridos" });

            if (request.Monto <= 0)
                return BadRequest(new { mensaje = "El monto debe ser mayor a cero" });

            if (string.IsNullOrWhiteSpace(request.MetodoPago))
                return BadRequest(new { mensaje = "El método de pago es requerido" });

            try
            {
                _pagoDao.RegistrarPago(request.IdDeuda, request.Monto, request.MetodoPago);
                return Ok(new 
                { 
                    mensaje = "Pago registrado exitosamente",
                    idDeuda = request.IdDeuda,
                    monto = request.Monto,
                    metodoPago = request.MetodoPago
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al procesar el pago: {ex.Message}" });
            }
        }

        [HttpPost("pagar")]
        public ActionResult Pagar(int idDeuda, decimal monto, string metodo)
        {
            try
            {
                _pagoDao.RegistrarPago(idDeuda, monto, metodo);
                return Ok("Pago registrado exitosamente");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al procesar el pago: {ex.Message}");
            }
        }

        [HttpGet("getPagos")]
        public ActionResult getPagos()
        {
            var lista = _pagoDao.Listar();
            if (lista == null || !lista.Any())
                return NoContent();

            return Ok(lista);
        }

        [HttpGet("reporteRecaudado")]
        public ActionResult reporteRecaudado(DateTime inicio, DateTime fin)
        {
            if (inicio > fin)
                return BadRequest("La fecha inicio no puede ser mayor que la fecha fin");

            var total = _pagoDao.TotalRecaudadoPorRango(inicio, fin);

            return Ok(new
            {
                FechaInicio = inicio,
                FechaFin = fin,
                TotalRecaudado = total
            });
        }
    }
}