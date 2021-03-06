﻿namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Net.Http.Handlers;
    using System.IO;
    public static class HttpClientHelper
    {
        public  static void PostUploadFile
                            (
                                string file
                                , string url
                                , Action
                                    <
                                        HttpClient
                                        , bool
                                        , HttpProgressEventArgs
                                    >
                                    onProgressProcessAction = null
                                , bool reThrowException = false
                                , Func<Exception, Exception, string, bool> onCaughtExceptionProcessFunc = null
                                , Action<bool, Exception, Exception, string> onFinallyProcessAction = null
                                , int httpClientTimeOutInSeconds = 200
                                , int bufferSize = 1024
                            )
        {
            HttpClient httpClient = null;
            using
                (
                    var httpClientHandler
                                    = new HttpClientHandler()
                                    {
                                        UseDefaultCredentials = true
                                    }
                )
            {
                ProgressMessageHandler progressMessageHandler = null;
                if (onProgressProcessAction != null)
                {
                    progressMessageHandler = new ProgressMessageHandler(httpClientHandler);
                    progressMessageHandler
                                .HttpSendProgress
                                        +=
                                            (sender, httpProgressEventArgs) =>
                                            {
                                                onProgressProcessAction
                                                        (
                                                            httpClient
                                                            , true
                                                            , httpProgressEventArgs
                                                        );
                                            };
                    progressMessageHandler
                                .HttpReceiveProgress
                                        +=
                                            (sender, httpProgressEventArgs) =>
                                            {
                                                onProgressProcessAction
                                                        (
                                                            httpClient
                                                            , false
                                                            , httpProgressEventArgs
                                                        );
                                            };
                }
                using
                        (
                            httpClient = new HttpClient(httpClientHandler)
                            {
                                Timeout = TimeSpan
                                                                .FromSeconds
                                                                    (
                                                                        httpClientTimeOutInSeconds
                                                                    )

                            }
                        )
                {
                    try
                    {

                        HttpResponseMessage response = null;
                        using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
                        {
                            // Create a stream content for the file
                            StreamContent content = new StreamContent(fileStream, bufferSize);

                            // Create Multipart form data content, add our submitter data and our stream content
                            MultipartFormDataContent formData = new MultipartFormDataContent();
                            formData.Add(new StringContent("Me"), "submitter");
                            formData.Add(content, "filename", file);

                            // Post the MIME multipart form data upload with the file
                            Uri address = new Uri(url);
                            response = httpClient.PostAsync(address, formData).Result;

                            //FileResult result = await response.Content.ReadAsAsync<FileResult>();
                            //Console.WriteLine("{0}Result:{0}  Filename:  {1}{0}  Submitter: {2}", Environment.NewLine, result.FileNames.FirstOrDefault(), result.Submitter);
                        }



                        //Console.WriteLine("await ed");
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception e)
                    {
                        var innerExceptionMessage = "Caught Exception";
                        var newException = new Exception
                                                    (
                                                        innerExceptionMessage
                                                        , e
                                                    );
                        if (onCaughtExceptionProcessFunc != null)
                        {
                            reThrowException = onCaughtExceptionProcessFunc(e, newException, innerExceptionMessage);
                        }
                        if (reThrowException)
                        {
                            throw
                                newException;
                        }
                    }
                }

            }
        }



        public static void TryParallelHttpClientsSendData
                     (
                        IEnumerable<string> urls
                        , bool isAsyncWithoutWaitResult = false
                        , string httpMethod = "Get"     //HttpMethod.Get.Method
                        , string contentData = null     //string.Empty
                        , Action
                                <
                                    HttpClient
                                    , bool
                                    , HttpProgressEventArgs
                                >
                                    onProgressProcessAction = null
                        , bool reThrowException = false
                        , Func<Exception, Exception, string, bool> onCaughtExceptionProcessFunc = null
                        , Action<bool, Exception, Exception, string> onFinallyProcessAction = null
                        , int httpClientTimeOutInSeconds = 5
                        , int maxDegreeOfParallelism = 4
                     )
        {
            Parallel
                .ForEach
                    (
                        urls
                        , new ParallelOptions()
                        {
                            MaxDegreeOfParallelism = maxDegreeOfParallelism//Environment
                                                                           //    .ProcessorCount
                        }
                        , (x) =>
                        {
                            if (isAsyncWithoutWaitResult)
                            {
                                //如果允许异步执行不等待

                                SendDataWithoutWaitResultAsync
                                    (
                                        x
                                        , httpMethod
                                        , contentData
                                        , onProgressProcessAction
                                        , reThrowException
                                        , (xx, yy, zz) =>
                                        {
                                            return onCaughtExceptionProcessFunc(xx, yy, zz);
                                        }
                                        , null
                                        , httpClientTimeOutInSeconds
                                    );

                            }
                            else
                            {
                                //如果同步执行
                                SendData
                                    (
                                        x
                                        , httpMethod
                                        , contentData
                                        , onProgressProcessAction
                                        , httpClientTimeOutInSeconds
                                    );
                            }
                        }
                    );
        }

        /// <summary>
        /// 异步执行不等待
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpMethod"></param>
        /// <param name="contentData"></param>
        /// <param name="onProgressProcessAction"></param>
        /// <param name="httpClientTimeOutInSeconds"></param>
        public static async Task SendDataWithoutWaitResultAsync
                                    (
                                        string url
                                        , string httpMethod = "Get"
                                        , string contentData = null
                                        , Action
                                                <
                                                    HttpClient
                                                    , bool
                                                    , HttpProgressEventArgs
                                                >
                                            onProgressProcessAction = null
                                        , bool reThrowException = false
                                        , Func<Exception, Exception, string, bool> onCaughtExceptionProcessFunc = null
                                        , Action<bool, Exception, Exception, string> onFinallyProcessAction = null
                                        , int httpClientTimeOutInSeconds = 5
                                    )
        {
            HttpClient httpClient = null;
            using
                (
                    var httpClientHandler
                                    = new HttpClientHandler()
                                    {
                                        UseDefaultCredentials = true
                                    }
                )
            {
                ProgressMessageHandler progressMessageHandler = null;
                if (onProgressProcessAction != null)
                {
                    progressMessageHandler = new ProgressMessageHandler(httpClientHandler);
                    progressMessageHandler
                                .HttpSendProgress
                                        +=
                                            (sender, httpProgressEventArgs) =>
                                            {
                                                onProgressProcessAction
                                                        (
                                                            httpClient
                                                            , true
                                                            , httpProgressEventArgs
                                                        );
                                            };
                    progressMessageHandler
                                .HttpReceiveProgress
                                        +=
                                            (sender, httpProgressEventArgs) =>
                                            {
                                                onProgressProcessAction
                                                        (
                                                            httpClient
                                                            , false
                                                            , httpProgressEventArgs
                                                        );
                                            };
                }

                using
                        (
                            httpClient = new HttpClient(httpClientHandler)
                            {
                                Timeout = TimeSpan
                                                                .FromSeconds
                                                                    (
                                                                        httpClientTimeOutInSeconds
                                                                    )

                            }
                        )
                {
                    try
                    {

                        HttpResponseMessage response = null;
                        //Task<HttpResponseMessage> task = null;
                        if (httpMethod.Trim().ToLower() == "get")
                        {
                            response =
                            await httpClient
                                        .GetAsync
                                            (
                                                url
                                            );
                        }
                        else if (httpMethod.Trim().ToLower() == "post")
                        {
                            StringContent stringContent = null;
                            if (!string.IsNullOrEmpty(contentData))
                            {
                                stringContent = new StringContent(contentData);
                            }
                            response = await httpClient
                                        .PostAsync
                                                (
                                                    url
                                                    , stringContent
                                                );
                            //.ContinueWith
                            //            (
                            //                (x) =>
                            //                {
                            //                    var taskExceptionMessage = "ContinueWith OnlyOnFaulted Caught Exception";
                            //                    var taskException = x.Exception;
                            //                    taskException = taskException.Flatten();

                            //                    var newException = new Exception(taskExceptionMessage, taskException);

                            //                    taskExceptionMessage = string.Format("{1}{0}{2}", ":\r\n", taskExceptionMessage, taskException.ToString());
                            //                    if (onCaughtExceptionProcessFunc != null)
                            //                    {
                            //                        onCaughtExceptionProcessFunc(taskException, newException, taskExceptionMessage);
                            //                    }
                            //                }
                            //                , TaskContinuationOptions.OnlyOnFaulted
                            //            );
                        }

                        Console.WriteLine("await ed");
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception e)
                    {
                        var innerExceptionMessage = "Caught Exception";
                        var newException = new Exception
                                                    (
                                                        innerExceptionMessage
                                                        , e
                                                    );
                        if (onCaughtExceptionProcessFunc != null)
                        {
                            reThrowException = onCaughtExceptionProcessFunc(e, newException, innerExceptionMessage);
                        }
                        if (reThrowException)
                        {
                            throw
                                newException;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 同步执行等待
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpMethod"></param>
        /// <param name="contentData"></param>
        /// <param name="onProgressProcessAction"></param>
        /// <param name="httpClientTimeOutInSeconds"></param>
        public static void SendData
                            (
                                string url
                                , string httpMethod = "Get"
                                , string contentData = null
                                , Action
                                        <
                                            HttpClient
                                            , bool
                                            , HttpProgressEventArgs
                                        >
                                    onProgressProcessAction = null
                                , int httpClientTimeOutInSeconds = 5
                            )
        {
            HttpClient httpClient = null;
            using
                (
                    var httpClientHandler
                                    = new HttpClientHandler()
                                    {
                                        UseDefaultCredentials = true
                                    }

                )
            {
                ProgressMessageHandler progressMessageHandler = null;
                if (onProgressProcessAction != null)
                {
                    progressMessageHandler
                                = new ProgressMessageHandler(httpClientHandler);
                    progressMessageHandler
                                .HttpSendProgress
                                        +=
                                            (sender, httpProgressEventArgs) =>
                                            {
                                                onProgressProcessAction
                                                        (
                                                            httpClient
                                                            , true
                                                            , httpProgressEventArgs
                                                        );
                                            };
                    progressMessageHandler
                                .HttpReceiveProgress
                                        +=
                                            (sender, httpProgressEventArgs) =>
                                            {
                                                onProgressProcessAction
                                                        (
                                                            httpClient
                                                            , false
                                                            , httpProgressEventArgs
                                                        );
                                            };
                }
                using
                        (
                            httpClient
                                        =
                                            new HttpClient(httpClientHandler)
                                            {
                                                Timeout = TimeSpan
                                                                .FromSeconds
                                                                    (
                                                                        httpClientTimeOutInSeconds
                                                                    )
                                            }
                        )
                {
                    HttpResponseMessage response = null;
                    if (httpMethod.Trim().ToLower() == "get")
                    {
                        response = httpClient
                                            .GetAsync
                                                (
                                                    url
                                                )
                                            .Result;
                    }
                    else if (httpMethod.Trim().ToLower() == "post")
                    {
                        StringContent stringContent = null;
                        if (!string.IsNullOrEmpty(contentData))
                        {
                            stringContent = new StringContent(contentData);
                        }
                        response = httpClient
                                            .PostAsync
                                                (
                                                    url
                                                    , stringContent
                                                )
                                            .Result;
                    }
                }
            }
        }

    }
}
