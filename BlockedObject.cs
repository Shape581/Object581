using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Object581
{
    public class BlockedObject
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int ItemId { get; set; }
        public bool BlockedForStaff { get; set; }
    }
}
