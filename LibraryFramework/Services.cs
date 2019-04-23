using LibraryFramework.Models;
using LibraryFramework.Models.Paging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Http.Filters;
using System.Web.Mvc;

namespace LibraryFramework
{
    public class Services : IDisposable
    {
        private const int Keysize = 128;
        private const int DerivationIterations = 1000;
        static string[] roman1 = { "MMM", "MM", "M" };
        static string[] roman2 = { "CM", "DCCC", "DCC", "DC", "D", "CD", "CCC", "CC", "C" };
        static string[] roman3 = { "XC", "LXXX", "LXX", "LX", "L", "XL", "XXX", "XX", "X" };
        static string[] roman4 = { "IX", "VIII", "VII", "VI", "V", "IV", "III", "II", "I" };
        private static Random random = new Random();

        public Services()
        {

        }

        #region Security Encrypt Decrypt
        public static string Encrypt(string plainText, string passPhrase)
        {
            var saltStringBytes = Generate128BitsOfRandomEntropy();
            var ivStringBytes = Generate128BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 128;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 128;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }

        private static byte[] Generate128BitsOfRandomEntropy()
        {
            var randomBytes = new byte[16];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        public static string Base64Encode(string sData)
        {
            try
            {
                byte[] numArray = new byte[sData.Length];
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(sData));
            }
            catch (Exception ex)
            {
                throw new Exception("Error in base64Encode" + ex.Message);
            }
        }

        public static string Base64Decode(string sData)
        {
            try
            {
                Decoder decoder = new UTF8Encoding().GetDecoder();
                byte[] numArray = Convert.FromBase64String(sData);
                byte[] bytes1 = numArray;
                int index = 0;
                int length1 = numArray.Length;
                char[] chArray = new char[decoder.GetCharCount(bytes1, index, length1)];
                byte[] bytes2 = numArray;
                int byteIndex = 0;
                int length2 = numArray.Length;
                char[] chars = chArray;
                int charIndex = 0;
                decoder.GetChars(bytes2, byteIndex, length2, chars, charIndex);
                return new string(chArray);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in base64Decode" + ex.Message);
            }
        }
        
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz/?'()^$%!#";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion

