/* 
 * Optical Mark Recognition 
 * Copyright 2015, Justin Fyfe
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * Author: Justin
 * Date: 4-17-2015
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using OmrMarkEngine.Core.Template.Scripting.Forms;

namespace OmrMarkEngine.Template.Scripting.Util
{
    /// <summary>
    ///     Represents a utility for calling rest functions and interpreting the results
    /// </summary>
    public class RestUtil
    {
        // Trusted certs
        private static readonly List<string> s_trustedCerts = new List<string>();

        private static string s_username;
        private static string s_password;

        /// <summary>
        ///     The base uri
        /// </summary>
        private readonly Uri m_baseUri;

        /// <summary>
        ///     Creates a new instance of the rest utility
        /// </summary>
        public RestUtil(Uri baseUri)
        {
            ServicePointManager.ServerCertificateValidationCallback = RestCertificateValidation;
            m_baseUri = baseUri;
        }

        public string GetCurrentUserName
        {
            get { return s_username; }
        }

        /// <summary>
        ///     Validate the REST certificate
        /// </summary>
        private static bool RestCertificateValidation(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors error)
        {
            if ((certificate == null) || (chain == null))
                return false;
            var valid = s_trustedCerts.Contains(certificate.Subject);
            if (!valid && ((chain.ChainStatus.Length > 0) || (error != SslPolicyErrors.None)))
                if (
                    MessageBox.Show(
                        string.Format(
                            "The remote certificate is not trusted. The error was {0}. The certificate is: \r\n{1}\r\nWould you like to temporarily trust this certificate?",
                            error, certificate.Subject), "Certificate Error", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information) == DialogResult.No)
                    return false;
                else
                    s_trustedCerts.Add(certificate.Subject);

            return true;
            //isValid &= chain.ChainStatus.Length == 0;
        }

        /// <summary>
        ///     Get the specified resource contents
        /// </summary>
        public void Post<T>(string resourcePath, T data)
        {
            var retry = 0;
            while (retry++ < 3)
                try
                {
                    var requestUri = new Uri(string.Format("{0}/{1}", m_baseUri, resourcePath));
                    var request = WebRequest.Create(requestUri);
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    if (!string.IsNullOrEmpty(s_username))
                        request.Headers.Add("Authorization",
                            string.Format("Basic {0}",
                                Convert.ToBase64String(
                                    Encoding.UTF8.GetBytes(string.Format("{0}:{1}", s_username, s_password)))));

                    var serializer = new DataContractJsonSerializer(typeof(T));
                    serializer.WriteObject(request.GetRequestStream(), data);
                    var response = request.GetResponse();
                    return;
                }
                catch (WebException e)
                {
                    if ((e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var authForm = new frmAuthenticate();
                        if (authForm.ShowDialog() == DialogResult.OK)
                        {
                            s_username = authForm.Username;
                            s_password = authForm.Password;
                        }
                        else
                            throw new SecurityException("Authorization for service failed!");
                    }
                    else
                        throw;
                }
                catch (Exception e)
                {
                    throw;
                }
            throw new SecurityException("Authorization for service failed!");
        }

        /// <summary>
        ///     Get the specified resource contents
        /// </summary>
        public T Get<T>(string resourcePath, params KeyValuePair<string, object>[] queryParms)
        {
            var retry = 0;

            while (retry++ < 3)
                try
                {
                    var queryBuilder = new StringBuilder();
                    if (queryParms != null)
                        foreach (var qp in queryParms)
                            queryBuilder.AppendFormat("{0}={1}&", qp.Key, qp.Value);
                    if (queryBuilder.Length > 0)
                        queryBuilder.Remove(queryBuilder.Length - 1, 1);
                    var requestUri = new Uri(string.Format("{0}/{1}?{2}", m_baseUri, resourcePath, queryBuilder));

                    var request = WebRequest.Create(requestUri);
                    request.Timeout = 600000;
                    request.Method = "GET";
                    if (!string.IsNullOrEmpty(s_username))
                        request.Headers.Add("Authorization",
                            string.Format("Basic {0}",
                                Convert.ToBase64String(
                                    Encoding.UTF8.GetBytes(string.Format("{0}:{1}", s_username, s_password)))));
                    var response = request.GetResponse();

                    var serializer = new DataContractJsonSerializer(typeof(T));
                    var retVal = (T) serializer.ReadObject(response.GetResponseStream());
                    //Thread.Sleep(100); // Yeah, you're reading that right... Idk why but GIIS WS don't like to be called too quickly
                    return retVal;
                }
                catch (WebException e)
                {
                    if ((e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var authForm = new frmAuthenticate();
                        if (authForm.ShowDialog() == DialogResult.OK)
                        {
                            s_username = authForm.Username;
                            s_password = authForm.Password;
                        }
                        else
                            throw new SecurityException("Authorization for service failed!");
                    }
                    else
                        throw;
                }
                catch (Exception e)
                {
                    throw;
                }
            throw new SecurityException("Authorization for service failed!");
        }

        /// <summary>
        ///     Get raw unparsed response
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="queryParms"></param>
        /// <returns></returns>
        public string GetRawResponse(string resourcePath, params KeyValuePair<string, object>[] queryParms)
        {
            try
            {
                var queryBuilder = new StringBuilder();
                if (queryParms != null)
                    foreach (var qp in queryParms)
                        queryBuilder.AppendFormat("{0}={1}&", qp.Key, qp.Value);
                if (queryBuilder.Length > 0)
                    queryBuilder.Remove(queryBuilder.Length - 1, 1);
                var requestUri = new Uri(string.Format("{0}/{1}?{2}", m_baseUri, resourcePath, queryBuilder));

                var request = WebRequest.Create(requestUri);
                request.Timeout = 600000;
                request.Method = "GET";
                var response = request.GetResponse();
                var reader = new StreamReader(response.GetResponseStream());
                return reader.ReadToEnd();
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}