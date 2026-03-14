/*
 * Copyright(c) 2025 GiR-Zippo, 2022 Mateusz Muszyñski
 * Licensed under the MIT License (MIT). See https://raw.githubusercontent.com/Timu5/BasicSharp/master/LICENSE for full license information.
 */

namespace BasicSharp
{
    public enum Token
    {
        Unknown,

        Identifier,
        Value,

        //Keywords
        Print,
        Macro,
        If,
        EndIf,
        Then,
        Else,
        For,
        To,
        Next,
        Step,
        Goto,
        Input,
        Playtime,
        Let,
        Gosub,
        Return,
        Rem,
        End,
        Assert,
        Select,
        UnSelect,
        Sleep,
        TapKey,
        CPrint,

        NewLine,
        Colon,
        Semicolon,
        Comma,

        Plus,
        Minus,
        Slash,
        Asterisk,
        Caret,
        Equal,
        Less,
        More,
        NotEqual,
        LessEqual,
        MoreEqual,
        Or,
        And,
        Not,

        LParen,
        RParen,

        EOF = -1   //End Of File
    }
}
