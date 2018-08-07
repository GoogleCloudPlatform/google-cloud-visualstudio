// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GoogleCloudExtension.Utils.Wpf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Documents;
using TestingHelpers;

namespace GoogleCloudExtension.Utils.UnitTests.Wpf
{
    [TestClass]
    public class WhitespaceDiscardingSpanTests
    {
        private WhitespaceDiscardingSpan _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new WhitespaceDiscardingSpan();
            _objectUnderTest.BeginInit();
        }

        [TestMethod]
        public void TestEndInit_RemovesSingleSpaceRun()
        {
            _objectUnderTest.Inlines.Add(new Run(" "));

            _objectUnderTest.EndInit();

            CollectionAssert.That.IsEmpty(_objectUnderTest.Inlines);
        }

        [TestMethod]
        public void TestEndInit_PreservesOtherInlines()
        {
            var inlines = new Inline[]
            {
                new Run("SomeOtherText"),
                new Span(),
                new InlineUIContainer(),
                new Run("\n"),
                new Run("\t")
            };
            _objectUnderTest.Inlines.AddRange(inlines);

            _objectUnderTest.EndInit();

            CollectionAssert.AreEqual(inlines, _objectUnderTest.Inlines);
        }
    }
}
