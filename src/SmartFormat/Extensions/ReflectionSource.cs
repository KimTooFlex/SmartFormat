﻿//
// Copyright (C) axuno gGmbH, Scott Rippey, Bernhard Millauer and other contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SmartFormat.Core.Extensions;

namespace SmartFormat.Extensions
{
    public class ReflectionSource : ISource
    {
        private static readonly object[] Empty = Array.Empty<object>();

        private readonly Dictionary<(Type, string?), (FieldInfo? field, MethodInfo? method)> _typeCache = new();

        public ReflectionSource(SmartFormatter formatter)
        {
            // Add some special info to the parser:
            formatter.Parser.AddAlphanumericSelectors(); // (A-Z + a-z)
            formatter.Parser.AddAdditionalSelectorChars("_");
            formatter.Parser.AddOperators(".");
        }

        public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

            var current = selectorInfo.CurrentValue;
            var selector = selectorInfo.SelectorText;

            if (current == null) return false;

            // REFLECTION:
            // Let's see if the argSelector is a Selectors/Field/ParseFormat:
            var sourceType = current.GetType();

            // Check the type cache
            if (_typeCache.TryGetValue((sourceType, selector), out var found))
            {
                if (found.field != null)
                {
                    selectorInfo.Result = found.field.GetValue(current);
                    return true;
                }

                if (found.method != null)
                {
                    selectorInfo.Result = found.method.Invoke(current, Empty);
                    return true;
                }

                return false;
            }

            // Important:
            // GetMembers (opposite to GetMember!) returns all members, 
            // both those defined by the type represented by the current T:System.Type object 
            // AS WELL AS those inherited from its base types.
            var members = sourceType.GetMembers(bindingFlags).Where(m =>
                string.Equals(m.Name, selector, selectorInfo.FormatDetails.Settings.GetCaseSensitivityComparison()));
            foreach (var member in members)
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        //  Selector is a Field; retrieve the value:
                        var field = (FieldInfo) member;
                        selectorInfo.Result = field.GetValue(current);
                        _typeCache[(sourceType, selector)] = (field, null);
                        return true;
                    case MemberTypes.Property:
                    case MemberTypes.Method:
                        MethodInfo? method;
                        if (member.MemberType == MemberTypes.Property)
                        {
                            //  Selector is a Property
                            var prop = (PropertyInfo) member;
                            //  Make sure the property is not WriteOnly:
                            if (prop != null && prop.CanRead)
                                method = prop.GetGetMethod();
                            else
                                continue;
                        }
                        else
                        {
                            //  Selector is a method
                            method = (MethodInfo) member;
                        }

                        //  Check that this method is valid -- it needs to return a value and has to be parameter-less:
                        //  We are only looking for a parameter-less Function/Property:
                        if (method?.GetParameters().Length > 0) continue;

                        //  Make sure that this method is not void!  It has to be a Function!
                        if (method?.ReturnType == typeof(void)) continue;

                        // Add to cache
                        _typeCache[(sourceType, selector)] = (null, method);

                        //  Retrieve the Selectors/ParseFormat value:
                        selectorInfo.Result = method?.Invoke(current, new object[0]);
                        return true;
                }

            // We also cache failures so we dont need to call GetMembers again
            _typeCache[(sourceType, selector)] = (null, null);

            return false;
        }
    }
}