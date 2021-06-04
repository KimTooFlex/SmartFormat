﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartFormat.Extensions;
using SmartFormat.Utilities;

namespace SmartFormat.Tests.Core
{
    [TestFixture]
    public class AlignmentTests
    {
        private SmartFormatter GetSimpleFormatter()
        {
            var formatter = new SmartFormatter(); 
            formatter.FormatterExtensions.Add(new DefaultFormatter());
            formatter.SourceExtensions.Add(new ReflectionSource(formatter));
            formatter.SourceExtensions.Add(new DefaultSource(formatter));
            return formatter;
        }

        [Test]
        public void Formatter_PreAlign()
        {
            const string name = "Joe";
            var obj = new { name = name };
            var result = GetSimpleFormatter().Format("Name: {name,10}| Column 2", obj);
            Assert.That(result, Is.EqualTo("Name:        Joe| Column 2"));
        }

        [Test]
        public void Formatter_PostAlign()
        {
            const string name = "Joe";
            var obj = new { name = name };
            var result = GetSimpleFormatter().Format("Name: {name,-10}| Column 2", obj);
            Assert.That(result, Is.EqualTo("Name: Joe       | Column 2"));
        }

        [Test]
        public void Formatter_PostAlign_Custom_AlignmentChar()
        {
            const string name = "Joe";
            var obj = new { name = name };
            var smart = GetSimpleFormatter();
            smart.Settings.Formatter.AlignmentFillCharacter = '.'; // dot instead of space
            var result = smart.Format("Name: {name,-10}|", obj);
            Assert.That(result, Is.EqualTo("Name: Joe.......|"));
        }

        [Test]
        public void Formatter_AlignNull()
        {
            var smart = GetSimpleFormatter();
            string? name = null;
            var obj = new { name = name };
            var result = GetSimpleFormatter().Format("Name: {name,-10}| Column 2", obj);
            Assert.That(result, Is.EqualTo("Name:           | Column 2"));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(-2)]
        [TestCase(10)]
        [TestCase(-10)]
        public void ChooseFormatter_Alignment(int alignment)
        {
            var smart = GetSimpleFormatter();
            smart.FormatterExtensions.Add(new ChooseFormatter());

            var data = new {Number = 2};
            var format = $"{{Number,{alignment}:choose(1|2|3):one|two|three}}";
            var result = smart.Format(format, data);
            var expected = string.Format($"{{0,{alignment}}}", "two");
            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        /// Arguments should be aligned, but spacers should not
        /// </summary>
        /// <param name="alignment"></param>
        [TestCase(0)]
        [TestCase(2)]
        [TestCase(-2)]
        [TestCase(10)]
        [TestCase(-10)]
        public void ListFormatter_NestedFormats_Alignment(int alignment)
        {
            var smart = GetSimpleFormatter();
            smart.FormatterExtensions.Add(new ListFormatter(smart));

            var items = new [] { "one", "two", "three" };
            var result = smart.Format($"{{items,{alignment}:list:{{}}|, |, and }}", new { items }); // important: not only "items" as the parameter
            var expected = string.Format($"{{0,{alignment}}}, {{1,{alignment}}}, and {{2,{alignment}}}", items);
            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        /// Arguments should be aligned, but spacers should not
        /// </summary>
        /// <param name="format"></param>
        /// <param name="expected"></param>
        [TestCase("{0:list}", "System.Int32[]")]
        [TestCase("{0,2:list:|}"," 1 2 3 4 5")]
        [TestCase("{0,4:list:000|}"," 001 002 003 004 005")]
        [TestCase("{0,3:list:|,}","  1,  2,  3,  4,  5")]
        [TestCase("{0,-3:list:||+  |}","1  2  3  4  +  5  ")]
        [TestCase("{0,0:list:N2|, |, and }","1.00, 2.00, 3.00, 4.00, and 5.00")]
        public void ListFormatter_UnnestedFormats_Alignment(string format, string expected)
        {
            var smart = GetSimpleFormatter();
            smart.FormatterExtensions.Add(new ListFormatter(smart));

            var args = new[] {1, 2, 3, 4, 5};
            var result = smart.Format(CultureInfo.InvariantCulture, format, args);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
