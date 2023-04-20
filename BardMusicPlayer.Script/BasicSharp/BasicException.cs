/*
 * Copyright(c) 2022 Mateusz Muszyñski
 * Licensed under the MIT License (MIT). See https://raw.githubusercontent.com/Timu5/BasicSharp/master/LICENSE for full license information.
 */

using System;

namespace BasicSharp
{
    internal sealed class BasicException : Exception
    {
        public int line;
        public BasicException()
        {
        }

        public BasicException(string message, int line)
            : base(message)
        {
            this.line = line;
        }

        public BasicException(string message, int line, Exception inner)
            : base(message, inner)
        {
            this.line = line;
        }
    }
}
