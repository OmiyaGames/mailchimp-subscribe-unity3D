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
using MailChimp;

namespace MailChimp.Demos {
    public class SetParametersMailchimp : MonoBehaviour {
        [SerializeField]
        MailchimpPlugin mailchimpPlugin;
        [SerializeField]
        string store;
        [SerializeField]
        string language;

        [SerializeField]
        bool SetPlayerPrefs;
        [SerializeField]
        bool SetParameters;

        void Update() {
		
            if (SetPlayerPrefs) {
                SetPlayerPrefs = false;
                setPlayerPrefs();
            }

            if (SetParameters) {
                SetParameters = false;
                getPlayerPrefs();
            }
        }

        void setPlayerPrefs() {
            PlayerPrefs.SetString("store", "apple");
            PlayerPrefs.SetString("language", "spanish");
        }

        void getPlayerPrefs() {
            store = PlayerPrefs.GetString("store");
            language = PlayerPrefs.GetString("language");
            buildData();
        }

        void buildData() {

            List<MailchimpPlugin.JsonParameter> optionalParameters = new List<MailchimpPlugin.JsonParameter>(){ new MailchimpPlugin.JsonParameter("merge_vars", store, "store"), new MailchimpPlugin.JsonParameter("merge_vars", language, "language") };
            setParameters(optionalParameters);
        }

        void setParameters(List<MailchimpPlugin.JsonParameter> optionalParameters) {
            mailchimpPlugin.SetParameters(optionalParameters);
        }
    }
}