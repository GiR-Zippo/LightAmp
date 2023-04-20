/*
 * Copyright(c) 2022 Mateusz Muszyñski
 * Licensed under the MIT License (MIT). See https://raw.githubusercontent.com/Timu5/BasicSharp/master/LICENSE for full license information.
 */

namespace BasicSharp
{
    public struct Marker
    {
        public int Pointer { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public Marker(int pointer, int line, int column)
            : this()
        {
            Pointer = pointer;
            Line = line;
            Column = Column;
        }
    }
}
