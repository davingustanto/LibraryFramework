using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibraryFramework.Models;

namespace LibraryFramework
{
    public class UserProviders : IDisposable
    {
        private string connectionString = null;
        private Connection connection = null;
        private List<ParameterQueryString> parameterQueryStrings = null;
        private ValidationModel validationModel = null;
        private RoleProviders roleProviders = null;

        public UserProviders(string _connectionString)
        {
            if (!Services.IsConnectionValid(_connectionString))
                throw new Exception("Cannot connect to Database Server.");
            validationModel = new ValidationModel { IsValid = true };
            parameterQueryStrings = new List<ParameterQueryString>();
            connectionString = _connectionString;
            connection = new Connection(connectionString);
            roleProviders = new RoleProviders(connectionString);
            StructureUserTable.BuildTable(connection);
        }

        public IEnumerable<UserProfile> UserProfiles
        {
            get
            {
                var user_lists = new List<UserProfile>();
                DataTable dt_users = connection.OpenDataTable(@"SELECT a.id, a.user_name, b.first_name, b.last_name, b.email, b.phone_number, b.register_date
                                                                FROM Membership_User a, Membership_Profile b
                                                                WHERE b.id = a.id ORDER BY UPPER(a.user_name)");
                for (int i = 0; i < dt_users.Rows.Count; i++)
                {
                    user_lists.Add(new UserProfile
                    {
                        Email = dt_users.Rows[i][4].ToString(),
                        Id = dt_users.Rows[i][0].ToString(),
                        RegisterDate = DateTime.Parse(dt_users.Rows[i][6].ToString()),
                        FirstName = dt_users.Rows[i][2].ToString(),
                        LastName = dt_users.Rows[i][3].ToString(),
                        PhoneNumber = dt_users.Rows[i][5].ToString(),
                        Username = dt_users.Rows[i][1].ToString()
                    });
                }
                return user_lists;
            }
        }

        public ValidationModel Register(RegisterBinding registerBinding)
        {
            var Model = ValidationAction.IsRegisterValid(registerBinding);
            if (!Model.IsValid)
                validationModel = Model;
            else
            {
                if (IsUserNameRegistered(registerBinding.UserName))
                    validationModel = new ValidationModel { IsValid = false, Message = "Username has already registered." };
                parameterQueryStrings.Clear();
                PropertyInfo[] propInfos = registerBinding.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var item in propInfos)
                {
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@" + item.Name, Value = (item.GetValue(registerBinding) == null ? null : item.GetValue(registerBinding).ToString()) });
                }

                try
                {
                    var guid_ = Guid.NewGuid();
                    var securityStamp = Services.Base64Encode(guid_.ToString());

                    string password_hash = PasswordStorage.CreateHash(registerBinding.Password);

                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = registerBinding.UserName, DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = guid_.ToString().ToUpper(), DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@security_stamp", Value = securityStamp, DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@password_hash", Value = password_hash, DbType = SqlDbType.NVarChar });

                    connection.ExecuteQuery(@"INSERT INTO Membership_User (id, user_name, access_failed_count, email_confirmed, last_login_time, lockout_enabled, security_Stamp, password_hash) 
                                        VALUES (@id, @user_name, 0, 0, GETDATE(), 0, @security_stamp, @password_hash)", parameterQueryStrings);

                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = guid_.ToString().ToUpper(), DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@first_name", Value = registerBinding.FirstName.ToUpper(), DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@last_name", Value = (!string.IsNullOrEmpty(registerBinding.LastName) ? (object)registerBinding.LastName.ToUpper() : DBNull.Value), DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@email", Value = registerBinding.EmailAddress, DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@phone_number", Value = (!string.IsNullOrEmpty(registerBinding.PhoneNumber) ? (object)registerBinding.PhoneNumber : DBNull.Value), DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@register_date", Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DbType = SqlDbType.NVarChar });
                    connection.ExecuteQuery("INSERT INTO Membership_Profile VALUES (@id, @first_name, @last_name, @email, @phone_number, @register_date)", parameterQueryStrings);
                    validationModel = new ValidationModel { IsValid = true, Message = null };
                }
                catch (Exception ex)
                {
                    validationModel = new ValidationModel { Message = ex.Message, IsValid = false };
                }
            }
            return validationModel;
        }

