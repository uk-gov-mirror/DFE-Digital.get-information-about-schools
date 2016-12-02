﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edubase.Services.Exceptions
{

    [Serializable]
    public class EdubaseException : Exception
    {
        public EdubaseException() { }
        public EdubaseException(string message) : base(message) { }
        public EdubaseException(string message, Exception inner) : base(message, inner) { }
        protected EdubaseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}