        public static DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Defining type of data column gives proper data table 
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name, type);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }

        public static bool IsConnectionValid(string connectionString)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetIPAddress(string HostNameAddress)
        {
            string IPv4 = string.Empty;
            foreach (IPAddress IPAdd in Dns.GetHostAddresses(HostNameAddress))
            {
                if (IPAdd.AddressFamily.ToString() == "InterNetwork")
                {
                    IPv4 = IPAdd.ToString();
                    break;
                }
            }

            if (IPv4 != string.Empty)
                return IPv4;

            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily.ToString() == "InterNetwork")
                {
                    IPv4 = IPA.ToString();
                    break;
                }
            }
            return IPv4;
        }

        public static string ToRoman(int num)
        {
            if (num > 3999) throw new ArgumentException("Too big - can't exceed 3999");
            if (num < 1) throw new ArgumentException("Too small - can't be less than 1");
            int thousands, hundreds, tens, units;
            thousands = num / 1000;
            num %= 1000;
            hundreds = num / 100;
            num %= 100;
            tens = num / 10;
            units = num % 10;
            var sb = new StringBuilder();
            if (thousands > 0) sb.Append(roman1[3 - thousands]);
            if (hundreds > 0) sb.Append(roman2[9 - hundreds]);
            if (tens > 0) sb.Append(roman3[9 - tens]);
            if (units > 0) sb.Append(roman4[9 - units]);
            return sb.ToString();
        }

        public virtual bool IsFileClose(FileInfo file_info)
        {
            FileStream stream = null;
            try
            {
                stream = file_info.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return false;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return true;
        }

        public static string GenerateAutoNumber(int CountData, int LengthNumber)
        {
            string number = "";

            for (int i = 0; i < LengthNumber; i++)
            {
                number = number + "0";
            }

            int length_temp = 0;
            int.TryParse(number.Substring(0, 1).Replace("0", "1") + number.Substring(1, (number.Length - 1)), out length_temp);

            if (CountData == 0)
                number = number.Substring(0, (number.Length - 1)) + number.Substring((number.Length - 1), 1).Replace("0", "1");
            else
            {
                for (int i = length_temp; i > (CountData - 1); i--)
                {
                    if (i == CountData)
                    {
                        string initial = "";
                        for (int j = 0; j < i.ToString().Length; j++)
                        {
                            initial = initial + "0";
                        }
                        bool is_additional_length = ((i + 1).ToString().Length > i.ToString().Length);
                        number = number.Substring(0, (number.Length - (is_additional_length ? (i.ToString().Length + 1) : i.ToString().Length))) +
                            number.Substring((number.Length - i.ToString().Length), i.ToString().Length).Replace(initial, (i + 1).ToString());
                    }
                }
            }
            return number;
        }

        public static byte[] GzipByte(byte[] str)
        {
            if (str == null)
                return null;

            using (var output = new MemoryStream())
            {
                using (var compressor = new Ionic.Zlib.GZipStream(output, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestSpeed))
                {
                    compressor.Write(str, 0, str.Length);
                }
                return output.ToArray();
            }
        }

        public void ConvertToPDF(string source_document, string path_libre, string destination_output)
        {
            string exe_sOffice_path = path_libre + "\\App\\libreoffice\\program\\";
            string file_name_id = Guid.NewGuid().ToString();
            byte[] data;
            using (WebClient client = new WebClient())
            {
                data = client.DownloadData(source_document);
            }
            File.WriteAllBytes(exe_sOffice_path + "temp\\" + file_name_id, data);
            var pdfProcess = new Process();
            pdfProcess.StartInfo.FileName = "soffice.exe";
            pdfProcess.StartInfo.Arguments = "-norestore -nofirststartwizard -headless -convert-to pdf  \"temp\\" + file_name_id + "\" -outdir \"converted\"";
            pdfProcess.StartInfo.WorkingDirectory = exe_sOffice_path;
            pdfProcess.Start();
            pdfProcess.WaitForExit();
            File.Copy(exe_sOffice_path + "converted\\" + file_name_id + ".pdf", destination_output);
        }

        public static string TranslateDay(string day_origin, string culture = null)
        {
            DayOfWeek day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), day_origin);
            var cultureInfo = new CultureInfo((string.IsNullOrEmpty(culture)) ? "id-ID" : culture);
            var dateTimeInfo = cultureInfo.DateTimeFormat;
            return dateTimeInfo.GetDayName(day);
        }

        public static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }

        private static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name == column.ColumnName)
                        pro.SetValue(obj, (dr[column.ColumnName] != System.DBNull.Value) ? dr[column.ColumnName] : null, null);
                    else
                        continue;
                }
            }
            return obj;
        }

        public static DateTime UtcToLocalTime(DateTime date_input)
        {
            DateTime runtimeKnowsThisIsUtc = DateTime.SpecifyKind(date_input, DateTimeKind.Utc);
            return runtimeKnowsThisIsUtc.ToLocalTime();
        }

        public static DateTime UtcToLocalTime(DateTime date_input, int date_offset)
        {
            return date_input.AddHours(date_offset);
        }

        public static string GetMimeTypeByWindowsRegistry(string fileNameOrExtension)
        {
            string mimeType = "application/unknown";
            string ext = (fileNameOrExtension.Contains(".")) ? Path.GetExtension(fileNameOrExtension).ToLower() : "." + fileNameOrExtension;
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null) mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }

        public void Dispose()
        {

        }
    }

    public class Connection : IDisposable
    {
        private SqlDataAdapter adapter;
        private SqlCommand cmd;
        private SqlConnection con;
        private DataTable dt;
        private SqlDataReader dataReader;
        private string strConnection;

        public Connection(string connectionString)
        {
            if (!Services.IsConnectionValid(connectionString))
                throw new Exception("Cannot connect to Database Server.");
            strConnection = connectionString;
            con = new SqlConnection(connectionString);
            if (con.State == ConnectionState.Open)
                con.Close();
        }

        public bool IsTableExist(string tableName)
        {
            DataTable dt_check = OpenDataTable("SELECT CASE WHEN EXISTS((SELECT * FROM information_schema.tables WHERE table_name = '" + tableName + "')) THEN 1 ELSE 0 END");
            return int.Parse(dt_check.Rows[0][0].ToString()) == 1;
        }

        public DataTable OpenDataTable(string queryString, List<ParameterQueryString> parametersString = null)
        {
            try
            {
                con.Open();
                dt = new DataTable();
                dt.Clear();
                adapter = new SqlDataAdapter(queryString, strConnection);
                if (parametersString != null)
                {
                    foreach (var item in parametersString)
                    {
                        adapter.SelectCommand.Parameters.Add(new SqlParameter
                        {
                            ParameterName = item.Name,
                            Value = (item.Value == null ? DBNull.Value : item.Value)
                        });
                    }
                }
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            con.Close();
            return dt;
        }

        public void ExecuteQuery(string queryString, List<ParameterQueryString> parametersString = null)
        {
            try
            {
                con.Open();
                cmd = new SqlCommand(queryString, con);
                if (parametersString != null)
                {
                    foreach (var item in parametersString)
                    {
                        cmd.Parameters.Add(new SqlParameter
                        {
                            ParameterName = item.Name,
                            SqlDbType = SetDbType(item.Value),
                            Value = (item.Value == null ? DBNull.Value : item.Value)
                        });
                    }
                }
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            con.Close();
        }

        public SqlDbType SetDbType(object value_object)
        {
            var sql_db_type = new SqlDbType();
            if (value_object.GetType().Name == "String")
                sql_db_type = SqlDbType.VarChar;
            else if (value_object.GetType().Name == "Int32" || value_object.GetType().Name == "Int64")
                sql_db_type = SqlDbType.Int;
            else if (value_object.GetType().Name == "Boolean")
                sql_db_type = SqlDbType.Bit;
            return sql_db_type;
        }

        public void ExecuteStoredProcedure(string procedureName, List<ParameterQueryString> parameterQueryStrings = null)
        {
            try
            {
                cmd = new SqlCommand();
                cmd.CommandText = procedureName;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = con;
                foreach (var item in parameterQueryStrings)
                {
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = item.Name,
                        SqlDbType = item.DbType,
                        Value = item.Value
                    });
                }
                con.Open();
                dataReader = cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string SetValueNull(string key)
        {
            int result;
            key = int.TryParse(key, out result) ? key : "'" + key + "'";
            key = string.IsNullOrEmpty(key) || key == "''" ? "NULL" : key;
            return key;
        }

        public void Dispose()
        {
            if (con.State == ConnectionState.Open)
                con.Close();
            dt.Clear();
        }
    }

    public partial class UrlFormat
    {
        public static string Encode(string encodeParam)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(encodeParam);
            return Convert.ToBase64String(encoded);
        }
        public static string Decode(string param)
        {
            byte[] decoded = Convert.FromBase64String(param);
            return Encoding.UTF8.GetString(decoded);
        }
    }

    #region Password Storage

    class InvalidHashException : Exception
    {
        public InvalidHashException() { }
        public InvalidHashException(string message)
            : base(message) { }
        public InvalidHashException(string message, Exception inner)
            : base(message, inner) { }
    }

    class CannotPerformOperationException : Exception
    {
        public CannotPerformOperationException() { }
        public CannotPerformOperationException(string message)
            : base(message) { }
        public CannotPerformOperationException(string message, Exception inner)
            : base(message, inner) { }
    }

    class PasswordStorage
    {
        // These constants may be changed without breaking existing hashes.
        public const int SALT_BYTES = 24;
        public const int HASH_BYTES = 18;
        public const int PBKDF2_ITERATIONS = 64000;

        // These constants define the encoding and may not be changed.
        public const int HASH_SECTIONS = 2;
        //public const int HASH_ALGORITHM_INDEX = 0;
        //public const int ITERATION_INDEX = 0;
        //public const int HASH_SIZE_INDEX = 1;
        public const int SALT_INDEX = 0;
        public const int PBKDF2_INDEX = 1;

        public static string CreateHash(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[SALT_BYTES];
            try
            {
                using (RNGCryptoServiceProvider csprng = new RNGCryptoServiceProvider())
                {
                    csprng.GetBytes(salt);
                }
            }
            catch (CryptographicException ex)
            {
                throw new CannotPerformOperationException(
                    "Random number generator not available.",
                    ex
                );
            }
            catch (ArgumentNullException ex)
            {
                throw new CannotPerformOperationException(
                    "Invalid argument given to random number generator.",
                    ex
                );
            }

            byte[] hash = PBKDF2(password, salt, PBKDF2_ITERATIONS, HASH_BYTES);

            // format: algorithm:iterations:hashSize:salt:hash
            String parts =
                Convert.ToBase64String(salt) +
                ":" +
                Convert.ToBase64String(hash);
            return parts;
        }

        public static bool VerifyPassword(string password, string goodHash)
        {
            char[] delimiter = { ':' };
            string[] split = goodHash.Split(delimiter);

            if (split.Length != HASH_SECTIONS)
            {
                throw new InvalidHashException(
                    "Fields are missing from the password hash."
                );
            }

            // We only support SHA1 with C#.
            //if (split[HASH_ALGORITHM_INDEX] != "sha1")
            //{
            //    throw new CannotPerformOperationException(
            //        "Unsupported hash type."
            //    );
            //}

            int iterations = PBKDF2_ITERATIONS;
            //try
            //{
            //    iterations = Int32.Parse(split[ITERATION_INDEX]);
            //}
            //catch (ArgumentNullException ex)
            //{
            //    throw new CannotPerformOperationException(
            //        "Invalid argument given to Int32.Parse",
            //        ex
            //    );
            //}
            //catch (FormatException ex)
            //{
            //    throw new InvalidHashException(
            //        "Could not parse the iteration count as an integer.",
            //        ex
            //    );
            //}
            //catch (OverflowException ex)
            //{
            //    throw new InvalidHashException(
            //        "The iteration count is too large to be represented.",
            //        ex
            //    );
            //}

            //if (iterations < 1)
            //{
            //    throw new InvalidHashException(
            //        "Invalid number of iterations. Must be >= 1."
            //    );
            //}

            byte[] salt = null;
            try
            {
                salt = Convert.FromBase64String(split[SALT_INDEX]);
            }
            catch (ArgumentNullException ex)
            {
                throw new CannotPerformOperationException(
                    "Invalid argument given to Convert.FromBase64String",
                    ex
                );
            }
            catch (FormatException ex)
            {
                throw new InvalidHashException(
                    "Base64 decoding of salt failed.",
                    ex
                );
            }

            byte[] hash = null;
            try
            {
                hash = Convert.FromBase64String(split[PBKDF2_INDEX]);
            }
            catch (ArgumentNullException ex)
            {
                throw new CannotPerformOperationException(
                    "Invalid argument given to Convert.FromBase64String",
                    ex
                );
            }
            catch (FormatException ex)
            {
                throw new InvalidHashException(
                    "Base64 decoding of pbkdf2 output failed.",
                    ex
                );
            }

            int storedHashSize = HASH_BYTES;
            //try
            //{
            //    storedHashSize = Int32.Parse(split[HASH_SIZE_INDEX]);
            //}
            //catch (ArgumentNullException ex)
            //{
            //    throw new CannotPerformOperationException(
            //        "Invalid argument given to Int32.Parse",
            //        ex
            //    );
            //}
            //catch (FormatException ex)
            //{
            //    throw new InvalidHashException(
            //        "Could not parse the hash size as an integer.",
            //        ex
            //    );
            //}
            //catch (OverflowException ex)
            //{
            //    throw new InvalidHashException(
            //        "The hash size is too large to be represented.",
            //        ex
            //    );
            //}

            if (storedHashSize != hash.Length)
            {
                throw new InvalidHashException(
                    "Hash length doesn't match stored hash length."
                );
            }

            byte[] testHash = PBKDF2(password, salt, iterations, hash.Length);
            return SlowEquals(hash, testHash);
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }

        private static byte[] PBKDF2(string password, byte[] salt, int iterations, int outputBytes)
        {
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt))
            {
                pbkdf2.IterationCount = iterations;
                return pbkdf2.GetBytes(outputBytes);
            }
        }
    }

    #endregion

    public class Generator : IDisposable
    {
        public static void InterfaceReact<T>(string destinationPath, T model)
        {
            List<string> field_types = new List<string>();
            List<string> field_list = new List<string>();
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            string string_build = "export interface " + typeof(T).Name + " { ";
            for (int i = 0; i < Props.Length; i++)
            {
                field_types.Add(Props[i].PropertyType.FullName.ToLower());
                string field_name = Props[i].Name.Replace(Props[i].Name.Substring(0, 1), Props[i].Name.Substring(0, 1).ToLower());
                string field_type = "";

                if (Props[i].PropertyType.FullName.ToLower().Contains("int") || Props[i].PropertyType.FullName.ToLower().Contains("decimal"))
                    field_type = "number";
                else if (Props[i].PropertyType.FullName.ToLower().Contains("string"))
                    field_type = "string";
                else if (Props[i].PropertyType.FullName.ToLower().Contains("datetime"))
                    field_type = "Date";
                else if (Props[i].PropertyType.FullName.ToLower().Contains("boolean"))
                    field_type = "boolean";
                else if (Props[i].PropertyType.FullName.ToLower().Contains("icollection") || Props[i].PropertyType.FullName.ToLower().Contains("list"))
                    field_type = "[]";
                else
                    field_type = "{}";

                if (Props[i].PropertyType.FullName.ToLower().Contains("null"))
                    field_type = field_type + " | null";

                field_list.Add(field_name + ": " + field_type);
            }
            string_build = string_build + string.Join(",\n", field_list) + " };";
            File.WriteAllText(destinationPath, string_build);
        }

        public void Dispose()
        {

        }
    }

    #region GzipCompression

    public class GzipCompression : System.Web.Http.Filters.ActionFilterAttribute
    {
        byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionContext)
        {
            try
            {
                var content = actionContext.Response.Content;
                var bytes = content == null ? null : content.ReadAsByteArrayAsync().Result;
                var zlibbedContent = bytes == null ? new byte[0] : Services.GzipByte(bytes);
                actionContext.Response.Content = new ByteArrayContent(zlibbedContent);
                actionContext.Response.Content.Headers.Remove("Content-Type");
                actionContext.Response.Content.Headers.Add("Content-encoding", "gzip");
                actionContext.Response.Content.Headers.Add("Content-Type", "application/json");
            }
            catch (Exception ex)
            {
                var content = new { StatusCode = HttpStatusCode.InternalServerError, Message = ex.Message };
                actionContext.Response = new HttpResponseMessage { Content = new ObjectContent(content.GetType(), content, new JsonMediaTypeFormatter()), StatusCode = HttpStatusCode.InternalServerError };
            }
            base.OnActionExecuted(actionContext);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class GenerateResultListFilterAttribute : System.Web.Mvc.FilterAttribute, IResultFilter
    {
        private readonly Type _sourceType;
        private readonly Type _destinationType;

        public GenerateResultListFilterAttribute(Type sourceType, Type destinationType)
        {
            _sourceType = sourceType;
            _destinationType = destinationType;
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {

        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var model = filterContext.Controller.ViewData.Model;
            var resultListGenericType = typeof(ResultList<>).MakeGenericType(new Type[] { _destinationType });

            var queryOptions = filterContext.Controller.ViewData.ContainsKey("QueryOptions") ?
                filterContext.Controller.ViewData["QueryOptions"] :
                new QueryOptions();

            var resultList = Activator.CreateInstance(resultListGenericType, model, queryOptions);

            filterContext.Controller.ViewData.Model = resultList;
        }
    }

    #endregion

    #region Knockout

    public static class HtmlHelperExtensions
    {
        public static HtmlString HtmlConvertToJson(this HtmlHelper htmlHelper, object model)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            return new HtmlString(JsonConvert.SerializeObject(model, settings));
        }

        public static MvcHtmlString BuildKnockoutSortableLink(this HtmlHelper htmlHelper,
        string fieldName, string actionName, string sortField)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);

            return new MvcHtmlString(string.Format(
                "<a href=\"{0}\" data-bind=\"click: pagingService.sortEntitiesBy\"" +
                " data-sort-field=\"{1}\">{2} " +
                "<span data-bind=\"css: pagingService.buildSortIcon('{1}')\"></span></a>",
                urlHelper.Action(actionName),
                sortField,
                fieldName));
        }

        public static MvcHtmlString BuildKnockoutNextPreviousLinks(this HtmlHelper htmlHelper, string actionName)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);

            return new MvcHtmlString(string.Format(
    "<nav>" +
    "    <ul class=\"pager\">" +
    "        <li data-bind=\"css: pagingService.buildPreviousClass()\">" +
    "           <a href=\"{0}\" data-bind=\"click: pagingService.previousPage\"><span class=\"fa fa-caret-left\"></span> Previous</a></li>" +
    "        <li data-bind=\"css: pagingService.buildNextClass()\">" +
    "           <a href=\"{0}\" data-bind=\"click: pagingService.nextPage\">Next <span class=\"fa fa-caret-right\"></span></a></li></li>" +
    "    </ul>" +
    "</nav>",
            @urlHelper.Action(actionName)
            ));
        }

        public static MvcHtmlString BuildSortableLink(this HtmlHelper htmlHelper,
            string fieldName, string actionName, string sortField, QueryOptions queryOptions)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);

            var isCurrentSortField = queryOptions.SortField == sortField;

            return new MvcHtmlString(string.Format("<a href=\"{0}\">{1} {2}</a>",
                urlHelper.Action(actionName,
                new
                {
                    SortField = sortField,
                    SortOrder = (isCurrentSortField
                                && queryOptions.SortOrder == Models.Paging.SortOrder.ASC.ToString())
                            ? Models.Paging.SortOrder.DESC : Models.Paging.SortOrder.ASC
                }),
                fieldName,
                BuildSortIcon(isCurrentSortField, queryOptions)));
        }

        private static string BuildSortIcon(bool isCurrentSortField, QueryOptions queryOptions)
        {
            string sortIcon = "sort";

            if (isCurrentSortField)
            {
                sortIcon += "-by-alphabet";
                if (queryOptions.SortOrder == Models.Paging.SortOrder.DESC.ToString())
                    sortIcon += "-alt";
            }

            return string.Format("<span class=\"{0} {1}{2}\"></span>",
                "glyphicon", "glyphicon-", sortIcon);
        }

        public static MvcHtmlString BuildNextPreviousLinks(this HtmlHelper htmlHelper, QueryOptions queryOptions, string actionName)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);

            return new MvcHtmlString(string.Format(
    "<nav>" +
    "    <ul class=\"pager\">" +
    "        <li class=\"previous {0}\">{1}</li>" +
    "        <li class=\"next {2}\">{3}</li>" +
    "    </ul>" +
    "</nav>",
            IsPreviousDisabled(queryOptions),
            BuildPreviousLink(urlHelper, queryOptions, actionName),
            IsNextDisabled(queryOptions),
            BuildNextLink(urlHelper, queryOptions, actionName)
            ));
        }

        private static string IsPreviousDisabled(QueryOptions queryOptions)
        {
            return (queryOptions.CurrentPage == 1)
                ? "disabled" : string.Empty;
        }

        private static string IsNextDisabled(QueryOptions queryOptions)
        {
            return (queryOptions.CurrentPage == queryOptions.TotalPages)
                ? "disabled" : string.Empty;
        }

        private static string BuildPreviousLink(UrlHelper urlHelper, QueryOptions queryOptions, string actionName)
        {
            return string.Format(
                "<a href=\"{0}\"><span aria-hidden=\"true\">&larr;</span> Previous</a>",
                urlHelper.Action(actionName, new
                {
                    SortOrder = queryOptions.SortOrder,
                    SortField = queryOptions.SortField,
                    CurrentPage = queryOptions.CurrentPage - 1,
                    PageSize = queryOptions.PageSize
                }));
        }

        private static string BuildNextLink(UrlHelper urlHelper, QueryOptions queryOptions, string actionName)
        {
            return string.Format(
                "<a href=\"{0}\">Next <span aria-hidden=\"true\">&rarr;</span></a>",
                urlHelper.Action(actionName, new
                {
                    SortOrder = queryOptions.SortOrder,
                    SortField = queryOptions.SortField,
                    CurrentPage = queryOptions.CurrentPage + 1,
                    PageSize = queryOptions.PageSize
                }));
        }
    }

    #endregion
}
