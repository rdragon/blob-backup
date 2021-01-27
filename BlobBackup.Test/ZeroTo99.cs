using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup.Test
{
    public class ZeroTo99 : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            for (int i = 0; i < 100; i++)
            {
                yield return new object[] { i };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
