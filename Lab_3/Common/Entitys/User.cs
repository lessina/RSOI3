using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class User
    {
        public string name;
        public string password;

        public User()
        {
        }

        public User(string name, string password)
        {   this.name = name;
            this.password = password;
        }
    }
}
