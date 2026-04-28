using CooperativaMercado.Model;
using CooperativaMercado.Repository.Dao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CooperativaMercado.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PuestosController : ControllerBase
    {
        private readonly PuestoDao _puestoDao;
        private readonly HistorialPuestoDao _historialDao;

        public PuestosController(PuestoDao puestoDao, HistorialPuestoDao historialDao)
        {
            _puestoDao = puestoDao;
            _historialDao = historialDao;
        }
       
        [HttpGet("getPuestos")] 
        [Authorize(Roles = "Admin")]
        public ActionResult getPuestos()
        {
            var lista = _puestoDao.Listar();
            if (lista == null || !lista.Any())
                return NoContent();

            return Ok(lista);
        }
        
        [HttpPost("savePuesto")]
        [Authorize(Roles = "Admin")]
        public ActionResult savePuesto(Puesto puesto)
        {
            if (puesto == null)
                return BadRequest("Los datos del puesto son requeridos");

            try
            {
                _puestoDao.Registrar(puesto);
                return Created("", puesto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error en el servidor: {ex.Message}");
            }
        }
        
        [HttpPut("updatePuesto")]
        [Authorize(Roles = "Admin")]
        public ActionResult updatePuesto(Puesto puesto)
        {
            if (puesto == null)
                return BadRequest("Datos invalidos para actualizar");

            try
            {
                _puestoDao.Actualizar(puesto);
                return Ok(puesto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar: {ex.Message}");
            }
        }
       
        [HttpPut("asignar")]
        [Authorize(Roles = "Admin")]
        public ActionResult asignar(int idPuesto, int idSocio)
        {
            try
            {
                _puestoDao.AsignarSocio(idPuesto, idSocio);
                return Ok("Asignacion realizada con exito");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error en la asignacion: {ex.Message}");
            }
        }
        
        [HttpPut("desasignar")]
        [Authorize(Roles = "Admin")]
        public ActionResult desasignar(int idPuesto, [FromQuery] string? motivo = null)
        {
            try
            {
                _puestoDao.DesasignarSocio(idPuesto, motivo);

                string mensaje = string.IsNullOrEmpty(motivo) 
                    ? "Socio desasignado correctamente" 
                    : $"Socio desasignado. Motivo: {motivo}";

                return Ok(mensaje);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al desasignar: {ex.Message}");
            }
        }
     
        [HttpGet("historial/{idPuesto}")]  
        [Authorize(Roles = "Admin")]
        public ActionResult ObtenerHistorial(int idPuesto)
        {
            try
            {
                var historial = _historialDao.ObtenerPorPuesto(idPuesto);
                if (historial == null || !historial.Any())
                    return Ok(new { mensaje = "No hay historial para este puesto", historial = new List<object>() });

                return Ok(historial);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al obtener historial: {ex.Message}");
            }
        }

        [HttpGet("misPuestos")]
        [Authorize(Roles = "Socio")]
        public ActionResult ObtenerMisPuestos()
        {
            try
            {
                // Debug: Ver todos los claims
                var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

                // Obtener el IdSocio del token JWT
                var idSocioClaim = User.Claims.FirstOrDefault(c => c.Type == "IdSocio");

                if (idSocioClaim == null || string.IsNullOrEmpty(idSocioClaim.Value))
                {
                    return BadRequest(new
                    {
                        mensaje = "No se pudo obtener el ID del socio del token",
                        claims = allClaims // Para debug
                    });
                }

                int idSocio;
                if (!int.TryParse(idSocioClaim.Value, out idSocio))
                {
                    return BadRequest(new
                    {
                        mensaje = "El ID del socio no es válido",
                        idSocioValue = idSocioClaim.Value
                    });
                }

                // Obtener los puestos del socio
                var puestos = _puestoDao.ListarPorSocio(idSocio);

                if (puestos == null || !puestos.Any())
                {
                    return Ok(new List<Puesto>());
                }

                return Ok(puestos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener puestos: {ex.Message}");
            }
        }
    }
}
