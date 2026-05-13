using baranggaysystem1.Models;
using baranggaysystem1.Services;
using System.Collections.Generic;

namespace baranggaysystem1.Controllers
{
    public class BarangayOfficialController
    {
        private readonly BarangayOfficialService _barangayOfficialService;

        public BarangayOfficialController()
        {
            _barangayOfficialService = new BarangayOfficialService();
        }

        public List<BarangayOfficial> GetBarangayOfficials()
        {
            return _barangayOfficialService.GetBarangayOfficials();
        }

        public void AddBarangayOfficial(BarangayOfficial official)
        {
            _barangayOfficialService.AddBarangayOfficial(official);
        }

        public void UpdateBarangayOfficial(BarangayOfficial official)
        {
            _barangayOfficialService.UpdateBarangayOfficial(official);
        }

        public void DeleteBarangayOfficial(int officialId)
        {
            _barangayOfficialService.DeleteBarangayOfficial(officialId);
        }
    }
}
