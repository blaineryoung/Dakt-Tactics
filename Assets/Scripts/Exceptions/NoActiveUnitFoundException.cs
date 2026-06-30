using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.Exceptions
{
    public class NoActiveUnitFoundException : Exception
    {
        public NoActiveUnitFoundException(string e) : base(e) { }   
    }
}
