using CooperativaMercado.Model;
using CooperativaMercado.Repository.Dao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CooperativaMercado.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeudasController : ControllerBase
    {
        private readonly DeudaDao _deudaDao;

        public DeudasController(DeudaDao deudaDao)
        {
            _deudaDao = deudaDao;
        }

        [HttpGet("getDeudas")]
        [Authorize(Roles = "Admin")]
        public ActionResult getDeudas()
        {
            var lista = _deudaDao.Listar();
            if (lista == null || !lista.Any())
                return NoContent();

            return Ok(lista);
        }

        [HttpGet("pendientes")]
        [Authorize(Roles = "Admin")]
        public ActionResult getPendientes()
        {
            var lista = _deudaDao.ObtenerPendientes();
            if (lista == null || !lista.Any())
                return NoContent();

            return Ok(lista);
        }

        [HttpGet("misDeudas")]
        [Authorize(Roles = "Socio")]
        public ActionResult MisDeudas()
        {
            var idSocioClaim = User.FindFirst("IdSocio")?.Value;
            if (string.IsNullOrEmpty(idSocioClaim) || !int.TryParse(idSocioClaim, out int idSocio))
                return BadRequest("No se pudo obtener el ID del socio");

            var lista = _deudaDao.ObtenerPorSocio(idSocio);
            if (lista == null || !lista.Any())
                return Ok(new { mensaje = "No tienes deudas registradas", deudas = new List<Deuda>() });

            var agrupado = lista.GroupBy(d => d.NumeroPuesto)
                                .Select(g => new
                                {
                                    puesto = g.Key,
                                    deudas = g.ToList(),
                                    totalPuesto = g.Sum(d => d.MontoTotal)
                                });

            return Ok(new
            {
                totalGeneral = lista.Sum(d => d.MontoTotal),
                detallesPorPuesto = agrupado
            });
        }

        [HttpPost("saveDeuda")]
        [Authorize(Roles = "Admin")]
        public ActionResult saveDeuda(Deuda deuda)
        {
            if (deuda == null)
                return BadRequest("Los datos de la deuda son requeridos");
            try
            {
                _deudaDao.Registrar(deuda);
                return Created("", deuda);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al registrar deuda: {ex.Message}");
            }
        }

        [HttpPost("generarAlquiler")]
        [Authorize(Roles = "Admin")]
        public ActionResult generar(int mes, int anio)
        {
            if (mes < 1 || mes > 12)
                return BadRequest("El mes debe estar entre 1 y 12");
            try
            {
                _deudaDao.GenerarAlquilerMensual(mes, anio);
                return Ok("Proceso de generacion de alquileres finalizado");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al generar alquileres: {ex.Message}");
            }
        }

        [HttpPost("aplicarMora")]
        [Authorize(Roles = "Admin")]
        public ActionResult AplicarMora(int idDeuda, decimal montoMora)
        {
            if (montoMora < 0)
                return BadRequest("El monto de la mora no puede ser negativo");

            try
            {
                var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
                if (string.IsNullOrEmpty(idUsuarioClaim) || !int.TryParse(idUsuarioClaim, out int idUsuario))
                    return BadRequest("No se pudo obtener el ID del usuario");

                _deudaDao.AplicarMora(idDeuda, montoMora, idUsuario);
                return Ok(new { mensaje = "Mora aplicada exitosamente", idDeuda, montoMora });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al aplicar mora: {ex.Message}");
            }
        }

        [HttpGet("reporteDeudasPendientes")]
        [Authorize(Roles = "Admin")]
        public ActionResult reporteDeudasPendientes()
        {
            var lista = _deudaDao.ReportePendientes();
            if (lista == null || !lista.Any())
                return NoContent();

            return Ok(lista);
        }

        [HttpGet("reporteDeudasPagadas")]
        [Authorize(Roles = "Admin")]
        public ActionResult reporteDeudasPagadas()
        {
            var lista = _deudaDao.ReportePagadas();
            if (lista == null || !lista.Any())
                return NoContent();

            return Ok(lista);
        }

        [HttpPost("generarDeudasRecurrentes")]
        [Authorize(Roles = "Admin")]
        public ActionResult GenerarDeudasRecurrentes(int mes, int anio)
        {
            if (mes < 1 || mes > 12)
                return BadRequest(new { mensaje = "El mes debe estar entre 1 y 12" });

            try
            {
                _deudaDao.GenerarDeudasRecurrentes(mes, anio);
                string[] meses = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", 
                                   "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
                return Ok(new 
                { 
                    mensaje = $"Deudas recurrentes de {meses[mes]} {anio} generadas correctamente",
                    mes,
                    anio
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al generar deudas recurrentes: {ex.Message}" });
            }
        }

        [HttpPost("generarTodasLasDeudas")]
        [Authorize(Roles = "Admin")]
        public ActionResult GenerarTodasLasDeudas(int mes, int anio)
        {
            if (mes < 1 || mes > 12)
                return BadRequest(new { mensaje = "El mes debe estar entre 1 y 12" });

            try
            {
                _deudaDao.GenerarTodasLasDeudas(mes, anio);
                string[] meses = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", 
                                   "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
                return Ok(new 
                { 
                    mensaje = $"Todas las deudas de {meses[mes]} {anio} generadas correctamente",
                    mes,
                    anio,
                    detalle = "Se generaron alquileres y deudas recurrentes (Luz, Agua, Limpieza, etc.)"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al generar todas las deudas: {ex.Message}" });
            }
        }

        [HttpPost("generarDeudaEspecifica")]
        [Authorize(Roles = "Admin")]
        public ActionResult GenerarDeudaEspecifica(int idTipoDeuda, int mes, int anio)
        {
            if (mes < 1 || mes > 12)
                return BadRequest(new { mensaje = "El mes debe estar entre 1 y 12" });

            if (idTipoDeuda <= 0)
                return BadRequest(new { mensaje = "El ID del tipo de deuda es inválido" });

            try
            {
                _deudaDao.GenerarDeudaEspecifica(idTipoDeuda, mes, anio);
                string[] meses = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", 
                                   "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
                return Ok(new 
                { 
                    mensaje = $"Deuda específica de {meses[mes]} {anio} generada correctamente",
                    idTipoDeuda,
                    mes,
                    anio
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al generar deuda específica: {ex.Message}" });
            }
        }

        [HttpPost("generarDeudasParaPuesto")]
        [Authorize(Roles = "Admin")]
        public ActionResult GenerarDeudasParaPuesto(int idPuesto, int mes, int anio)
        {
            if (mes < 1 || mes > 12)
                return BadRequest(new { mensaje = "El mes debe estar entre 1 y 12" });

            if (idPuesto <= 0)
                return BadRequest(new { mensaje = "El ID del puesto es inválido" });

            try
            {
                _deudaDao.GenerarDeudasParaPuesto(idPuesto, mes, anio);
                string[] meses = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", 
                                   "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
                return Ok(new 
                { 
                    mensaje = $"Deudas de {meses[mes]} {anio} generadas correctamente para el puesto",
                    idPuesto,
                    mes,
                    anio
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al generar deudas para el puesto: {ex.Message}" });
            }
        }

        [HttpPost("generarDeudasParaPuestos")]
        [Authorize(Roles = "Admin")]
        public ActionResult GenerarDeudasParaPuestos([FromQuery] string idPuestos, int mes, int anio)
        {
            if (mes < 1 || mes > 12)
                return BadRequest(new { mensaje = "El mes debe estar entre 1 y 12" });

            if (string.IsNullOrWhiteSpace(idPuestos))
                return BadRequest(new { mensaje = "Debe proporcionar al menos un ID de puesto" });

            try
            {
                _deudaDao.GenerarDeudasParaPuestos(idPuestos, mes, anio);
                string[] meses = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", 
                                   "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
                return Ok(new 
                { 
                    mensaje = $"Deudas de {meses[mes]} {anio} generadas correctamente para los puestos especificados",
                    puestos = idPuestos,
                    mes,
                    anio
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al generar deudas para los puestos: {ex.Message}" });
            }
        }
    }
}
