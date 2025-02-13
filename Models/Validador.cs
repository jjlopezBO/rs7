using Oracle.ManagedDataAccess.Client;

public class Validador
{
    public long IdElemento { get; set; }
    public string Tipo { get; set; }
    public int Maximo { get; set; }
    public int Minimo { get; set; }

    public Validador(long idElemento, string tipo, int maximo, int minimo)
    {
        IdElemento = idElemento;
        Tipo = tipo;
        Maximo = maximo;
        Minimo = minimo;

    }


    public static List<Validador> GetStatus(string tabla)
    {
        List<Validador> validadores = new List<Validador>();
        using (OracleCommand command = OracleService.Instance.GetCommand())
        {
            command.CommandText = @$"select id,
       type,
       max_value,
       min(max_value)
       over() as min_value
  from (
   select id,
          type,
          max(intervalo) as max_value
     from {tabla}
    where fecha_sng = trunc(sysdate)
    group by id,
             type
)";

            using (OracleDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var validador = new Validador(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3));
                    validadores.Add(validador);
                }
            }
        }

        return validadores;
    }
}