using OSKHelpers.Logging;
using System;
using System.Reflection;
using System.Text;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Generic utilities applicable to any object type.
    /// </summary>
    public class ObjectUtils
    {
        /// <summary>
        /// Dumps all public properties of the object (with their values and types) to the console.<br/>
        /// When <paramref name="log"/> is true, the object contents are also written to the default log.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="obj">Object to dump.</param>
        /// <param name="objName">Name of the object; omitted when null.</param>
        /// <param name="log">When true, the object contents are written to the default log.</param>
        /// <param name="logLevel">Minimum log level for the object contents to be analysed.</param>
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
        /// Returns true if the given object is of a numeric type.
        /// </summary>
        /// <param name="obj">Object to inspect.</param>
        /// <returns>True if the object is of a numeric type.</returns>
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
