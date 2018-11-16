using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryFramework;
using LibraryFramework.Models;
using System.Configuration;

namespace LibraryFrameworkTest
{
    class Program
    {
        private static UserProviders userProviders = new UserProviders(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        private static RoleProviders roleProviders = new RoleProviders(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);

        public Program()
        {
            
        }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(LoginUser().Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static ValidationModel RegisterUser()
        {
            var regisModel = new RegisterBinding();
            regisModel.UserName = "davin.gustanto";
            regisModel.FirstName = "Davin";
            regisModel.Password = "GustantoD#09";
            regisModel.ConfirmPassword = "GustantoD#09";
            regisModel.EmailAddress = "davin.gustanto@rds.co.id";
            var regist = userProviders.Register(regisModel);
            return regist;
        }

        public static ValidationModel LoginUser()
        {
            var loginModel = new LoginBinding { Password = "GustantoD09", UserName = "davin.gustanto" };
            var auth = userProviders.Authentication(loginModel);
            return auth;
        }

        public static ValidationModel CreateRole()
        {
            var createRole = roleProviders.CreateRole("Administrator");
            return createRole;
        }

        public static ValidationModel AddUsersToRole()
        {
            var createUserRole = roleProviders.AddUserToRole("davin.gustanto", "Administrator");
            return createUserRole;
        }
    }
}
