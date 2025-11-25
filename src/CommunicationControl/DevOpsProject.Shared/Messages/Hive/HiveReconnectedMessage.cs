using DevOpsProject.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevOpsProject.Shared.Messages
{
    public class HiveReconnectedMessage : BaseMessage
    {
        public bool IsSuccessfullyReconnected { get; set; }
        public HiveModel Hive { get; set; }
        public HiveOperationalArea InitialOperationalArea { get; set; }
    }
}
