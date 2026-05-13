using System.Collections.Generic;
using System.Data;
using baranggaysystem1.Database;
using baranggaysystem1.Models;

namespace baranggaysystem1.Services
{
    public class BarangayOfficialService
    {
        public List<BarangayOfficial> GetBarangayOfficials()
        {
            var officials = new List<BarangayOfficial>();
            var table = DbHelper.LoadTable(
                "SELECT official_id, first_name, last_name, position, term_start, term_end FROM barangay_official ORDER BY position, last_name");

            foreach (DataRow row in table.Rows)
            {
                officials.Add(new BarangayOfficial
                {
                    OfficialId = Convert.ToInt32(row["official_id"]),
                    FirstName = row["first_name"]?.ToString() ?? string.Empty,
                    LastName = row["last_name"]?.ToString() ?? string.Empty,
                    Position = row["position"]?.ToString() ?? string.Empty,
                    TermStart = row["term_start"]?.ToString() ?? string.Empty,
                    TermEnd = row["term_end"]?.ToString() ?? string.Empty
                });
            }

            return officials;
        }

        public void AddBarangayOfficial(BarangayOfficial official)
        {
            DbHelper.ExecuteNonQuery(
                @"INSERT INTO barangay_official (first_name, last_name, position, term_start, term_end)
                  VALUES (@firstName, @lastName, @position, @termStart, @termEnd)",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@firstName", official.FirstName);
                    cmd.Parameters.AddWithValue("@lastName", official.LastName);
                    cmd.Parameters.AddWithValue("@position", official.Position);
                    cmd.Parameters.AddWithValue("@termStart", official.TermStart);
                    cmd.Parameters.AddWithValue("@termEnd", official.TermEnd);
                });
        }

        public void UpdateBarangayOfficial(BarangayOfficial official)
        {
            DbHelper.ExecuteNonQuery(
                @"UPDATE barangay_official
                  SET first_name = @firstName, last_name = @lastName, position = @position,
                      term_start = @termStart, term_end = @termEnd
                  WHERE official_id = @officialId",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@officialId", official.OfficialId);
                    cmd.Parameters.AddWithValue("@firstName", official.FirstName);
                    cmd.Parameters.AddWithValue("@lastName", official.LastName);
                    cmd.Parameters.AddWithValue("@position", official.Position);
                    cmd.Parameters.AddWithValue("@termStart", official.TermStart);
                    cmd.Parameters.AddWithValue("@termEnd", official.TermEnd);
                });
        }

        public void DeleteBarangayOfficial(int officialId)
        {
            DbHelper.ExecuteNonQuery(
                "DELETE FROM barangay_official WHERE official_id = @officialId",
                cmd => cmd.Parameters.AddWithValue("@officialId", officialId));
        }
    }
}
