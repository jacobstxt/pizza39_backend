﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.NovaPoshta.Area
{
    public class AreaPostModel
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string CalledMethod { get; set; } = string.Empty;
        public MethodProperties MethodProperties { get; set; } = new MethodProperties();
    }
}
