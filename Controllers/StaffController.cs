using baranggaysystem1.Models;
using baranggaysystem1.Services;
using System.Collections.Generic;

namespace baranggaysystem1.Controllers
{
    public class StaffController
    {
        private readonly StaffService _staffService;

        public StaffController()
        {
            _staffService = new StaffService();
        }

        public List<Staff> GetStaffs()
        {
            return _staffService.GetStaffs();
        }

        public void AddStaff(Staff staff)
        {
            _staffService.AddStaff(staff);
        }

        public void UpdateStaff(Staff staff)
        {
            _staffService.UpdateStaff(staff);
        }

        public void DeleteStaff(int staffId)
        {
            _staffService.DeleteStaff(staffId);
        }
    }
}
