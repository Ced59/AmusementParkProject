using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonStatus.General;

namespace Entities.Model
{
    public abstract class ModelBase
    {
        public Guid Id { get; set; }
        public GeneralStatus OpenStatus { get; set; }
    }
}
