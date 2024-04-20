using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonStatus.General;
using Entities.Model.Attractions;

namespace Entities.Model.Parks
{
    public class Park : ModelBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ParkType { get; set; }
        public string Address { get; set; }
        public string? City { get; set; }
        public string? StateProvince { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public GeneralStatus OpenStatus { get; set; }
        public List<Attraction>? Attractions { get; set; }
    }
}
