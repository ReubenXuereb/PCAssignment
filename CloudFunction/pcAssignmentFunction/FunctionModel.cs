using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using CloudNative.CloudEvents;
using RestSharp;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Google.Cloud.Storage.V1;
using Google.Cloud.Firestore;
using CloudProject.Models;

namespace CloudProject.Models
{
    [FirestoreData]
    public class FunctionModel
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Text { get; set; }

        [FirestoreProperty]
        public string Recipient { get; set; }

        [FirestoreProperty, ServerTimestamp]
        public Timestamp DateSent { get; set; }

        [FirestoreProperty]
        public string AttachmentUri { get; set; }

    }
}