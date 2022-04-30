using Google.Cloud.Firestore;

namespace PCAssignment.Models
{

    [FirestoreData]
    public class Files
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
