﻿using System.Collections;

namespace BlobBackup.Test;

public class Seeds : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var random = new Random(976328274);

        for (int i = 0; i < 200; i++)
        {
            yield return new object[] { random.Next() ^ 686464466 };
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
