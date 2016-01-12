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
            int rValue = 0;

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
            DateTime dt = DateTime.MinValue;

            if (value == null || value == DBNull.Value)
                return dt;

            try
            {
                DateTime.TryParse(value.ToString(), out dt);
            }
            catch { }

            return dt;
        }

        public static double ToDouble(object valor)
        {
            double numero = 0d;
            if (valor == null || valor == DBNull.Value || valor.ToString() == "")
                return numero;

            string numeroX = valor.ToString().Replace(',', '.');
            double.TryParse(numeroX, NumberStyles.Any, new CultureInfo("pt-BR"), out numero);

            return numero;
        }

        public static bool ToBool(object valor)
        {
            bool rValue = false;
            if (valor == null || valor == DBNull.Value || valor.ToString() == "")
                return rValue;

            try
            {
                bool.TryParse(valor.ToString(), out rValue);
            }
            catch
            {

            }
            return rValue;
        }
    }
}
