using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToDoCoreBot
{
    public class User
    {
        public List<string> TaskList = new List<string>();
        public static string UserID { get; set; }
    }
}
