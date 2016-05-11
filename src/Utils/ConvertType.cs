using System;
using System.Globalization;

namespace DataAccessFW.Utils
{
    public class ConvertType
    {
        public static short ToShort(object value)
        {
            short rValue = 0;

            if (value == null || value == DBNull.Value || value.ToString() == string.Empty)
                return rValue;

            short.TryParse(value.ToString().Trim(), out rValue);

            return rValue;
        }

        public static int ToInt(object value)
        {
            var rValue = 0;

            if(value == null || value == DBNull.Value ||  value.ToString() == string.Empty)
                return rValue;                

            int.TryParse(value.ToString().Trim(), out rValue);

            return rValue;
        }

        public static long ToLong(object value)
        {
            long rValue = 0;

            if (value == null || value == DBNull.Value || value.ToString() == string.Empty)
                return rValue;

            long.TryParse(value.ToString().Trim(), out rValue);

            return rValue;
        }

        public static DateTime ToDateTime(object value)
        {
            var dt = DateTime.MinValue;

            if (value == null || value == DBNull.Value)
                return dt;

            try
            {
                DateTime.TryParse(value.ToString(), out dt);
            }
            catch
            {
                // ignored
            }

            return dt;
        }

        public static double ToDouble(object valor)
        {
            var numero = 0d;
            if (valor == null || valor == DBNull.Value || valor.ToString() == "")
                return numero;

            var numeroX = valor.ToString().Replace(',', '.');
            double.TryParse(numeroX, NumberStyles.Any, new CultureInfo("pt-BR"), out numero);

            return numero;
        }

        public static bool ToBool(object valor)
        {
            var rValue = false;
            if (valor == null || valor == DBNull.Value || valor.ToString() == "")
                return false;

            try
            {
                bool.TryParse(valor.ToString(), out rValue);
            }
            catch
            {
                // ignored
            }

            return rValue;
        }
    }
}
