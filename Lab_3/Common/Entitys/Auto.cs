using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Photo
    {
        public int id;
        public string mileage;
        public string owner;

        public Photo()
        {
        }

        public Photo(int id, string mileage, string owner)
        {   this.id = id;
        this.mileage = mileage;
            this.owner = owner;
        }
    }
}
