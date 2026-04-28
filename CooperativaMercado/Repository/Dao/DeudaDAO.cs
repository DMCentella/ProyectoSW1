using CooperativaMercado.Model;
using CooperativaMercado.Repository.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CooperativaMercado.Repository.Dao
{
    public class DeudaDao : IDeuda
    {
        private readonly string connectionString;

        public DeudaDao()
        {
            connectionString = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetConnectionString("dataBase")!;
        }

        public List<Deuda> Listar()
        {
            List<Deuda> lista = new List<Deuda>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ListarDeudas", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(MapearDeuda(dr));
                }
            }
            return lista;
        }

        public Deuda? ObtenerPorId(int? id)
        {
            Deuda? deuda = null;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ObtenerDeudaPorId", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdDeuda", id);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    deuda = MapearDeuda(dr);
                }
            }
            return deuda;
        }

        public List<Deuda> ObtenerPorPuesto(int idPuesto)
        {
            List<Deuda> lista = new List<Deuda>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ObtenerDeudasPorPuesto", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdPuesto", idPuesto);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(MapearDeuda(dr));
                }
            }
            return lista;
        }

        public List<Deuda> ObtenerPendientes()
        {
            List<Deuda> lista = new List<Deuda>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_DeudasPendientes", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(MapearDeuda(dr));
                }
            }
            return lista;
        }

        public int Registrar(Deuda deuda)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_CrearDeuda", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdPuesto", deuda.IdPuesto);
                cmd.Parameters.AddWithValue("@IdTipoDeuda", deuda.IdTipoDeuda);
                cmd.Parameters.AddWithValue("@Descripcion", (object?)deuda.Descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Monto", deuda.Monto);
                cmd.Parameters.AddWithValue("@Mes", deuda.Mes);
                cmd.Parameters.AddWithValue("@Anio", deuda.Anio);
                cmd.Parameters.AddWithValue("@FechaVencimiento", (object?)deuda.FechaVencimiento ?? DBNull.Value);
                return cmd.ExecuteNonQuery();
            }
        }

        public int GenerarAlquilerMensual(int mes, int anio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_GenerarAlquilerMensual", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Mes", mes);
                cmd.Parameters.AddWithValue("@Anio", anio);
                return cmd.ExecuteNonQuery();
            }
        }

        // MÉTODO PRIVADO PARA MAPEAR
        private Deuda MapearDeuda(SqlDataReader dr)
        {
            return new Deuda()
            {
                IdDeuda = dr.GetInt32(0),
                IdPuesto = dr.GetInt32(1),
                IdTipoDeuda = dr.GetInt32(2), 
                NumeroPuesto = dr.IsDBNull(3) ? null : dr.GetString(3), 
                NombreTipoDeuda = dr.IsDBNull(4) ? null : dr.GetString(4),
                Descripcion = dr.IsDBNull(5) ? null : dr.GetString(5),
                Monto = dr.GetDecimal(6),
                Mora = dr.GetDecimal(7),
                MontoTotal = dr.GetDecimal(8),
                Mes = dr.GetInt32(9),
                Anio = dr.GetInt32(10),
                FechaVencimiento = dr.IsDBNull(11) ? null : dr.GetDateTime(11),
                Estado = dr.GetString(12)
            };
        }

        public Deuda ObtenerPorId(int id)
        {
            throw new NotImplementedException();
        }

        public List<Deuda> ReportePendientes()
        {
            List<Deuda> lista = new List<Deuda>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ReporteDeudasPendientes", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new Deuda()
                    {
                        IdDeuda = dr.GetInt32(0),
                        NumeroPuesto = dr.GetString(1),
                        NombreTipoDeuda = dr.GetString(2),
                        Descripcion = dr.IsDBNull(3) ? null : dr.GetString(3),
                        Monto = dr.GetDecimal(4),
                        Mora = dr.GetDecimal(5),
                        MontoTotal = dr.GetDecimal(6),
                        Mes = dr.GetInt32(7),
                        Anio = dr.GetInt32(8),
                        FechaVencimiento = dr.IsDBNull(9) ? null : dr.GetDateTime(9),
                        Estado = dr.GetString(10)
                    });
                }
            }
            return lista;
        }

        public List<ReporteDeuda> ReportePagadas()
        {
            List<ReporteDeuda> lista = new List<ReporteDeuda>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ReporteDeudasPagadas", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new ReporteDeuda()
                    {
                        IdDeuda = dr.GetInt32(0),
                        Puesto = dr.GetString(1),
                        Socio = dr.IsDBNull(2) ? null : dr.GetString(2),
                        Monto = dr.GetDecimal(3),
                        Mes = dr.GetInt32(4),
                        Anio = dr.GetInt32(5),
                        Estado = dr.GetString(6)
                    });
                }
            }
            return lista;
        }

        public List<Deuda> ObtenerPorSocio(int idSocio)
        {
            List<Deuda> lista = new List<Deuda>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ObtenerDeudasPorSocio", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdSocio", idSocio);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(MapearDeuda(dr));
                }
            }
            return lista;
        }

        public int AplicarMora(int idDeuda, decimal montoMora, int idUsuario)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_AplicarMora", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdDeuda", idDeuda);
                cmd.Parameters.AddWithValue("@MontoMora", montoMora);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                return cmd.ExecuteNonQuery();
            }
        }

        public int GenerarDeudasRecurrentes(int mes, int anio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_GenerarDeudasRecurrentes", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Mes", mes);
                cmd.Parameters.AddWithValue("@Anio", anio);
                return cmd.ExecuteNonQuery();
            }
        }

        public int GenerarTodasLasDeudas(int mes, int anio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_GenerarTodasLasDeudas", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Mes", mes);
                cmd.Parameters.AddWithValue("@Anio", anio);
                return cmd.ExecuteNonQuery();
            }
        }

        public int GenerarDeudaEspecifica(int idTipoDeuda, int mes, int anio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_GenerarDeudaEspecifica", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdTipoDeuda", idTipoDeuda);
                cmd.Parameters.AddWithValue("@Mes", mes);
                cmd.Parameters.AddWithValue("@Anio", anio);
                return cmd.ExecuteNonQuery();
            }
        }

        public int GenerarDeudasParaPuesto(int idPuesto, int mes, int anio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_GenerarDeudasParaPuesto", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdPuesto", idPuesto);
                cmd.Parameters.AddWithValue("@Mes", mes);
                cmd.Parameters.AddWithValue("@Anio", anio);
                return cmd.ExecuteNonQuery();
            }
        }

        public int GenerarDeudasParaPuestos(string idPuestos, int mes, int anio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_GenerarDeudasParaPuestos", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdPuestos", idPuestos);
                cmd.Parameters.AddWithValue("@Mes", mes);
                cmd.Parameters.AddWithValue("@Anio", anio);
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
