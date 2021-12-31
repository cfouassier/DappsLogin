using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DappsLogin.Models
{
    public class LoginUser
    {
        public string Signer { get; set; } // Ethereum account that claim the signature
        public string Signature { get; set; } // The signature
        public string Message { get; set; } // The plain message
        public string Hash { get; set; } // The prefixed and sha3 hashed message 
    }
}
