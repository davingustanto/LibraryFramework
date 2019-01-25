using LibraryFramework.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryFramework
{
    public class ServiceOneProvider : IDisposable
    {
        private Connection connection;

        public ServiceOneProvider(string connectionString)
        {
            connection = new Connection(connectionString);
        }

        public int CalculateLeave(string date_start, string date_end)
        {
            List<string> stringList1 = new List<string>();
            double num1 = (DateTime.Parse(date_end) - DateTime.Parse(date_start)).TotalDays + 1.0;
            int num2 = Convert.ToInt32(num1);
            DateTime dateTime1 = DateTime.Parse(date_start);
            DateTime dateTime2 = DateTime.Parse(date_end);
            List<string> stringList2 = new List<string>();
            for (int index = 0; (double)index < num1; ++index)
            {
                if (index == 0)
                {
                    if (dateTime1.DayOfWeek.ToString() == "Saturday" || dateTime1.DayOfWeek.ToString() == "Sunday")
                        num2 = Convert.ToInt32(num2) - 1;
                }
                else if (dateTime1.AddDays((double)index).DayOfWeek.ToString() == "Saturday" || dateTime1.AddDays((double)index).DayOfWeek.ToString() == "Sunday")
                    num2 = Convert.ToInt32(num2) - 1;
            }
            List<ParameterQueryString> parameterQueryStringList = new List<ParameterQueryString>();
            ParameterQueryString parameterQueryString1 = new ParameterQueryString { DbType = SqlDbType.Date, Name = "@DateStart", Value = dateTime1.ToString("yyyy-MM-dd") };
            parameterQueryStringList.Add(parameterQueryString1);

            ParameterQueryString parameterQueryString2 = new ParameterQueryString { DbType = SqlDbType.Date, Name = "@DateEnd", Value = dateTime2.ToString("yyyy-MM-dd") };
            parameterQueryStringList.Add(parameterQueryString2);

            DataTable dataTable = connection.OpenDataTable("SELECT * FROM IgnoranceDate WHERE Date >= @DateStart AND Date <= @DateEnd", parameterQueryStringList);
            if (dataTable.Rows.Count > 0)
            {
                for (int index = 0; index < dataTable.Rows.Count; ++index)
                {
                    DateTime dateTime3 = Convert.ToDateTime(dataTable.Rows[index][2]);
                    num2 = dateTime3.Date.DayOfWeek.ToString() == "Saturday" || dateTime3.Date.DayOfWeek.ToString() == "Sunday" ? Convert.ToInt32(num2) : Convert.ToInt32(num2) - 1;
                }
            }
            return num2;
        }

        public DateTime SetWorkingDate(string date_start, string date_end, int? id_form)
        {
            DateTime dateTime1 = DateTime.Parse(date_end);
            DateTime dateTime2 = DateTime.Parse(date_start);
            DateTime dateTime3 = DateTime.Parse(date_end);
            List<ParameterQueryString> parameterQueryStringList = new List<ParameterQueryString>();
            ParameterQueryString parameterQueryString1 = new ParameterQueryString { DbType = SqlDbType.Date, Value = dateTime2.ToString("yyyy-MM-dd"), Name = "@DateStart" };
            parameterQueryStringList.Add(parameterQueryString1);

            ParameterQueryString parameterQueryString2 = new ParameterQueryString { DbType = SqlDbType.Date, Name = "@DateEnd", Value = dateTime3.ToString("yyyy-MM-dd") };
            parameterQueryStringList.Add(parameterQueryString2);

            DataTable dataTable = connection.OpenDataTable("SELECT * FROM IgnoranceDate WHERE Date >= @DateStart AND Date <= @DateEnd", parameterQueryStringList);
            if (dateTime1.DayOfWeek.ToString().Contains("Saturday"))
                dateTime1 = dateTime1.AddDays(2.0);
            else if (dateTime1.DayOfWeek.ToString().Contains("Sunday"))
                dateTime1 = dateTime1.AddDays(1.0);
            else if (dataTable.Rows.Count > 0)
            {
                for (int index = 0; index < dataTable.Rows.Count; ++index)
                {
                    if (Convert.ToDateTime(dataTable.Rows[index].ItemArray[2]) == dateTime1)
                    {
                        DateTime dateTime4 = Convert.ToDateTime(dataTable.Rows[index].ItemArray[2]);
                        if (dateTime4.DayOfWeek.ToString().Contains("Saturday"))
                        {
                            dateTime4 = Convert.ToDateTime(dataTable.Rows[index].ItemArray[2]);
                            if (dateTime4.DayOfWeek.ToString().Contains("Sunday"))
                                continue;
                        }
                        dateTime1 = dateTime1.AddDays(1.0);
                    }
                }
            }
            return dateTime1;
        }

        public DateTime SetStartDate(DateTime date_origin, string time_origin, int id)
        {
            DateTime dateTime = date_origin;
            if (id == 1 || id == 4)
            {
                TimeSpan timeSpan = new TimeSpan(Convert.ToInt32(time_origin.Substring(0, 2)), Convert.ToInt32(time_origin.Substring(3, 2)), 0);
                dateTime = Convert.ToDateTime(date_origin).Date + timeSpan;
            }
            else if (id == 2 || id == 3)
            {
                TimeSpan timeSpan = new TimeSpan(8, 30, 0);
                dateTime = Convert.ToDateTime(date_origin).Date + timeSpan;
            }
            return dateTime;
        }

        public DateTime SetFinishDate(DateTime date_origin_start, DateTime date_origin_end, string amount, string time_origin, int id, int request_id)
        {
            DateTime dateTime = date_origin_end;
            if (id == 1 || id == 4)
            {
                if (request_id == 4)
                {
                    dateTime = Convert.ToDateTime(date_origin_start).Date.AddDays((double)(Convert.ToInt32(amount) - 1)).Date + new TimeSpan(Convert.ToInt32(time_origin.Substring(0, 2)), Convert.ToInt32(time_origin.Substring(3, 2)), 0);
                }
                else
                {
                    TimeSpan timeSpan = new TimeSpan(Convert.ToInt32(time_origin.Substring(0, 2)), Convert.ToInt32(time_origin.Substring(3, 2)), 0);
                    dateTime = Convert.ToDateTime(date_origin_start).Date + timeSpan;
                }
            }
            else if (id == 2)
            {
                TimeSpan timeSpan = amount.Contains(".") ? new TimeSpan(12, 0, 0) : new TimeSpan(17, 30, 0);
                dateTime = Convert.ToDateTime(date_origin_end).Date + timeSpan;
            }
            return dateTime;
        }

        public DateTime SetStartDateEdit(DateTime date_origin, string time_origin, int request_id)
        {
            DateTime dateTime = date_origin;
            if (request_id >= 1 && request_id <= 4 || request_id == 7)
            {
                int hours = 0;
                int minutes = 0;
                if (request_id == 2 || request_id == 1)
                {
                    hours = 8;
                    minutes = 30;
                }
                else if (request_id == 3)
                {
                    hours = int.Parse(time_origin.Substring(0, 2));
                    minutes = int.Parse(time_origin.Substring(3, 2));
                }
                TimeSpan timeSpan = new TimeSpan(hours, minutes, 0);
                dateTime = Convert.ToDateTime(date_origin).Date + timeSpan;
            }
            else if (request_id == 5 || request_id == 8)
            {
                TimeSpan timeSpan = new TimeSpan(8, 30, 0);
                dateTime = Convert.ToDateTime(date_origin).Date + timeSpan;
            }
            return dateTime;
        }

        public DateTime SetFinishDateEdit(DateTime date_origin_start, DateTime date_origin_end, string amount, string time_origin, int request_id)
        {
            DateTime dateTime = date_origin_end;
            if (request_id >= 1 && request_id <= 4 || request_id == 7)
            {
                if (request_id == 4)
                {
                    Convert.ToDateTime(date_origin_start).Date.AddDays((double)(Convert.ToInt32(amount) - 1));
                    TimeSpan timeSpan = new TimeSpan(int.Parse(time_origin.Substring(0, 2)), int.Parse(time_origin.Substring(3, 2)), 0);
                    dateTime = date_origin_start.Date + timeSpan;
                }
                else
                {
                    int minutes = 0;
                    int hours = 0;
                    if (request_id == 2 || request_id == 8)
                    {
                        hours = int.Parse(time_origin.Substring(0, 2));
                        minutes = int.Parse(time_origin.Substring(3, 2));
                    }
                    else if (request_id == 3 || request_id == 1)
                    {
                        hours = 17;
                        minutes = 30;
                    }
                    TimeSpan timeSpan = new TimeSpan(hours, minutes, 0);
                    dateTime = Convert.ToDateTime(date_origin_start).Date + timeSpan;
                }
            }
            else if (request_id == 5 || request_id == 8)
            {
                TimeSpan timeSpan = amount.Contains(".") ? new TimeSpan(12, 0, 0) : new TimeSpan(17, 30, 0);
                dateTime = Convert.ToDateTime(date_origin_end).Date + timeSpan;
            }
            return dateTime;
        }
        
        public void Dispose()
        {
            
        }
    }
}
