using CooperativaMercado.Model;
using CooperativaMercado.Repository.Dao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CooperativaMercado.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly UsuarioDAO _usuarioDao;
        private readonly SocioDao _socioDao;
        private readonly PuestoDao _puestoDao;

        public UsuarioController(IConfiguration config, UsuarioDAO usuarioDao, SocioDao socioDao, PuestoDao puestoDao)
        {
            _config = config;
            _usuarioDao = usuarioDao;
            _socioDao = socioDao;
            _puestoDao = puestoDao;
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] Usuario user)
        {
            var usuario = _usuarioDao.ValidarUsuario(user.username, user.password);

            if (usuario == null) return Unauthorized("Usuario o contraseña incorrectos");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, usuario.username),
                new Claim(ClaimTypes.Role, usuario.rol),
                new Claim("IdUsuario", usuario.id.ToString()),
                new Claim("IdSocio", usuario.idSocio?.ToString() ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["llavejwt"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                usuario = usuario.username,
                rol = usuario.rol,
                idSocio = usuario.idSocio
            });
        }

        [HttpPost("crearUsuarioConSocio")]
        [Authorize(Roles = "Admin")]
        public ActionResult CrearUsuarioConSocio([FromBody] CrearUsuarioConSocioRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username y password son obligatorios");

            if (string.IsNullOrWhiteSpace(request.NombreSocio))
                return BadRequest("El nombre del socio es obligatorio");

            try
            {
                var (idUsuario, idSocio) = _usuarioDao.CrearUsuarioConSocio(
                    request.Username,
                    request.Password,
                    request.NombreSocio,
                    request.DNI,
                    request.Telefono
                );

                return Created("", new
                {
                    mensaje = "Usuario y socio creados exitosamente",
                    idUsuario,
                    idSocio,
                    username = request.Username,
                    nombreSocio = request.NombreSocio
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpGet("misDatos")]
        [Authorize(Roles = "Socio")]
        public ActionResult MisDatos()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var usuario = _usuarioDao.ObtenerPorUsername(username);
            if (usuario == null || usuario.idSocio == null)
                return BadRequest("Usuario no vinculado a socio");

            var socio = _socioDao.ObtenerPorId(usuario.idSocio.Value);
            var puestos = _puestoDao.ListarPorSocio(usuario.idSocio.Value);

            return Ok(new
            {
                usuario = new
                {
                    usuario.id,
                    usuario.username,
                    usuario.rol
                },
                socio,
                puestos
            });
        }
    }
}
