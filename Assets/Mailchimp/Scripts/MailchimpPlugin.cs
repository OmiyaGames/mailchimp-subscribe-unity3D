/// <summary>
/// Created by KirbyRawr(Jairo Baldán) in Monkimun Inc.
/// This plugin integrates the subscribe to the list function of Mailchimp API into Unity3D.
/// Copyright(c) 2015 Monkimun Inc.
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MailChimp {
    public class MailchimpPlugin : MonoBehaviour {
        public const string ExampleUrl = "https://us[#].api.mailchimp.com/3.0/lists/[list-id]/members";
        public const string EmailField = "email_address";
        private static readonly Regex MatchEmailAddress = new Regex("^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$");

        public delegate void Callback(bool error,string message);

        [SerializeField]
        private string url = ExampleUrl;
        [SerializeField]
        private string apiKey = null;
        [SerializeField]
        private List<JsonParameter> requiredParameters = new List<JsonParameter>();
        [SerializeField]
        private List<JsonParameter> optionalParameters = new List<JsonParameter>();
        [SerializeField]
        private bool debugMode;

        private Callback callback;
        private readonly StringBuilder data = new StringBuilder();
        private readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        private readonly Dictionary<string, JsonParameter> setParameterCache = new Dictionary<string, JsonParameter>();

        [System.Serializable]
        public class JsonParameter {
            [SerializeField]
            string parameterKey;
            [SerializeField]
            string parameterValue;
            //public string parameterStruct;

            public string Key {
                get {
                    return parameterKey;
                }
            }

            public string Value {
                get {
                    return parameterValue;
                }
                set {
                    parameterValue = value;
                }
            }

            public JsonParameter(string ParameterKey, string ParameterValue) {
                parameterKey = ParameterKey;
                parameterValue = ParameterValue;
            }

            public JsonParameter(string ParameterKey, string ParameterValue, string ParameterStruct) {
                parameterKey = ParameterKey;
                parameterValue = ParameterValue;
                //parameterStruct = ParameterStruct;
            }
        }

        class JsonParameterComparer : IEqualityComparer<JsonParameter> {
            // Parameters are equal if their structs and parameter keys are equal.
            public bool Equals(JsonParameter x, JsonParameter y) {

                //Check whether the compared objects reference the same data.
                if (Object.ReferenceEquals(x, y))
                    return true;

                //Check whether any of the compared objects is null.
                if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                    return false;

                //Check whether the parameters properties are equal.
                return /*x.Struct == y.Struct &&*/ x.Key == y.Key;
            }

            // If Equals() returns true for a pair of objects
            // then GetHashCode() must return the same value for these objects.

            public int GetHashCode(JsonParameter optionalParameter) {
                //Check whether the object is null
                if (Object.ReferenceEquals(optionalParameter, null))
                    return 0;

                //Get hash code for the Name field if it is not null.
                int hashProductName = optionalParameter.Key == null ? 0 : optionalParameter.Key.GetHashCode();

                //Get hash code for the Code field.
                //int hashProductCode = optionalParameter.Struct.GetHashCode();

                //Calculate the hash code for the product.
                return hashProductName/* ^ hashProductCode*/;
            }

        }

        public void SetParameters(IList<JsonParameter> newParameters) {
            setParameterCache.Clear();
            for (int i = 0; i < optionalParameters.Count; ++i) {
                if (optionalParameters[i] != null) {
                    setParameterCache.Add(optionalParameters[i].Key, optionalParameters[i]);
                }
            }

            // Join values
            JsonParameter oldParam = null;
            for (int i = 0; i < newParameters.Count; ++i) {
                if (newParameters[i] != null) {
                    // Check if this parameter is already in the optionalParameters list
                    if (setParameterCache.TryGetValue(newParameters[i].Key, out oldParam) == true) {
                        // If so, only change the values
                        oldParam.Value = newParameters[i].Value;
                    }
                    else {
                        // Otherwise add the parameter into the optionaParameters list
                        optionalParameters.Add(newParameters[i]);
                    }
                }
            }
        }

        void Awake() {
            if (debugMode) {
                //Debug List of Mailchimp.
                requiredParameters[1].Value = "";

                if (string.IsNullOrEmpty(requiredParameters[1].Value)) {
                    Debug.Log("Your Debug Key is empty add it the line upper this debug");
                }
            }
        }


        [ContextMenu("Populate Default Required Variables")]
        void RequiredVariables() {
            url = ExampleUrl;
            requiredParameters = new List<JsonParameter>() { new JsonParameter("status", "pending"), new JsonParameter(EmailField, "") };
        }

        [ContextMenu("Populate Default Optional Variables")]
        void OptionalVariables() {
            optionalParameters = new List<JsonParameter>() { new JsonParameter("merge_vars", "", "") };
        }

        public void SubscribeEmail(string emailAddress, Callback tempCallback) {
            callback = tempCallback;
            if (!IsEmail(emailAddress)) {
                Debug.Log(emailAddress);
                callback(true, InvalidEmailMessage(emailAddress));
                return;
            }
            encodeData(emailAddress);
        }

        void encodeData(string emailAddress) {
            data.Length = 0;

            //Required Parameters (apikey, id, email)
            data.Append('{');
            for (int r = 0; r < requiredParameters.Count; r++) {
                if (requiredParameters[r].Key == EmailField) {
                    if (string.IsNullOrEmpty(emailAddress) == false) {
                        requiredParameters[r].Value = emailAddress;
                    }
                    requiredParameters[r].Value = requiredParameters[r].Value.Replace("+", "%2b");
                }

                // Check whether to append &
                if (r > 0) {
                    data.Append(',');
                }

                AppendJsonParameter(requiredParameters[r]);
            }

            //Optional Parameters (merged_vars, etc...)
            for (int o = 0; o < optionalParameters.Count; o++) {
                data.Append(',');
                AppendJsonParameter(optionalParameters[o]);
            }
            data.Append('}');

            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] encodedData = utf8.GetBytes(data.ToString());

            StartCoroutine(postMail(url, encodedData));
        }

        private void AppendJsonParameter(JsonParameter parameter) {
            // Append key
            data.Append('"');
            data.Append(parameter.Key);
            data.Append('"');
            data.Append(':');

            // Append value
            data.Append('"');
            data.Append(parameter.Value);
            data.Append('"');
        }

        public bool IsEmail(string emailAddress) {
            bool returnFlag = false;
            if ((string.IsNullOrEmpty(emailAddress) == false) && (MatchEmailAddress.IsMatch(emailAddress) == true)) {
                returnFlag = true;
            }
            else if (debugMode) {
                InvalidEmailMessage(emailAddress);
            }
            return returnFlag;
        }

        public string InvalidEmailMessage(string emailAddress) {
            data.Length = 0;
            data.Append("The Email address: ");
            data.Append(emailAddress);
            data.Append(" is not valid.");
            return data.ToString();
        }

        IEnumerator postMail(string postURL, byte[] postData) {
            if (headers.ContainsKey("Authorization") == false) {
                headers.Add("Authorization", "Basic " + apiKey);
                headers.Add("content-type", "application/json");
            }
            WWW wwwPost = new WWW(postURL, postData, headers);

            yield return wwwPost;

            if (string.IsNullOrEmpty(wwwPost.error) == false) {
                callback(true, wwwPost.error);
                if (debugMode) {
                    Debug.Log("Subscribe error: " + wwwPost.error);
                    Debug.Log("Reason: " + wwwPost.text);
                }
                yield break;
            }
            else {
                callback(false, null);
                if (debugMode) {
                    Debug.Log("Subscribed to the list!");
                }
            }
        }
    }
}
