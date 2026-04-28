using CooperativaMercado.Model;
using CooperativaMercado.Repository.Dao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CooperativaMercado.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SociosController : ControllerBase
    {
        private readonly SocioDao _socioDao;

        public SociosController(SocioDao socioDao)
        {
            _socioDao = socioDao;
        }

        [HttpGet("getSocios")]
        public ActionResult getSocios()
        {
            return Ok(_socioDao.Listar());
        }

        [HttpGet("getSocio/{id}")]
        public ActionResult getSocio(int id)
        {
            var socio = _socioDao.ObtenerPorId(id);

            if (socio == null)
                return NotFound();

            return Ok(socio);
        }

        [HttpPost("saveSocio")]
        public ActionResult saveSocio(Socio socio)
        {
            if (socio == null)
                return BadRequest("Los datos del socio son requeridos");
            try
            {
                _socioDao.Registrar(socio);
                return Created("", socio);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al registrar socio: {ex.Message}");
            }
        }

        [HttpPut("updateSocio")]
        public ActionResult updateSocio(Socio socio)
        {
            if (socio == null)
                return BadRequest("Datos inválidos para actualizar");
            try
            {
                _socioDao.Actualizar(socio);
                return Ok(socio);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al actualizar socio: {ex.Message}");
            }
        }

        [HttpGet("validarRetiro/{id}")]
        public ActionResult ValidarRetiro(int id)
        {
            try
            {
                bool puedeRetirarse = _socioDao.PuedeRetirarse(id);
                return Ok(new
                {
                    idSocio = id,
                    puedeRetirarse,
                    mensaje = puedeRetirarse
                        ? "El socio puede retirarse, no tiene deudas pendientes"
                        : "El socio NO puede retirarse, tiene deudas pendientes"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al validar retiro: {ex.Message}");
            }
        }

        [HttpPost("retirar/{idSocio}")]
        public ActionResult RetirarSocio(int idSocio, [FromQuery] string motivoRetiro)
        {
            try
            {
  
                bool puedeRetirarse = _socioDao.PuedeRetirarse(idSocio);

                if (!puedeRetirarse)
                {
                    return BadRequest(new
                    {
                        mensaje = "El socio NO puede retirarse, tiene deudas pendientes",
                        idSocio
                    });
                }


                _socioDao.RetirarSocio(idSocio, motivoRetiro);

                return Ok(new
                {
                    mensaje = "Socio retirado exitosamente",
                    idSocio,
                    motivoRetiro,
                    acciones = new
                    {
                        puestosLiberados = true,
                        historialCerrado = true,
                        socioDesactivado = true,
                        usuarioDesactivado = true
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al retirar socio: {ex.Message}");
            }
        }

        [HttpPost("reactivar/{id}")]
        public ActionResult ReactivarSocio(int id)
        {
            try
            {
                _socioDao.ReactivarSocio(id);

                return Ok(new
                {
                    mensaje = "Socio reactivado exitosamente",
                    idSocio = id,
                    acciones = new
                    {
                        socioReactivado = true,
                        usuarioReactivado = true
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al reactivar socio: {ex.Message}");
            }
        }
    }
}
