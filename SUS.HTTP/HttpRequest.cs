﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SUS.HTTP
{
    public class HttpRequest
    {
        public static IDictionary<string, Dictionary<string, string>> Sessions 
            = new Dictionary<string, Dictionary<string, string>>();
        public HttpRequest(string requestString)
        {
            this.Headers = new List<Header>();
            this.Cookies = new List<Cookie>();
            this.FormData = new Dictionary<string, string>();
            this.QueryData = new Dictionary<string, string>();

            var lines = requestString.Split(new string[] { HttpConstants.NewLine }, StringSplitOptions.None);
            var firstLine = lines[0];
            var firstLineParts = firstLine.Split(' ');

            this.Method = (HttpMethod)Enum.Parse(typeof(HttpMethod), firstLineParts[0], true);
            this.Path = firstLineParts[1];

            int lineIndex = 1;
            bool isHeaderLine = true;

            StringBuilder bodySb = new StringBuilder();

            while(lineIndex < lines.Length)
            {
                var line = lines[lineIndex];
                lineIndex++;

                if (string.IsNullOrEmpty(line))
                {
                    isHeaderLine = false;
                    continue;
                }

                if (isHeaderLine)
                {
                    this.Headers.Add(new Header(line));
                }
                else
                {
                    bodySb.AppendLine(line);
                }
            }

            this.Body = bodySb.ToString().TrimEnd().TrimStart();


            // set Cookies
            if (this.Headers.Any(x=>x.Name == HttpConstants.RequestCookieHeader))
            {
                var cookiesAsString = this.Headers.FirstOrDefault(x => x.Name == HttpConstants.RequestCookieHeader).Value;

                var cookies = cookiesAsString.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var cookieAsString in cookies)
                {
                    this.Cookies.Add(new Cookie(cookieAsString));
                }
            }

            // set Session
            var sessionCookie = this.Cookies.FirstOrDefault(x => x.Name == HttpConstants.SessionCookieName);

            if (sessionCookie == null)
            {
                var sessinId = Guid.NewGuid().ToString();

                this.Session = new Dictionary<string, string>();
                Sessions.Add(sessinId, this.Session);

                this.Cookies.Add(new Cookie (HttpConstants.SessionCookieName, sessinId));
            }
            else if (!Sessions.ContainsKey(sessionCookie.Value))
            {
                this.Session = new Dictionary<string, string>();
                Sessions.Add(sessionCookie.Value, this.Session);
            }
            else
            {
                this.Session = Sessions[sessionCookie.Value];
            }

            if (this.Path.Contains("?"))
            {
                var pathParts = this.Path.Split(new char[] { '?' }, 2);

                this.Path = pathParts[0];
                this.QueryString = pathParts[1];
            }
            else
            {
                this.QueryString = string.Empty;
            }


            SplitParameters(this.Body, this.FormData);
            SplitParameters(this.QueryString, this.QueryData);
        }

        private void SplitParameters(string parametersAsString, IDictionary<string, string> output)
        {
            
            var parameters = parametersAsString.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var param in parameters)
            {
                var paramParts = param.Split(new char[] { '=' }, 2);
                var name = paramParts[0];
                var value = WebUtility.UrlDecode(paramParts[1]);

                if (!output.ContainsKey(name))
                {
                    output.Add(name, value);
                }

            }
        }

        public string Path { get; set; }

        public string QueryString { get; set; }

        public HttpMethod Method { get; set; }

        public ICollection<Header> Headers { get; set; }

        public ICollection<Cookie> Cookies { get; set; }

        public IDictionary<string, string> FormData { get; set; }

        public IDictionary<string, string> QueryData { get; set; }

        public Dictionary<string, string> Session { get; set; }

        public string Body { get; set; }
    }
}