// Copyright 2022 Yubico AB
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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The response partner to some of the CredentialManagementCommand
    /// sub-commands.
    /// </summary>
    /// <remarks>
    /// Some of the sub-commands have responses that return data. If so, the
    /// sub-command class will implement
    /// <c>IYubiKeyCommand&lt;CredentialManagementResponse&gt;</c>. The response
    /// class will therefore be an instance of this class, and the caller
    /// can get the data returned as (<see cref="CredentialManagementData"/>).
    /// <para>
    /// Some sub-commands return no data, they simply return a success of failure
    /// code. Those sub-commands are subclasses of
    /// <c>CredentialManagementCommand</c>, and the partner response class is
    /// simply <c>IYubiKeyResponse</c>.
    /// </para>
    /// </remarks>
    public class CredentialManagementResponse : YubiKeyResponse, IYubiKeyResponseWithData<CredentialManagementData>
    {
        /// <summary>
        /// Constructs a new instance of
        /// <see cref="CredentialManagementResponse"/> based on a response APDU
        /// provided by the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// A response APDU containing the CBOR response data for the
        /// <c>authenticatorCredentialManagement</c> command.
        /// </param>
        public CredentialManagementResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }

        /// <inheritdoc />
        public CredentialManagementData GetData()
        {
            if (Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(StatusMessage);
            }

            return new CredentialManagementData(ResponseApdu.Data);
        }
    }
}
