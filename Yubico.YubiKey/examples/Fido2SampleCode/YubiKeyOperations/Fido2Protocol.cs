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
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    // This file contains the methods that demonstrate how to perform FIDO2
    // Protocol operations.
    public static class Fido2Protocol
    {
        public static bool RunGetAuthenticatorInfo(IYubiKeyDevice yubiKey, out AuthenticatorInfo authenticatorInfo)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                authenticatorInfo = fido2Session.AuthenticatorInfo;
            }

            return true;
        }

        // Note that this method will not check the length of credBlobData,
        // allowing you to see how the SDK handles improper input.
        public static bool RunMakeCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            ReadOnlyMemory<byte> clientDataHash,
            string relyingPartyName,
            string relyingPartyId,
            string userName,
            string userDisplayName,
            ReadOnlyMemory<byte> userId,
            byte[] credBlobData,
            out MakeCredentialData makeCredentialData)
        {
            if (credBlobData is null)
            {
                throw new ArgumentNullException(nameof(credBlobData));
            }

            makeCredentialData = null;
            var relyingParty = new RelyingParty(relyingPartyId)
            {
                Name = relyingPartyName,
            };
            var userEntity = new UserEntity(userId)
            {
                Name = userName,
                DisplayName = userDisplayName,
            };

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                var makeCredentialParameters = new MakeCredentialParameters(relyingParty, userEntity)
                {
                    ClientDataHash = clientDataHash,
                };

                // Although the standard specifies the Options as optional,
                // setting the option "rk" to true means that this credential
                // will be discoverable.
                // If a credential is discoverable, GetAssertion will be able to
                // find the credential given only the relying party ID.
                // If it is not discoverable, the credential ID must be supplied
                // in the AllowList for the call to GetAssertion.
                // This sample code wants all credentials to be discoverable.
                makeCredentialParameters.AddOption("rk", true);

                if (credBlobData.Length > 0)
                {
                    makeCredentialParameters.AddCredBlobExtension(
                        credBlobData, fido2Session.AuthenticatorInfo);
                }

                // In order to make a credential, the PIN must be verified in
                // this session. The SDK doesn't call the Verify method
                // automatically because an app might want to verify with
                // specific permissions (restrictions) and a relying party ID.
                // This sample code verifies without setting the permissions or
                // relying party. To see an example of PIN verification that
                // checks if permissions are possible and if so allows the caller
                // to specify, see the FidowSampleRun.RunVerifyPin method.
                fido2Session.VerifyPin();

                makeCredentialData = fido2Session.MakeCredential(makeCredentialParameters);

                // The MakeCredentialData contains an attestation statement (a
                // signature). It is possible to verify that signature.
                return makeCredentialData.VerifyAttestation(clientDataHash);
            }
        }

        public static bool RunGetAssertions(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            ReadOnlyMemory<byte> clientDataHash,
            string relyingPartyId,
            out IReadOnlyList<GetAssertionData> assertions)
        {
            assertions = new List<GetAssertionData>();
            var relyingParty = new RelyingParty(relyingPartyId);

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                // In order to get assertions, the PIN must be verified in
                // this session. The SDK doesn't call the Verify method
                // automatically because an app might want to verify with
                // specific permissions (restrictions) and a relying party ID.
                // This sample code verifies without setting the permissions or
                // relying party, but to see an example of verifying that checks
                // if permissions are possible and if so allows the caller to
                // specify, see the FidowSampleRun.RunVerifyPin method.
                fido2Session.VerifyPin();

                var getAssertionParameters = new GetAssertionParameters(relyingParty, clientDataHash);

                // If there is a credBlob, we want to get it. By setting the "credBlob"
                // extension, the YubiKey will return it if there is one (and return
                // nothing if there is none). In this case, the data to accompany the
                // name of the extension ("credBlob"), is the CBOR encoding of true.
                // That's simply the single byte 0xF5.
                getAssertionParameters.AddExtension("credBlob", new byte[] { 0xF5 });

                assertions = fido2Session.GetAssertions(getAssertionParameters);
            }

            return true;
        }

        // This method will get information about the discoverable credentials on
        // the YubiKey and return it as a List of object.
        // The first in the list (index zero) will be the result of getting
        // metadata, and will be an object of type Tuple<int,int>.
        // If there are any credentials, the next in the list will be the first
        // relying party. It will be an object of type RelyingParty.
        // Following the relying party will be the credential or credentials. For
        // each entry after a RelyingParty, until reaching the next RelyingParty
        // object, it is a credential, an object of the class CredentialUserInfo.
        // If there are any other relying parties, the list will then contain
        // sets of relying party and credential entries.
        // For example, suppose there are three credentials, two for RP
        // example.com and one for RP sample.org. The list will contain
        // objects representing the following:
        //
        //      entry                  class
        // -------------------------------------------
        //   metadata             Tuple<int,int>
        //   RP example.com       RelyingParty
        //     cred               CredentialUserInfo
        //     cred               CredentialUserInfo
        //   RP sample.org        RelyingParty
        //     cred               CredentialUserInfo
        public static bool RunGetCredentialData(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            out IReadOnlyList<object> credentialData)
        {
            var returnValue = new List<object>();
            credentialData = returnValue;

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                if (fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.credMgmt) != OptionValue.True)
                {
                    return false;
                }

                fido2Session.KeyCollector = KeyCollectorDelegate;

                (int credCount, int remainingCount) = fido2Session.GetCredentialMetadata();

                returnValue.Add(new Tuple<int, int>(credCount, remainingCount));

                IReadOnlyList<RelyingParty> rpList = fido2Session.EnumerateRelyingParties();
                foreach (RelyingParty currentRp in rpList)
                {
                    returnValue.Add(currentRp);

                    IReadOnlyList<CredentialUserInfo> credentialList =
                        fido2Session.EnumerateCredentialsForRelyingParty(currentRp);

                    foreach (CredentialUserInfo currentCredential in credentialList)
                    {
                        returnValue.Add(currentCredential);
                    }
                }
            }

            return true;
        }

        public static bool RunUpdateUserInfo(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            CredentialId credentialId,
            UserEntity updatedInfo)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                fido2Session.UpdateUserInfoForCredential(credentialId, updatedInfo);
            }

            return true;
        }

        public static bool RunDeleteCredential(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            CredentialId credentialId)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                fido2Session.KeyCollector = KeyCollectorDelegate;

                fido2Session.DeleteCredential(credentialId);
            }

            return true;
        }

        public static bool RunGetLargeBlobArray(
            IYubiKeyDevice yubiKey,
            out SerializedLargeBlobArray blobArray)
        {
            blobArray = null;

            using (var fido2Session = new Fido2Session(yubiKey))
            {
                if (fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.largeBlobs) != OptionValue.True)
                {
                    return false;
                }

                blobArray = fido2Session.GetSerializedLargeBlobArray();
            }

            return true;
        }

        // Cycle through the Array, trying to decrypt each entry. If it works,
        // return the decrypted data and the index at which the entry was found.
        // If there is no entry, return "" and -1.
        public static string GetLargeBlobEntry(
            SerializedLargeBlobArray blobArray,
            ReadOnlyMemory<byte> largeBlobKey,
            out int entryIndex)
        {
            if (blobArray is null)
            {
                throw new ArgumentNullException(nameof(blobArray));
            }

            Memory<byte> plaintext = Memory<byte>.Empty;
            byte[] plainArray = Array.Empty<byte>();
            entryIndex = -1;
            try
            {
                for (int index = 0; index < blobArray.Entries.Count; index++)
                {
                    if (blobArray.Entries[index].TryDecrypt(largeBlobKey, out plaintext))
                    {
                        entryIndex = index;
                        plainArray = plaintext.ToArray();
                        return Encoding.Unicode.GetString(plainArray);
                    }
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext.Span);
                CryptographicOperations.ZeroMemory(plainArray);
            }

            return "";
        }

        public static bool RunStoreLargeBlobArray(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SerializedLargeBlobArray blobArray)
        {
            using (var fido2Session = new Fido2Session(yubiKey))
            {
                if (fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.largeBlobs) != OptionValue.True)
                {
                    return false;
                }

                fido2Session.KeyCollector = KeyCollectorDelegate;

                fido2Session.SetSerializedLargeBlobArray(blobArray);
            }

            return true;
        }
    }
}
