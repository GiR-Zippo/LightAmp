/*
 * Copyright(c) 2017 Eli Belash
 * Licensed under the MIT license. See https://github.com/Nucs/JsonSettings/blob/master/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BardMusicPlayer.Pigeonhole.JsonSettings.Inline
{
    internal static class Activation
    {
        public static IEnumerable<ConstructorInfo> GetAllConstructors(this Type t)
        {
            return t.GetConstructors().Concat(t.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));
        }

        public static object CreateInstance(this Type t)
        {
            var ctrs = t.GetAllConstructors()
                .Where(static c => c.GetParameters().Length == 0 || c.GetParameters().All(static p => p.IsOptional))
                .ToArray();
            if (ReflectionHelpers.IsValueType(t) ||
                ctrs.Any(static c => c.IsPublic)) //is valuetype or has public constractor.
                return Activator.CreateInstance(t);

            var prv = ctrs.FirstOrDefault(static c =>
                c.IsAssembly || c.IsFamily || c.IsPrivate); //check protected/internal/private constructor
            if (prv == null)
                throw new BmpPigeonholeException(
                    $"Type {t.FullName} does not have empty constructor (public or private)");

            return prv.Invoke(null);
        }
    }
}