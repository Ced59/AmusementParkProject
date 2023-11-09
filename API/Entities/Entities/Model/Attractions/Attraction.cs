using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Model.Attractions
{
    public class Attraction : ModelBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string AttractionType { get; set; }

    }
}
