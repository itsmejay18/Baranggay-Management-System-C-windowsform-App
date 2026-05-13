using System;
using System.Collections.Generic;
using System.Data;
using baranggaysystem1.Database;
using baranggaysystem1.Models;

namespace baranggaysystem1.Services
{
    public class StaffService
    {
        public List<Staff> GetStaffs()
        {
            var staffList = new List<Staff>();
            var table = DbHelper.LoadTable(
                "SELECT staff_id, first_name, last_name, position, contact_number, address FROM staff ORDER BY last_name, first_name");

            foreach (DataRow row in table.Rows)
            {
                staffList.Add(new Staff
                {
                    StaffId = Convert.ToInt32(row["staff_id"]),
                    FirstName = row["first_name"]?.ToString() ?? string.Empty,
                    LastName = row["last_name"]?.ToString() ?? string.Empty,
                    Position = row["position"]?.ToString() ?? string.Empty,
                    ContactNumber = row["contact_number"]?.ToString() ?? string.Empty,
                    Address = row["address"]?.ToString() ?? string.Empty
                });
            }

            return staffList;
        }

        public void AddStaff(Staff staff)
        {
            DbHelper.ExecuteNonQuery(
                @"INSERT INTO staff (first_name, last_name, position, contact_number, address)
                  VALUES (@firstName, @lastName, @position, @contactNumber, @address)",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@firstName", staff.FirstName);
                    cmd.Parameters.AddWithValue("@lastName", staff.LastName);
                    cmd.Parameters.AddWithValue("@position", staff.Position);
                    cmd.Parameters.AddWithValue("@contactNumber", staff.ContactNumber);
                    cmd.Parameters.AddWithValue("@address", staff.Address);
                });
        }

        public void UpdateStaff(Staff staff)
        {
            DbHelper.ExecuteNonQuery(
                @"UPDATE staff
                  SET first_name = @firstName, last_name = @lastName, position = @position,
                      contact_number = @contactNumber, address = @address
                  WHERE staff_id = @staffId",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@staffId", staff.StaffId);
                    cmd.Parameters.AddWithValue("@firstName", staff.FirstName);
                    cmd.Parameters.AddWithValue("@lastName", staff.LastName);
                    cmd.Parameters.AddWithValue("@position", staff.Position);
                    cmd.Parameters.AddWithValue("@contactNumber", staff.ContactNumber);
                    cmd.Parameters.AddWithValue("@address", staff.Address);
                });
        }

        public void DeleteStaff(int staffId)
        {
            DbHelper.ExecuteNonQuery(
                "DELETE FROM staff WHERE staff_id = @staffId",
                cmd => cmd.Parameters.AddWithValue("@staffId", staffId));
        }
    }
}
