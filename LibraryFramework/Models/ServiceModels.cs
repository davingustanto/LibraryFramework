using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryFramework.Models
{
    public class ParameterQueryString
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public SqlDbType DbType { get; set; }
    }

    public class State
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }

    public class ValidationModel
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<State> ModelErrors { get; set; }
    }
}
