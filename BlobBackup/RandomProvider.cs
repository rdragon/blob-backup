using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class RandomProvider
    {
        private RNGCryptoServiceProvider? _random;

        private RNGCryptoServiceProvider Random
        {
            get
            {
                if (_random is null)
                {
                    _random = new RNGCryptoServiceProvider();
                }

                return _random;
            }
        }

        public byte[] GetBytes(int length)
        {
            var bytes = new byte[length];
            Random.GetBytes(bytes);

            return bytes;
        }
    }
}
