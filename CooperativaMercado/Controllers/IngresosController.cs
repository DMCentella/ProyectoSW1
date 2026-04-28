using CooperativaMercado.Model;
using CooperativaMercado.Model;
using CooperativaMercado.Repository.Dao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CooperativaMercado.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngresosController : ControllerBase
    {
        private readonly IngresoDao _ingresoDao;
        private readonly PuestoDao _puestoDao;

        public IngresosController(IngresoDao ingresoDao, PuestoDao puestoDao)
        {
            _ingresoDao = ingresoDao;
            _puestoDao = puestoDao;
        }

        [HttpPost("registrar")]
        [Authorize(Roles = "Socio")]
        public ActionResult Registrar([FromBody] RegistrarIngresoRequest request)
        {

            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            var idSocioClaim = User.FindFirst("IdSocio")?.Value;

            if (string.IsNullOrEmpty(idUsuarioClaim) || !int.TryParse(idUsuarioClaim, out int idUsuario))
                return BadRequest(new { mensaje = "No se pudo obtener el ID del usuario" });

            if (string.IsNullOrEmpty(idSocioClaim) || !int.TryParse(idSocioClaim, out int idSocio))
                return BadRequest(new { mensaje = "No se pudo obtener el ID del socio" });

            if (!_puestoDao.PuestoPerteneceSocio(request.IdPuesto, idSocio))
                return Forbid(); 
            var ingreso = new IngresoDiario
            {
                IdPuesto = request.IdPuesto,
                Fecha = request.Fecha,
                Monto = request.Monto,
                IdUsuario = idUsuario
            };

            _ingresoDao.RegistrarIngreso(ingreso);
            return Ok(new { mensaje = "Ingreso registrado exitosamente" });
        }

        [HttpPost("registrarAdmin")]
        [Authorize(Roles = "Admin")]
        public ActionResult RegistrarAdmin([FromBody] IngresoDiario ingreso)
        {
            _ingresoDao.RegistrarIngreso(ingreso);
            return Ok(new { mensaje = "Ingreso registrado exitosamente" });
        }

        [HttpGet("getIngresos")]
        [Authorize(Roles = "Admin")]
        public ActionResult getIngresos()
        {
            return Ok(_ingresoDao.Listar());
        }

        [HttpGet("misIngresos")]
        [Authorize(Roles = "Socio")]
        public ActionResult MisIngresos([FromQuery] DateTime? inicio = null, [FromQuery] DateTime? fin = null)
        {
            var idSocioClaim = User.FindFirst("IdSocio")?.Value;
            if (string.IsNullOrEmpty(idSocioClaim) || !int.TryParse(idSocioClaim, out int idSocio))
                return BadRequest("No se pudo obtener el ID del socio");

            var fechaInicio = inicio ?? DateTime.Today.AddMonths(-1);
            var fechaFin = fin ?? DateTime.Today;

            var lista = _ingresoDao.ObtenerPorSocio(idSocio, fechaInicio, fechaFin);

            if (lista == null || !lista.Any())
                return Ok(new
                {
                    mensaje = "No hay ingresos registrados en el rango de fechas",
                    fechaInicio,
                    fechaFin,
                    ingresos = new List<IngresoDiario>()
                });

            var agrupado = lista.GroupBy(i => i.NumeroPuesto)
                                .Select(g => new
                                {
                                    puesto = g.Key,
                                    ingresos = g.OrderByDescending(i => i.Fecha).ToList(),
                                    totalPuesto = g.Sum(i => i.Monto)
                                });

            return Ok(new
            {
                fechaInicio,
                fechaFin,
                totalGeneral = lista.Sum(i => i.Monto),
                detallesPorPuesto = agrupado
            });
        }

        [HttpGet("reporteIngresos")]
        [Authorize(Roles = "Admin")]
        public ActionResult reporteIngresos(DateTime inicio, DateTime fin)
        {
            if (inicio > fin)
                return BadRequest(new { mensaje = "La fecha inicio no puede ser mayor que la fecha fin" });

            return Ok(new
            {
                inicio,
                fin,
                total = _ingresoDao.TotalIngresosPorRango(inicio, fin)
            });
        }

        [HttpGet("reporteIngresosPuesto")]
        [Authorize(Roles = "Admin")]
        public ActionResult reporteIngresosPuesto(DateTime inicio, DateTime fin)
        {
            if (inicio > fin)
                return BadRequest(new { mensaje = "Rango de fechas inválido" });

            return Ok(_ingresoDao.ReporteIngresosPorPuesto(inicio, fin));
        }
    }
}
