using OSKHelpers.Logging;
using System;
using System.Reflection;
using System.Text;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilità generiche applicabili a qualsiasi tipo di oggetto.
    /// </summary>
    public class ObjectUtils
    {
        /// <summary>
        /// Visualizza a video tutte le proprietà pubbliche, con relativo valore e tipologia, dell'oggetto.<br/>
        /// Se <paramref name="log"/> è true il contenuto dell'oggetto sarà salvato nel log di default.
        /// </summary>
        /// <typeparam name="T">Tipologia dell'oggetto.</typeparam>
        /// <param name="obj">Oggetto di cui effettuare il dump.</param>
        /// <param name="objName">Nome dell'oggetto, se venisse passato null sarà omesso.</param>
        /// <param name="log">Se true il contenuto dell'oggetto sarà salvato nel log di default.</param>
        /// <param name="logLevel">Livello di log minimo perché il contenuto dell'oggetto sia analizzato.</param>
        public static void Dump<T>(T obj, string objName = null, bool log = false, LogLevel logLevel = LogLevel.Error)
        {
            if (SimpleLog.ToLog(logLevel))
            {
                var isInteractive = OSKEnvironment.IsInteractive;

                if (isInteractive || log)
                {
                    try
                    {
                        var typeName = typeof(T).Name;
                        var sb = new StringBuilder();

                        if (obj == null)
                        {
                            sb.Append($"{typeName}: null");
                        }
                        else
                        {
                            Type type = obj.GetType();
                            sb.AppendLine($"{(string.IsNullOrWhiteSpace(objName) ? "Tipo" : objName.Trim())}: {typeName}");

                            foreach (PropertyInfo property in type.GetProperties())
                            {
                                object value = null;
                                try
                                {
                                    value = property.GetValue(obj);
                                }
                                catch (Exception ex)
                                {
                                    value = $"#ERROR# {ex.Message}";
                                }
                                sb.AppendLine($"{property.Name} ({property.PropertyType.Name}) = {value}");
                            }
                        }

                        var strOutput = sb.ToString();
                        if (OSKEnvironment.IsInteractive)
                        {
                            Console.WriteLine(strOutput);
                        }

                        if (log)
                        {
                            SimpleLog.Write(strOutput);
                        }
                    }
                    catch(Exception ex)
                    {
                        SimpleLog.LogError(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Restituisce true se l'oggetto passato come parametro è di tipo numerico.
        /// </summary>
        /// <param name="obj">Oggetto da analizzare.</param>
        /// <returns>True se l'oggetto è di tipo numerico.</returns>
        public static bool IsNumeric(object obj)
        {
            var res = false;
            if (obj != null)
            {
                var typeCode = Type.GetTypeCode(obj.GetType());
                return typeCode == TypeCode.Byte    || typeCode == TypeCode.SByte   ||
                       typeCode == TypeCode.UInt16  || typeCode == TypeCode.UInt32  ||
                       typeCode == TypeCode.UInt64  || typeCode == TypeCode.Int16   ||
                       typeCode == TypeCode.Int32   || typeCode == TypeCode.Int64   ||
                       typeCode == TypeCode.Decimal || typeCode == TypeCode.Double  ||
                       typeCode == TypeCode.Single;
            }

            return res;
        }
    }
}
