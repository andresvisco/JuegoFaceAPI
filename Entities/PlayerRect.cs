using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class PlayerRect
    {
        public OpenCvSharp.Rect[] Rect { get; set; }

        public Guid Id { get; set; }
    }
}