        public ValidationModel Update(RegisterBinding registerBinding, string user_id)
        {
            var Model = ValidationAction.IsRegisterValid(registerBinding, true);
            if (!Model.IsValid)
                validationModel = Model;
            else
            {
                if (IsUserNameRegistered(registerBinding.UserName, true))
                    validationModel = new ValidationModel { IsValid = false, Message = "Username has already registered." };
                parameterQueryStrings.Clear();
                PropertyInfo[] propInfos = registerBinding.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var item in propInfos)
                {
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@" + item.Name, Value = (item.GetValue(registerBinding) == null ? null : item.GetValue(registerBinding).ToString()) });
                }

                try
                {
                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = registerBinding.UserName, DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = user_id, DbType = SqlDbType.NVarChar });
                    connection.ExecuteQuery("UPDATE Membership_User SET user_name = @user_name WHERE id = @id", parameterQueryStrings);

                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = user_id, DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@first_name", Value = registerBinding.FirstName.ToUpper(), DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@last_name", Value = (!string.IsNullOrEmpty(registerBinding.LastName) ? (object)registerBinding.LastName.ToUpper() : DBNull.Value), DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@email", Value = registerBinding.EmailAddress, DbType = SqlDbType.NVarChar });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@phone_number", Value = (!string.IsNullOrEmpty(registerBinding.PhoneNumber) ? (object)registerBinding.PhoneNumber : DBNull.Value), DbType = SqlDbType.NVarChar });
                    connection.ExecuteQuery("UPDATE Membership_Profile SET first_name = @first_name, last_name = @last_name, email = @email, phone_number = @phone_number WHERE id = @id", parameterQueryStrings);
                    validationModel = new ValidationModel { IsValid = true, Message = null };
                }
                catch (Exception ex)
                {
                    validationModel = new ValidationModel { Message = ex.Message, IsValid = false };
                }
            }
            return validationModel;
        }

        public ValidationModel Authentication(LoginBinding loginBinding)
        {
            var Model = ValidationAction.IsLoginValid(loginBinding);
            if (!Model.IsValid)
                validationModel = Model;
            else
            {
                if (!IsUserNameRegistered(loginBinding.UserName))
                    validationModel = new ValidationModel { IsValid = false, Message = "Username not registered." };
                else
                {
                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = loginBinding.UserName.ToUpper(), DbType = SqlDbType.NVarChar });
                    DataTable dt_Auth = connection.OpenDataTable("SELECT DISTINCT password_hash, security_stamp, email_confirmed FROM Membership_User WHERE UPPER(user_name) = @user_name", parameterQueryStrings);
                    //var password_hash = Services.Decrypt(dt_Auth.Rows[0]["password_hash"].ToString(), dt_Auth.Rows[0]["security_stamp"].ToString());
                    if (!PasswordStorage.VerifyPassword(loginBinding.Password, dt_Auth.Rows[0]["password_hash"].ToString()))
                    {
                        connection.ExecuteQuery("UPDATE Membership_User SET lockout_enabled = (lockout_enabled + 1) WHERE user_name = @user_name", parameterQueryStrings);
                        validationModel = new ValidationModel { IsValid = false, Message = "Username or password incorrect." };
                    }
                    else
                    {
                        if (!bool.Parse(dt_Auth.Rows[0]["email_confirmed"].ToString()))
                            validationModel = new ValidationModel { IsValid = false, Message = "Account must be verified." };
                        else
                        {
                            connection.ExecuteQuery("UPDATE Membership_User SET lockout_enabled = 0 WHERE user_name = @user_name", parameterQueryStrings);
                            validationModel = new ValidationModel { IsValid = true, Message = "Authentication success." };
                        }
                    }
                }
            }
            return validationModel;
        }

        public ValidationModel ChangePassword(ChangePasswordBinding changePasswordBinding)
        {
            validationModel = new ValidationModel { IsValid = false };
            var user_detail = UserDetail(changePasswordBinding.UserName, null);
            if (user_detail == null)
            {
                validationModel.IsValid = false;
                validationModel.Message = "User not found.";
            }
            else if (!Authentication(new LoginBinding { Password = changePasswordBinding.OldPassword, UserName = changePasswordBinding.UserName }).IsValid)
            {
                validationModel.IsValid = false;
                validationModel.Message = "Old password incorrect.";
            }
            else
            {
                if (changePasswordBinding.NewPassword != changePasswordBinding.ConfirmNewPassword)
                {
                    validationModel.IsValid = false;
                    validationModel.Message = "New password and confirm password not match.";
                }
                else
                {
                    try
                    {
                        string security_stamp = null;
                        parameterQueryStrings.Clear();
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = changePasswordBinding.UserName });
                        DataTable dt_user = connection.OpenDataTable("SELECT security_stamp FROM Membership_User WHERE user_name = @user_name", parameterQueryStrings);
                        if (dt_user.Rows.Count > 0)
                            security_stamp = dt_user.Rows[0][0].ToString();
                        parameterQueryStrings.Clear();
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@password_hash", Value = PasswordStorage.CreateHash(changePasswordBinding.NewPassword) });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = user_detail.Id });
                        connection.ExecuteQuery("UPDATE Membership_User SET password_hash = @password_hash WHERE id = @id", parameterQueryStrings);
                        validationModel = new ValidationModel { IsValid = true };
                    }
                    catch (Exception ex)
                    {
                        validationModel.IsValid = false;
                        validationModel.Message = ex.Message;
                    }
                }
            }
            return validationModel;
        }

        public string GenerateTokenConfirmation(string userName)
        {
            string token_ = null;
            var user_ = UserDetail(userName, null);
            if (user_ != null)
                token_ = "Confirmation " + user_.Id + " " + user_.Email + " " + DateTime.Now.ToString("yyyy/MM/dd") + "_" + DateTime.Now.ToString("HH:mm:ss");
            token_ = Services.Base64Encode(token_);
            return token_;
        }

        public string GenerateTokenForgotPassword(string userName)
        {
            string token_ = null;
            var user_ = UserDetail(userName, null);
            if (user_ != null)
                token_ = user_.Id + " " + DateTime.Now.ToString("yyyy/MM/dd") + "_" + DateTime.Now.ToString("HH:mm:ss");
            token_ = Services.Base64Encode(token_);
            return token_;
        }

        public ValidationModel MatchingTokenForgotPassword(string token_, string userName)
        {
            validationModel = new ValidationModel { IsValid = true };
            if (string.IsNullOrEmpty(token_) && string.IsNullOrEmpty(userName))
                validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
            token_ = Services.Base64Decode(token_);
            string[] token_list = token_.Split(' ');
            if (token_list.Count() < 2)
                validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
            else
            {
                string user_id = token_list[0];
                DateTime issued_date = new DateTime();
                if (!DateTime.TryParse(token_list[1].Replace("_", " "), out issued_date))
                    validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                else
                {
                    issued_date = DateTime.Parse(token_list[1].Replace("_", " "));
                    if ((DateTime.Now - issued_date).Hours > 3)
                        validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                    else
                    {
                        var user_data = UserDetail(userName, null);
                        if (user_data == null)
                            validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                        else
                        {
                            if (user_data.Id.ToUpper() != user_id.ToUpper())
                                validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                        }
                    }
                }
            }
            return validationModel;
        }

        public ValidationModel MatchingTokenConfirmation(string token_, string userName)
        {
            validationModel = new ValidationModel { IsValid = true };
            if (string.IsNullOrEmpty(token_) && string.IsNullOrEmpty(userName))
                validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
            token_ = Services.Base64Decode(token_);
            string[] token_list = token_.Split(' ');
            if (token_list.Count() < 4)
                validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
            else
            {
                if (token_list[0] != "Confirmation")
                    validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                else
                {
                    DateTime issued_date = new DateTime();
                    if (!DateTime.TryParse(token_list[3].Replace("_", " "), out issued_date))
                        validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                    else
                    {
                        issued_date = DateTime.Parse(token_list[3].Replace("_", " "));
                        if ((DateTime.Now - issued_date).Hours > 3)
                            validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                        else
                        {
                            var user_data = UserDetail(userName, null);
                            if (user_data == null)
                                validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                            else
                            {
                                if (user_data.Id.ToUpper() != token_list[1].ToUpper())
                                    validationModel = new ValidationModel { IsValid = false, Message = "Invalid code." };
                            }
                        }
                    }
                }
            }

            if (validationModel.IsValid)
            {
                parameterQueryStrings.Clear();
                parameterQueryStrings.Add(new ParameterQueryString { DbType = SqlDbType.Bit, Name = "@email_confirmed", Value = true });
                parameterQueryStrings.Add(new ParameterQueryString { DbType = SqlDbType.NVarChar, Name = "@user_name", Value = userName });
                connection.ExecuteQuery("UPDATE Membership_User SET email_confirmed = @email_confirmed WHERE user_name = @user_name", parameterQueryStrings);
            }

            return validationModel;
        }

        public UserProfile FindByUserName(string userName)
        {
            return UserDetail(userName, null);
        }

        public UserProfile FindById(string userId)
        {
            return UserDetail(null, userId);
        }

        private UserProfile UserDetail(string userName = null, string userId = null)
        {
            var data = new UserProfile();
            parameterQueryStrings.Clear();
            if ((!string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(userId)))
            {
                if (!string.IsNullOrEmpty(userName))
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = userName });
                else if (!string.IsNullOrEmpty(userId))
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = userId });

                DataTable dt_user = connection.OpenDataTable(@"SELECT DISTINCT a.id, a.user_name, b.first_name, b.last_name, b.email, b.phone_number, b.register_date
                FROM Membership_User a, Membership_Profile b
                WHERE b.id = a.id AND " + (!string.IsNullOrEmpty(userName) ? "a.user_name = @user_name" : "a.id = @id"), parameterQueryStrings);
                if (dt_user.Rows.Count > 0)
                    data = new UserProfile
                    {
                        Email = dt_user.Rows[0][4].ToString(),
                        Id = dt_user.Rows[0][0].ToString(),
                        RegisterDate = DateTime.Parse(dt_user.Rows[0][6].ToString()),
                        FirstName = dt_user.Rows[0][2].ToString(),
                        LastName = dt_user.Rows[0][3].ToString(),
                        PhoneNumber = dt_user.Rows[0][5].ToString(),
                        Username = dt_user.Rows[0][1].ToString()
                    };
                else
                    data = null;
            }
            else
                data = null;
            return data;
        }

        public bool IsUserNameRegistered(string userName, bool isUpdating = false)
        {
            if (string.IsNullOrEmpty(userName))
                return false;
            else
            {
                parameterQueryStrings.Clear();
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = userName.ToUpper() });
                DataTable dt_User = connection.OpenDataTable("SELECT user_name FROM Membership_User WHERE UPPER(user_name) = @user_name" + (isUpdating ? " AND UPPER(user_name) != @user_name" : null), parameterQueryStrings);
                return (dt_User.Rows.Count > 0);
            }
        }

        public void Migration(List<OldTempUser> oldTempUsers)
        {
            try
            {
                foreach (var item in oldTempUsers)
                {
                    bool isValid = true;
                    if (FindById(item.UserId) != null)
                        isValid = false;
                    if (IsUserNameRegistered(item.Username))
                        isValid = false;
                    if (isValid)
                    {
                        var securityStamp = Services.Base64Encode(item.UserId);
                        string password_hash = PasswordStorage.CreateHash(item.Password);
                        parameterQueryStrings.Clear();
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = item.Username, DbType = SqlDbType.NVarChar });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = item.UserId.ToUpper(), DbType = SqlDbType.NVarChar });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@security_stamp", Value = securityStamp, DbType = SqlDbType.NVarChar });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@password_hash", Value = password_hash, DbType = SqlDbType.NVarChar });

                        connection.ExecuteQuery(@"INSERT INTO Membership_User (id, user_name, access_failed_count, email_confirmed, last_login_time, lockout_enabled, security_Stamp, password_hash) 
                                        VALUES (@id, @user_name, 0, 0, GETDATE(), 0, @security_stamp, @password_hash)", parameterQueryStrings);

                        parameterQueryStrings.Clear();
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = item.UserId.ToUpper(), DbType = SqlDbType.NVarChar });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@first_name", Value = item.FirstName.ToUpper(), DbType = SqlDbType.NVarChar });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@last_name", Value = (!string.IsNullOrEmpty(item.LastName) ? (object)item.LastName.ToUpper() : DBNull.Value), DbType = SqlDbType.NVarChar });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@email", Value = item.EmailAddress, DbType = SqlDbType.NVarChar });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@register_date", Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DbType = SqlDbType.NVarChar });
                        connection.ExecuteQuery("INSERT INTO Membership_Profile VALUES (@id, @first_name, @last_name, @email, NULL, @register_date)", parameterQueryStrings);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Migration(List<OldTempRole> oldTempRoles)
        {
            try
            {
                foreach (var item in oldTempRoles)
                {
                    bool isValid = true;
                    var userData = FindById(item.UserId);
                    if (userData == null)
                        isValid = false;
                    if (string.IsNullOrEmpty(item.RoleId))
                        isValid = false;
                    if (isValid)
                    {
                        string role_id = item.RoleId.ToUpper();
                        if (roleProviders.RoleExists(item.RoleName.ToUpper()))
                            role_id = roleProviders.GetAllRoles().Where(a => a.ToUpper() == item.RoleName.ToUpper()).FirstOrDefault();
                        else
                        {
                            parameterQueryStrings.Clear();
                            parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = role_id });
                            parameterQueryStrings.Add(new ParameterQueryString { Name = "@role_name", Value = item.RoleName });
                            connection.ExecuteQuery("INSERT INTO Membership_Role VALUES (@id, @role_name)", parameterQueryStrings);
                        }

                        if (!roleProviders.IsUserInRole(userData.Username, item.RoleName))
                            roleProviders.AddUserToRole(userData.Username, item.RoleName);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Dispose()
        {
            validationModel = null;
            parameterQueryStrings.Clear();
            connection.Dispose();
            connectionString = null;
        }
    }

    public class RoleProviders : IDisposable
    {
        private string connectionString = null;
        private Connection connection = null;
        private List<ParameterQueryString> parameterQueryStrings = null;
        private ValidationModel validationModel = null;

        public RoleProviders(string _connectionString)
        {
            if (!Services.IsConnectionValid(_connectionString))
                throw new Exception("Cannot connect to Database Server.");
            validationModel = new ValidationModel { IsValid = true };
            parameterQueryStrings = new List<ParameterQueryString>();
            connectionString = _connectionString;
            connection = new Connection(connectionString);
            StructureUserTable.BuildTable(connection);
        }

        private List<string> role_list { get; set; }

        public ValidationModel CreateRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                validationModel = new ValidationModel { IsValid = false, Message = "Role name must be filled." };
            else
            {
                parameterQueryStrings.Clear();
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@role_name", Value = roleName });
                DataTable dt_Exist = connection.OpenDataTable("SELECT * FROM Membership_Role WHERE UPPER(name) = @role_name", parameterQueryStrings);
                if (dt_Exist.Rows.Count > 0)
                    validationModel = new ValidationModel { IsValid = false, Message = "Role name is already used." };
                else
                {
                    try
                    {
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = Guid.NewGuid().ToString().ToUpper() });
                        connection.ExecuteQuery("INSERT INTO Membership_Role VALUES (@id, @role_name)", parameterQueryStrings);
                        validationModel = new ValidationModel { IsValid = true, Message = null };
                    }
                    catch (Exception ex)
                    {
                        validationModel = new ValidationModel { IsValid = false, Message = ex.Message };
                    }
                }
            }
            return validationModel;
        }

        public ValidationModel AddUserToRole(string userName, string roleName)
        {
            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(roleName))
                validationModel = new ValidationModel { IsValid = false, Message = "User Id or Role Id cannot be empty." };
            else
            {
                parameterQueryStrings.Clear();
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = userName.ToUpper() });
                DataTable dt_Exist = connection.OpenDataTable("SELECT user_name, id FROM Membership_User WHERE UPPER(user_name) = @user_name", parameterQueryStrings);
                if (dt_Exist.Rows.Count == 0)
                    validationModel = new ValidationModel { IsValid = false, Message = "Username not found." };
                else
                {
                    string user_id = dt_Exist.Rows[0][1].ToString();
                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@role_name", Value = roleName.ToUpper() });
                    dt_Exist = connection.OpenDataTable("SELECT id, name FROM Membership_Role WHERE UPPER(name) = @role_name", parameterQueryStrings);
                    if (dt_Exist.Rows.Count == 0)
                        validationModel = new ValidationModel { IsValid = false, Message = "Role Id not found." };
                    else
                    {
                        try
                        {
                            string role_id = dt_Exist.Rows[0][0].ToString();
                            parameterQueryStrings.Clear();
                            parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_id", Value = user_id.ToUpper() });
                            parameterQueryStrings.Add(new ParameterQueryString { Name = "@role_id", Value = role_id.ToUpper() });

                            dt_Exist = connection.OpenDataTable("SELECT * FROM Membership_User_Role WHERE user_id = @user_id AND role_id = @role_id", parameterQueryStrings);
                            if (dt_Exist.Rows.Count > 0)
                                validationModel = new ValidationModel { IsValid = false, Message = "User has been registered to this role." };
                            else
                            {
                                connection.ExecuteQuery("INSERT INTO Membership_User_Role (user_id, role_id) VALUES (@user_id, @role_id)", parameterQueryStrings);
                                validationModel = new ValidationModel { IsValid = true, Message = null };
                            }
                        }
                        catch (Exception ex)
                        {
                            validationModel = new ValidationModel { IsValid = false, Message = ex.Message };
                        }
                    }
                }
            }
            return validationModel;
        }

        public ValidationModel RemoveUserFromRole(string userName, string roleName)
        {
            var model_return = new ValidationModel { IsValid = true };
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(roleName))
                model_return = new ValidationModel { IsValid = false, Message = "Username and role name must be filled." };
            else
            {
                if (!IsUserInRole(userName, roleName))
                    model_return = new ValidationModel { IsValid = false, Message = "Username and role name are not registered." };
                else
                {
                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = userName.ToUpper() });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@role_name", Value = roleName.ToUpper() });
                    try
                    {
                        connection.ExecuteQuery(@"DELETE FROM Membership_User_Role
                        WHERE user_id = (SELECT id FROM Membership_User WHERE UPPER(user_name) = @user_name) AND role_id = (SELECT id FROM Membership_Role WHERE UPPER(name) = @role_name)", parameterQueryStrings);
                        model_return = new ValidationModel { IsValid = true };
                    }
                    catch (Exception ex)
                    {
                        model_return = new ValidationModel { IsValid = false, Message = ex.Message };
                    }
                }
            }
            return model_return;
        }

        public List<string> GetRolesForUser(string userName)
        {
            role_list = new List<string>();
            parameterQueryStrings.Clear();
            if (!string.IsNullOrEmpty(userName))
            {
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_name", Value = userName.ToUpper() });
                DataTable dt_roles = connection.OpenDataTable(@"SELECT a.user_name, b.name
                FROM Membership_User a, Membership_Role b, Membership_User_Role c
                WHERE b.id = c.role_id AND a.id = c.user_id AND UPPER(a.user_name) = @user_name", parameterQueryStrings);
                for (int i = 0; i < dt_roles.Rows.Count; i++)
                {
                    role_list.Add(dt_roles.Rows[i][1].ToString());
                }
            }
            return role_list;
        }

        public List<string> GetAllRoles()
        {
            List<string> role_list = new List<string>();
            DataTable dt_roles = connection.OpenDataTable("SELECT name FROM Membership_Role");
            for (int i = 0; i < dt_roles.Rows.Count; i++)
            {
                role_list.Add(dt_roles.Rows[i][0].ToString());
            }
            return role_list;
        }

        public List<string> GetUsersInRole(string roleName)
        {
            List<string> user_list = new List<string>();
            parameterQueryStrings.Clear();
            if (!string.IsNullOrEmpty(roleName))
            {
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@role_name", Value = roleName.ToUpper() });
                DataTable dt_userRoles = connection.OpenDataTable(@"SELECT b.user_name
                FROM Membership_User_Role a, Membership_User b, Membership_Role c
                WHERE b.id = a.user_id AND c.id = a.role_id AND UPPER(c.name) = @role_name", parameterQueryStrings);
                for (int i = 0; i < dt_userRoles.Rows.Count; i++)
                {
                    user_list.Add(dt_userRoles.Rows[i][0].ToString());
                }
            }
            return user_list;
        }

        public bool RoleExists(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return true;
            parameterQueryStrings.Clear();
            parameterQueryStrings.Add(new ParameterQueryString { Value = roleName, Name = "@role_name" });
            DataTable dt_role = connection.OpenDataTable("SELECT name FROM Membership_Role WHERE UPPER(name) = @role_name", parameterQueryStrings);
            return (dt_role.Rows.Count > 0);
        }

        public bool IsUserInRole(string userName, string roleName)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(roleName))
                return false;
            else
            {
                GetRolesForUser(userName);
                return (role_list.Where(a => a == roleName).Count() > 0);
            }
        }

        public bool DeleteRole(string roleName)
        {
            parameterQueryStrings.Clear();
            if (string.IsNullOrEmpty(roleName))
                return false;
            else
            {
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@role_name", Value = roleName.ToUpper() });
                DataTable dt_role = connection.OpenDataTable("SELECT id, name FROM Membership_Role WHERE UPPER(name) = @role_name", parameterQueryStrings);
                if (dt_role.Rows.Count == 0)
                    return false;
                else
                {
                    string role_id = dt_role.Rows[0][0].ToString();
                    parameterQueryStrings.Clear();
                    try
                    {
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@role_id", Value = role_id });
                        connection.ExecuteQuery("DELETE FROM Membership_Role WHERE id = @role_id", parameterQueryStrings);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        public void Dispose()
        {
            validationModel = null;
            parameterQueryStrings.Clear();
            connection.Dispose();
            connectionString = null;
        }
    }

    public class PermissionProviders : IDisposable
    {
        private string connectionString = null;
        private Connection connection = null;
        private UserProviders userProviders = null;
        private List<ParameterQueryString> parameterQueryStrings = null;
        private ValidationModel validationModel = null;
        private RoleProviders roleProviders = null;

        public PermissionProviders(string _connectionString)
        {
            if (!Services.IsConnectionValid(_connectionString))
                throw new Exception("Cannot connect to Database Server.");
            validationModel = new ValidationModel { IsValid = true };
            parameterQueryStrings = new List<ParameterQueryString>();
            connectionString = _connectionString;
            connection = new Connection(connectionString);
            roleProviders = new RoleProviders(_connectionString);
            userProviders = new UserProviders(_connectionString);
            StructureUserTable.BuildTable(connection);
        }

        public List<PermissionBinding> Permissions
        {
            get
            {
                List<PermissionBinding> permissionBindings = new List<PermissionBinding>();
                DataTable dt_permissions = connection.OpenDataTable("SELECT * FROM Membership_Permission");
                for (int i = 0; i < dt_permissions.Rows.Count; i++)
                {
                    permissionBindings.Add(new PermissionBinding { Id = int.Parse(dt_permissions.Rows[i][0].ToString()), Name = dt_permissions.Rows[i][1].ToString() });
                }
                return permissionBindings;
            }
        }

        public ValidationModel CreatePermissionName(PermissionBinding permissionBinding)
        {
            validationModel = new ValidationModel { IsValid = true };
            if (string.IsNullOrEmpty(permissionBinding.Name))
                validationModel = new ValidationModel { IsValid = false, Message = "Permission name must be filled." };
            else
            {
                if (!IsPermissionNameExist(permissionBinding.Name))
                {
                    try
                    {
                        connection.ExecuteQuery("INSERT INTO Membership_Permission (name) VALUES (@name)", parameterQueryStrings);
                    }
                    catch (Exception ex)
                    {
                        validationModel = new ValidationModel { IsValid = false, Message = ex.Message };
                    }
                }
                else
                    validationModel = new ValidationModel { IsValid = false, Message = "Permission name has already used." };
            }
            return validationModel;
        }

        public ValidationModel UpdatePermissionName(PermissionBinding permissionBinding)
        {
            validationModel = new ValidationModel { IsValid = true };
            if (!IsPermissionNameExist(permissionBinding.Name))
                validationModel = new ValidationModel { IsValid = false, Message = "Permission name is not exist." };
            else
            {
                parameterQueryStrings.Clear();
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@name", Value = permissionBinding.Name.ToUpper() });
                DataTable dt_permission = connection.OpenDataTable("SELECT id FROM Membership_Permission WHERE UPPER(name) = @name", parameterQueryStrings);
                try
                {
                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = dt_permission.Rows[0][0] });
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@name", Value = permissionBinding.Name });
                    connection.ExecuteQuery("UPDATE Membership_Permission SET name = @name WHERE id = @id");
                }
                catch (Exception ex)
                {
                    validationModel = new ValidationModel { IsValid = false, Message = ex.Message };
                }
            }
            return validationModel;
        }

        public ValidationModel GrantUserPermission(UserPermissionBinding userPermissionBinding)
        {
            validationModel = new ValidationModel { IsValid = true };
            if (!IsPermissionNameExist(userPermissionBinding.PermissionId))
                validationModel = new ValidationModel { IsValid = false, Message = "Permission data not found." };
            else
            {
                if (userProviders.FindById(userPermissionBinding.UserId) == null)
                    validationModel = new ValidationModel { IsValid = false, Message = "User not found." };
                else
                {
                    try
                    {
                        parameterQueryStrings.Clear();
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@permission_id", Value = userPermissionBinding.PermissionId });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_id", Value = userPermissionBinding.UserId });
                        connection.ExecuteQuery("INSERT INTO Membership_Permission_User (permission_id, user_id) VALUES (@permission_id, @user_id)");
                    }
                    catch (Exception ex)
                    {
                        validationModel = new ValidationModel { IsValid = false, Message = ex.Message };
                    }
                }
            }
            return validationModel;
        }

        public bool IsUserGranted(string user_name, string permission_name)
        {
            var user_data = userProviders.FindByUserName(user_name);
            if (string.IsNullOrEmpty(user_name) || string.IsNullOrEmpty(permission_name))
                return false;
            else if (userProviders.FindByUserName(user_name) == null)
                return false;
            else if (!IsPermissionNameExist(permission_name))
                return false;
            else
            {
                parameterQueryStrings.Clear();
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@name", Value = permission_name.ToUpper(), DbType = SqlDbType.NVarChar });
                DataTable dt_permission = connection.OpenDataTable("SELECT * FROM Membership_Permission WHERE UPPER(name) = @name", parameterQueryStrings);
                parameterQueryStrings.Clear();
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_id", Value = user_data.Id, DbType = SqlDbType.NVarChar });
                parameterQueryStrings.Add(new ParameterQueryString { Name = "@permission_name", Value = int.Parse(dt_permission.Rows[0][0].ToString()), DbType = SqlDbType.Int });
                DataTable dt_user_permission = connection.OpenDataTable("SELECT * FROM Membership_Permission_User WHERE user_id = @user_id AND permission_id = @permission_id", parameterQueryStrings);
                return (dt_user_permission.Rows.Count > 0);
            }
        }

        public ValidationModel GrantUserPermission(string permission_name, string role_name)
        {
            validationModel = new ValidationModel { IsValid = true };
            if (!IsPermissionNameExist(permission_name))
                validationModel = new ValidationModel { IsValid = false, Message = "Permission not valid." };
            else
            {
                if (roleProviders.RoleExists(role_name))
                    validationModel = new ValidationModel { IsValid = false, Message = "Role not valid." };
                else
                {
                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@name", Value = permission_name.ToUpper(), DbType = SqlDbType.NVarChar });
                    DataTable dt_permission = connection.OpenDataTable("SELECT * FROM Membership_Permission WHERE UPPER(name) = @name", parameterQueryStrings);
                    foreach (var item in roleProviders.GetUsersInRole(role_name))
                    {
                        var user_data = userProviders.FindByUserName(item);
                        if (!IsUserGranted(item, permission_name))
                        {
                            parameterQueryStrings.Clear();
                            parameterQueryStrings.Add(new ParameterQueryString { Name = "@permission_id", Value = dt_permission.Rows[0][0], DbType = SqlDbType.Int });
                            parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_id", Value = item, DbType = SqlDbType.NVarChar });
                            connection.ExecuteQuery("INSERT INTO Membership_Permission_User (permission_id, user_id) VALUES (@permission_id, @user_id)");
                        }
                    }
                }
            }
            return validationModel;
        }

        public ValidationModel RevokeUserPermission(string user_name, string permission_name)
        {
            validationModel = new ValidationModel { IsValid = true };
            if (string.IsNullOrEmpty(user_name) && string.IsNullOrEmpty(permission_name))
                validationModel = new ValidationModel { IsValid = false, Message = "Username and permission name must be filled." };
            else
            {
                var user_data = userProviders.FindByUserName(user_name);
                if (user_data == null && !IsPermissionNameExist(permission_name))
                    validationModel = new ValidationModel { IsValid = false, Message = "User or permission not found." };
                else
                {
                    try
                    {
                        parameterQueryStrings.Clear();
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@name", Value = permission_name.ToUpper(), DbType = SqlDbType.NVarChar });
                        DataTable dt_permission = connection.OpenDataTable("SELECT * FROM Membership_Permission WHERE UPPER(name) = @name", parameterQueryStrings);
                        parameterQueryStrings.Clear();
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@user_id", DbType = SqlDbType.NVarChar, Value = user_data.Id });
                        parameterQueryStrings.Add(new ParameterQueryString { Name = "@permission_id", Value = dt_permission.Rows[0][0], DbType = SqlDbType.Int });
                        connection.ExecuteQuery("DELETE FROM Membership_Permission_User WHERE user_id = @user_id AND permission_id = @permission_id", parameterQueryStrings);
                    }
                    catch (Exception ex)
                    {
                        validationModel = new ValidationModel { IsValid = false, Message = ex.Message };
                    }
                }
            }
            return validationModel;
        }

        public ValidationModel DeletePermission(string permission_name)
        {
            validationModel = new ValidationModel { IsValid = true };
            if (string.IsNullOrEmpty(permission_name))
                validationModel = new ValidationModel { IsValid = false, Message = "Permission name must be filled." };
            else
            {
                if (!IsPermissionNameExist(permission_name))
                    validationModel = new ValidationModel { IsValid = false, Message = "Permission not found." };
                else
                {
                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@name", Value = permission_name.ToUpper(), DbType = SqlDbType.NVarChar });
                    DataTable dt_permission = connection.OpenDataTable("SELECT * FROM Membership_Permission WHERE UPPER(name) = @name", parameterQueryStrings);
                    parameterQueryStrings.Clear();
                    parameterQueryStrings.Add(new ParameterQueryString { Name = "@permission_id", Value = dt_permission.Rows[0][0], DbType = SqlDbType.Int });
                    DataTable dt_user_permission = connection.OpenDataTable("SELECT * FROM Membership_Permission_User WHERE permission_id = @permission_id");
                    try
                    {
                        for (int i = 0; i < dt_user_permission.Rows.Count; i++)
                        {
                            parameterQueryStrings.Clear();
                            parameterQueryStrings.Add(new ParameterQueryString { DbType = SqlDbType.Int, Name = "@id", Value = dt_user_permission.Rows[i][0] });
                            connection.ExecuteQuery("DELETE FROM Membership_Permission_User WHERE id = @id", parameterQueryStrings);
                        }
                        parameterQueryStrings.Clear();
                        parameterQueryStrings.Add(new ParameterQueryString { DbType = SqlDbType.Int, Name = "@id", Value = dt_permission.Rows[0][0] });
                        connection.ExecuteQuery("DELETE FROM Membership_Permission WHERE id = @id");
                    }
                    catch (Exception ex)
                    {
                        validationModel = new ValidationModel { IsValid = false, Message = ex.Message };
                    }
                }
            }
            return validationModel;
        }

        public bool IsPermissionNameExist(string permissionName)
        {
            if (string.IsNullOrEmpty(permissionName))
                return true;
            parameterQueryStrings.Clear();
            parameterQueryStrings.Add(new ParameterQueryString { Name = "@name", Value = permissionName.ToUpper() });
            DataTable dt_checkData = connection.OpenDataTable("SELECT * FROM Membership_Permission WHERE UPPER(name) = @name", parameterQueryStrings);
            return (dt_checkData.Rows.Count > 0);
        }

        public bool IsPermissionNameExist(int permission_id)
        {
            if (permission_id < 1)
                return true;
            parameterQueryStrings.Clear();
            parameterQueryStrings.Add(new ParameterQueryString { Name = "@id", Value = permission_id });
            DataTable dt_checkData = connection.OpenDataTable("SELECT * FROM Membership_Permission WHERE id = @id", parameterQueryStrings);
            return (dt_checkData.Rows.Count > 0);
        }

        public void Dispose()
        {
            validationModel = null;
            parameterQueryStrings.Clear();
            connection.Dispose();
            connectionString = null;
        }
    }

    public static class StructureUserTable
    {
        public static void BuildTable(Connection connection)
        {
            try
            {
                if (!connection.IsTableExist("Membership_Profile"))
                {
                    connection.ExecuteQuery("CREATE TABLE [dbo].[Membership_Profile]([id] [nvarchar](128) NOT NULL, [first_name] [varchar](256) NOT NULL, [last_name] [varchar](256) NULL, [email] [varchar](256) NOT NULL, [phone_number] [varchar](128) NULL, [register_date] [datetime] NOT NULL, PRIMARY KEY CLUSTERED ([id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]");
                    connection.ExecuteQuery("ALTER TABLE [dbo].[Membership_Profile] ADD DEFAULT (getdate()) FOR [register_date]");
                }
                if (!connection.IsTableExist("Membership_Role"))
                {
                    connection.ExecuteQuery("CREATE TABLE [dbo].[Membership_Role]([id] [nvarchar](128) NOT NULL, [name] [nvarchar](128) NOT NULL, PRIMARY KEY CLUSTERED ([id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]");
                }
                if (!connection.IsTableExist("Membership_User"))
                {
                    connection.ExecuteQuery("CREATE TABLE [dbo].[Membership_User]([id] [nvarchar](128) NOT NULL, [user_name] [nvarchar](256) NOT NULL, [password_hash] [nvarchar](256) NOT NULL, [security_stamp] [nvarchar](256) NOT NULL, [last_login_time] [datetime] NOT NULL, [email_confirmed] [bit] NOT NULL, [access_failed_count] [int] NOT NULL, [lockout_enabled] [bit] NOT NULL, CONSTRAINT [PK__Membersh__3214EC07F7380BF6] PRIMARY KEY CLUSTERED ([id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]");
                    connection.ExecuteQuery(@"ALTER TABLE[dbo].[Membership_User] ADD CONSTRAINT[DF__Membership__LastLoginTime] DEFAULT(getdate()) FOR[last_login_time]
                    ALTER TABLE[dbo].[Membership_User] ADD CONSTRAINT[DF__Membership__EmailConfirmed] DEFAULT((0)) FOR[email_confirmed]
                    ALTER TABLE[dbo].[Membership_User] ADD CONSTRAINT[DF__AccessFailedCount]  DEFAULT((0)) FOR[access_failed_count]
                    ALTER TABLE[dbo].[Membership_User] ADD CONSTRAINT[DF__LockoutEnabled]  DEFAULT((0)) FOR[lockout_enabled]");
                }
                if (!connection.IsTableExist("Membership_User_Role"))
                {
                    connection.ExecuteQuery("CREATE TABLE [dbo].[Membership_User_Role]([id] [int] IDENTITY(1,1) NOT NULL, [user_id] [nvarchar](128) NOT NULL, [role_id] [nvarchar](128) NOT NULL, PRIMARY KEY CLUSTERED ([id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]");
                    connection.ExecuteQuery(@"ALTER TABLE[dbo].[Membership_User_Role] WITH CHECK ADD FOREIGN KEY([role_id]) REFERENCES[dbo].[Membership_Role]([id]) ON DELETE CASCADE
                    ALTER TABLE[dbo].[Membership_User_Role] WITH CHECK ADD CONSTRAINT[FK__Membership__UserId] FOREIGN KEY([user_id]) REFERENCES[dbo].[Membership_User]([id]) ON DELETE CASCADE ALTER TABLE[dbo].[Membership_User_Role] CHECK CONSTRAINT[FK__Membership__UserId]");
                }
                if (!connection.IsTableExist("Membership_Permission"))
                {
                    connection.ExecuteQuery("CREATE TABLE Membership_Permission ( id INT IDENTITY(1, 1) NOT NULL PRIMARY KEY, name NVARCHAR(256) NOT NULL )");
                }
                if (!connection.IsTableExist("Membership_Permission_User"))
                {
                    connection.ExecuteQuery(@"CREATE TABLE Membership_Permission_User
                    (
                    id INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
                    permission_id INT NOT NULL,
                    user_id NVARCHAR(128) NOT NULL,
                    FOREIGN KEY(permission_id) REFERENCES Membership_Permission(id) ON DELETE CASCADE,
                    FOREIGN KEY(user_id) REFERENCES Membership_User(id) ON DELETE CASCADE
                    )");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public class ValidationAction : IDisposable
    {
        public static ValidationModel IsRegisterValid(RegisterBinding registerBinding, bool isUpdate = false)
        {
            var model_return = new ValidationModel();
            if (registerBinding == null)
                return new ValidationModel { Message = "Parameter must be passed.", IsValid = false };
            else
            {
                model_return.ModelErrors = new List<State>();
                if (string.IsNullOrEmpty(registerBinding.UserName))
                    model_return.ModelErrors.Add(new State { Field = "UserName", Message = "Username must be filled." });
                else
                {
                    if (registerBinding.UserName.Length < 5 || registerBinding.UserName.Length > 200)
                        model_return.ModelErrors.Add(new State { Field = "UserName", Message = "Username must between 5 - 200 characters." });
                }

                if (string.IsNullOrEmpty(registerBinding.FirstName))
                    model_return.ModelErrors.Add(new State { Field = "FirstName", Message = "First name must be filled." });
                else
                {
                    if (registerBinding.FirstName.Length < 3 || registerBinding.FirstName.Length > 200)
                        model_return.ModelErrors.Add(new State { Field = "FirstName", Message = "First name must between 3 - 200 characters." });
                }

                if (!isUpdate)
                {
                    if (string.IsNullOrEmpty(registerBinding.Password))
                        model_return.ModelErrors.Add(new State { Field = "Password", Message = "Password must be filled." });
                    else
                    {
                        if (registerBinding.Password.Length < 5 || registerBinding.Password.Length > 200)
                            model_return.ModelErrors.Add(new State { Field = "Password", Message = "Password must between 5 - 200 characters." });
                        else
                        {
                            var regexItem = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).{8,15}$");
                            if (!regexItem.IsMatch(registerBinding.Password))
                                model_return.ModelErrors.Add(new State { Field = "Password", Message = "Password must contains alphanumeric characters." });
                        }
                    }

                    if (string.IsNullOrEmpty(registerBinding.ConfirmPassword))
                        model_return.ModelErrors.Add(new State { Field = "ConfirmPassword", Message = "Confirm password must be filled." });
                    else
                    {
                        if (registerBinding.ConfirmPassword != registerBinding.Password)
                            model_return.ModelErrors.Add(new State { Field = "ConfirmPassword", Message = "Wrong password." });
                    }
                }

                if (string.IsNullOrEmpty(registerBinding.EmailAddress))
                    model_return.ModelErrors.Add(new State { Field = "EmailAddress", Message = "Email address must be filled." });
                else
                {
                    if (!registerBinding.EmailAddress.Contains("@"))
                        model_return.ModelErrors.Add(new State { Field = "EmailAddress", Message = "Email address wrong format." });
                    else
                    {
                        string email_temp = registerBinding.EmailAddress.Substring(registerBinding.EmailAddress.IndexOf("@") + 1);
                        if (!email_temp.Contains("."))
                            model_return.ModelErrors.Add(new State { Field = "EmailAddress", Message = "Password must between 5 - 200 characters." });
                    }
                }

                model_return.IsValid = (model_return.ModelErrors.Count() == 0);
                model_return.Message = (model_return.IsValid ? null : "Invalid request, please check your paramaters.");
            }
            return model_return;
        }

        public static ValidationModel IsLoginValid(LoginBinding loginBinding)
        {
            var model_return = new ValidationModel { IsValid = true };
            if (loginBinding == null)
                return new ValidationModel { IsValid = false, Message = "Parameter must be passed." };
            else
            {
                model_return.ModelErrors = new List<State>();
                if (string.IsNullOrEmpty(loginBinding.UserName))
                    model_return.ModelErrors.Add(new State { Field = "UserName", Message = "Username must be filled." });
                if (string.IsNullOrEmpty(loginBinding.Password))
                    model_return.ModelErrors.Add(new State { Field = "Password", Message = "Password must be filled." });
                return model_return;
            }
        }

        public void Dispose()
        {

        }
    }
}
