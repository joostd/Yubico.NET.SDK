// Copyright 2021 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Security.Cryptography;
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Piv.Objects
{
    public class ChuidTests
    {
        [Fact]
        public void Constructor_IsEmpty_Correct()
        {
            using var chuid = new CardholderUniqueId();

            Assert.True(chuid.IsEmpty);
        }

        [Fact]
        public void Constructor_DataTag_Correct()
        {
            using var chuid = new CardholderUniqueId();

            Assert.Equal(0x005FC102, chuid.DataTag);
        }

        [Fact]
        public void Constructor_DefinedDataTag_Correct()
        {
            using var chuid = new CardholderUniqueId();

            int definedTag = chuid.GetDefinedDataTag();
            Assert.Equal(0x005FC102, definedTag);
        }

        [Fact]
        public void SetTag_DataTag_Correct()
        {
            using var chuid = new CardholderUniqueId();
            chuid.SetDataTag(0x005FFF10);

            Assert.Equal(0x005FFF10, chuid.DataTag);
        }

        [Fact]
        public void SetTag_DefinedDataTag_Correct()
        {
            using var chuid = new CardholderUniqueId();
            chuid.SetDataTag(0x005FFF10);

            int definedTag = chuid.GetDefinedDataTag();
            Assert.Equal(0x005FC102, definedTag);
        }

        [Theory]
        [InlineData(0x015FFF10)]
        [InlineData(0x0000007E)]
        [InlineData(0x00007F61)]
        [InlineData(0x005FC101)]
        [InlineData(0x005FC104)]
        [InlineData(0x005FC105)]
        [InlineData(0x005FC10A)]
        [InlineData(0x005FC10B)]
        [InlineData(0x005FC10D)]
        [InlineData(0x005FC120)]
        [InlineData(0x005FFF01)]
        public void SetTag_InvalidTag_Throws(int newTag)
        {
            using var chuid = new CardholderUniqueId();

            _ = Assert.Throws<ArgumentException>(() => chuid.SetDataTag(newTag));
        }

        [Fact]
        public void Constructor_Encode_Exception()
        {
            using var chuid = new CardholderUniqueId();

            _ = Assert.Throws<InvalidOperationException>(() => chuid.Encode());
        }

        [Fact]
        public void Decode_NoData_Empty()
        {
            var encoding = new Memory<byte>();

            using var chuid = new CardholderUniqueId();

            chuid.Decode(encoding);
            Assert.True(chuid.IsEmpty);
        }

        [Fact]
        public void RandomGuid_Valid_NotEmpty()
        {
            using var chuid = new CardholderUniqueId();

            chuid.SetRandomGuid();
            Assert.False(chuid.IsEmpty);
        }

        [Fact]
        public void SetGuid_Valid_NotEmpty()
        {
            byte[] newGuid = GetRandomBytes(16, false);

            using var chuid = new CardholderUniqueId();

            chuid.SetGuid(newGuid);
            Assert.False(chuid.IsEmpty);
        }

        [Fact]
        public void SetGuid_Valid_CorrectData()
        {
            var expectedValue = new Span<byte>(new byte[] {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
            });
            byte[] newGuid = GetRandomBytes(16, true);

            using var chuid = new CardholderUniqueId();

            chuid.SetGuid(newGuid);

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expectedValue, chuid.GuidValue.Span);
            Assert.True(isValid);
        }

        [Fact]
        public void SetGuid_BadLength_Throws()
        {
            byte[] newGuid = GetRandomBytes(15, true);

            using var chuid = new CardholderUniqueId();

            _ = Assert.Throws<ArgumentException>(() => chuid.SetGuid(newGuid));
        }

        [Fact]
        public void Encode_Valid_CorrectData()
        {
            var expectedValue = new Span<byte>(new byte[] {
                0x53, 0x3B, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x00,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x00
            });
            byte[] newGuid = GetRandomBytes(16, true);

            using var chuid = new CardholderUniqueId();

            chuid.SetGuid(newGuid);
            byte[] encoding = chuid.Encode();
            var encodingSpan = new Span<byte>(encoding);

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expectedValue, encodingSpan);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_Valid_IsEmptyFalse()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x3B, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x00,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x00
            });

            using var chuid = new CardholderUniqueId();
            chuid.Decode(encodedValue);

            Assert.False(chuid.IsEmpty);
        }

        [Fact]
        public void Decode_Valid_CorrectGuid()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x3B, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x00,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x00
            });
            var expectedValue = new Span<byte>(new byte[] {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
            });

            using var chuid = new CardholderUniqueId();
            chuid.Decode(encodedValue);

            bool isValid = MemoryExtensions.SequenceEqual<byte>(expectedValue, chuid.GuidValue.Span);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_InvalidFascn_Throws()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x3B, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xea, 0x34, 0x10, 0x00,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x00
            });

            using var chuid = new CardholderUniqueId();
            _ = Assert.Throws<ArgumentException>(() => chuid.Decode(encodedValue));
        }

        [Fact]
        public void Decode_InvalidGuid_Throws()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x3C, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x11, 0x00,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x35, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x00
            });

            using var chuid = new CardholderUniqueId();
            _ = Assert.Throws<ArgumentException>(() => chuid.Decode(encodedValue));
        }

        [Fact]
        public void Decode_InvalidDate_Throws()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x3B, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x00,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x31, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x00
            });

            using var chuid = new CardholderUniqueId();
            _ = Assert.Throws<ArgumentException>(() => chuid.Decode(encodedValue));
        }

        [Fact]
        public void Decode_InvalidSig_Throws()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x3B, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x00,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3f, 0x00, 0xfe, 0x00
            });

            using var chuid = new CardholderUniqueId();
            _ = Assert.Throws<ArgumentException>(() => chuid.Decode(encodedValue));
        }

        [Fact]
        public void Decode_InvalidLrc_Throws()
        {
            var encodedValue = new Memory<byte>(new byte[] {
                0x53, 0x3C, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x00,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x01, 0x01
            });

            using var chuid = new CardholderUniqueId();
            _ = Assert.Throws<ArgumentException>(() => chuid.Decode(encodedValue));
        }

        private byte[] GetRandomBytes(int count, bool isFixed)
        {
            byte[] fixedBytes = new byte[] {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22
            };

            byte[] returnValue = new byte[count];

            byte[]? inputArg = null;
            if (isFixed)
            {
                inputArg = fixedBytes;
            }

            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(inputArg);
            random.GetBytes(returnValue);

            return returnValue;
        }
    }
}